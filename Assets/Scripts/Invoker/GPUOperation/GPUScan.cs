using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LODFluid
{
    public class GPUScan
    {
        private ComputeShader GPUScanCS;
        private int scanInBucketKernel;
        private int scanBucketResultKernel;
        private int scanAddBucketResultKernel;
        private uint scanInBucketGroupThreadNum;
        private uint scanAddBucketResultGroupThreadNum;

        private uint ScanArrayCount = 0;
        private ComputeBuffer ScanCache1;
        private ComputeBuffer ScanCache2;

        ~GPUScan()
        {
            ScanCache1.Release();
            if (Mathf.CeilToInt((float)ScanArrayCount / scanInBucketGroupThreadNum) > 0)
            {
                ScanCache2.Release();
            }
        }

        public GPUScan(uint vScanBufferSize)
        {
            GPUScanCS = Resources.Load<ComputeShader>("Shaders/GPU Operation/GPUScan");
            scanInBucketKernel = GPUScanCS.FindKernel("scanInBucket");
            scanBucketResultKernel = GPUScanCS.FindKernel("scanBucketResult");
            scanAddBucketResultKernel = GPUScanCS.FindKernel("scanAddBucketResult");
            GPUScanCS.GetKernelThreadGroupSizes(scanInBucketKernel, out scanInBucketGroupThreadNum, out _, out _);
            GPUScanCS.GetKernelThreadGroupSizes(scanAddBucketResultKernel, out scanAddBucketResultGroupThreadNum, out _, out _);
            ScanCache1 = new ComputeBuffer((int)Mathf.Pow(scanInBucketGroupThreadNum, 2), sizeof(uint));
            if (Mathf.CeilToInt((float)vScanBufferSize / scanInBucketGroupThreadNum) > 0)
            {
                ScanCache2 = new ComputeBuffer((int)scanInBucketGroupThreadNum, sizeof(uint));
            }
            ScanArrayCount = vScanBufferSize;
        }

        public void Scan(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer)
        {
            GPUScanCS.SetBuffer(scanInBucketKernel, "Input", vCountBuffer);
            GPUScanCS.SetBuffer(scanInBucketKernel, "Output", voOffsetBuffer);
            int GroupCount = (int)Mathf.Ceil((float)ScanArrayCount / scanInBucketGroupThreadNum);
            GPUScanCS.Dispatch(scanInBucketKernel, GroupCount, 1, 1);

            GroupCount = (int)Mathf.Ceil((float)GroupCount / scanInBucketGroupThreadNum);
            if (GroupCount > 0)
            {
                GPUScanCS.SetBuffer(scanBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(scanBucketResultKernel, "Output", ScanCache1);
                GPUScanCS.Dispatch(scanBucketResultKernel, GroupCount, 1, 1);

                GroupCount = (int)Mathf.Ceil((float)GroupCount / scanInBucketGroupThreadNum);
                if (GroupCount > 0)
                {
                    GPUScanCS.SetBuffer(scanBucketResultKernel, "Input", ScanCache1);
                    GPUScanCS.SetBuffer(scanBucketResultKernel, "Output", ScanCache2);
                    GPUScanCS.Dispatch(scanBucketResultKernel, GroupCount, 1, 1);

                    GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input", ScanCache1);
                    GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input1", ScanCache2);
                    GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Output", ScanCache1);
                    GPUScanCS.Dispatch(scanAddBucketResultKernel, GroupCount * (int)scanInBucketGroupThreadNum, 1, 1);
                }

                GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input1", ScanCache1);
                GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Output", voOffsetBuffer);
                GPUScanCS.Dispatch(scanAddBucketResultKernel, (int)Mathf.Ceil(((float)ScanArrayCount / scanAddBucketResultGroupThreadNum)), 1, 1);
            }
        }
    }
}
