using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class GPUOperation : Singleton<GPUOperation>
    {
        private ComputeShader GPUScanCS;
        private int scanInBucketKernel;
        private int scanBucketResultKernel;
        private int scanAddBucketResultKernel;
        private uint scanInBucketGroupThreadNum;
        private uint scanAddBucketResultGroupThreadNum;

        private ComputeShader GPUBufferClearCS;
        private int clearUIntBufferWithZeroKernel;
        private uint clearUIntBufferWithZeroGroupThreadNum;

        public GPUOperation()
        {
            GPUScanCS = Resources.Load<ComputeShader>("GPU Operation/GPUScan");
            scanInBucketKernel = GPUScanCS.FindKernel("scanInBucket");
            scanBucketResultKernel = GPUScanCS.FindKernel("scanBucketResult");
            scanAddBucketResultKernel = GPUScanCS.FindKernel("scanAddBucketResult");
            GPUScanCS.GetKernelThreadGroupSizes(scanInBucketKernel, out scanInBucketGroupThreadNum, out _, out _);
            GPUScanCS.GetKernelThreadGroupSizes(scanAddBucketResultKernel, out scanAddBucketResultGroupThreadNum, out _, out _);
            
            GPUBufferClearCS = Resources.Load<ComputeShader>("GPU Operation/GPUBufferClear");
            clearUIntBufferWithZeroKernel = GPUBufferClearCS.FindKernel("clearUIntBufferWithZero");
            GPUBufferClearCS.GetKernelThreadGroupSizes(clearUIntBufferWithZeroKernel, out clearUIntBufferWithZeroGroupThreadNum, out _, out _);
        }

        public void ClearUIntBufferWithZero(int vBufferSize, ComputeBuffer vTargetBuffer)
        {
            GPUBufferClearCS.SetInt("BufferSize", vBufferSize);
            GPUBufferClearCS.SetBuffer(clearUIntBufferWithZeroKernel, "TargetBuffer_RW", vTargetBuffer);
            GPUBufferClearCS.Dispatch(clearUIntBufferWithZeroKernel, (int)Mathf.Ceil(((float)vBufferSize / clearUIntBufferWithZeroGroupThreadNum)), 1, 1);
        }

        public void Scan(int vBufferSize, ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer, ComputeBuffer vScanCacheBuffer1, ComputeBuffer vScanCacheBuffer2)
        {
            GPUScanCS.SetBuffer(scanInBucketKernel, "Input", vCountBuffer);
            GPUScanCS.SetBuffer(scanInBucketKernel, "Output", voOffsetBuffer);
            int GroupCount = (int)Mathf.Ceil((float)vBufferSize / scanInBucketGroupThreadNum);
            GPUScanCS.Dispatch(scanInBucketKernel, GroupCount, 1, 1);

            GroupCount = (int)Mathf.Ceil((float)GroupCount / scanInBucketGroupThreadNum);
            if (GroupCount > 0)
            {
                GPUScanCS.SetBuffer(scanBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(scanBucketResultKernel, "Output", vScanCacheBuffer1);
                GPUScanCS.Dispatch(scanBucketResultKernel, GroupCount, 1, 1);

                GroupCount = (int)Mathf.Ceil((float)GroupCount / scanInBucketGroupThreadNum);
                if (GroupCount > 0)
                {
                    GPUScanCS.SetBuffer(scanBucketResultKernel, "Input", vScanCacheBuffer1);
                    GPUScanCS.SetBuffer(scanBucketResultKernel, "Output", vScanCacheBuffer2);
                    GPUScanCS.Dispatch(scanBucketResultKernel, GroupCount, 1, 1);

                    GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input", vScanCacheBuffer1);
                    GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input1", vScanCacheBuffer2);
                    GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Output", vScanCacheBuffer1);
                    GPUScanCS.Dispatch(scanAddBucketResultKernel, GroupCount * (int)scanInBucketGroupThreadNum, 1, 1);
                }

                GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input1", vScanCacheBuffer1);
                GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Output", voOffsetBuffer);
                GPUScanCS.Dispatch(scanAddBucketResultKernel, (int)Mathf.Ceil(((float)vBufferSize / scanAddBucketResultGroupThreadNum)), 1, 1);
            }
        }
    }
}
