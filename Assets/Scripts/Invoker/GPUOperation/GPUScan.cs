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
        private string[] CacheNames = new string[2]
        {
            "ScanCache1",
            "ScanCache2"
        };
        private Dictionary<string, ComputeBuffer> Caches = new Dictionary<string, ComputeBuffer>();

        ~GPUScan()
        {
            foreach (var Pair in Caches)
                Pair.Value.Release();
        }

        public GPUScan(uint vScanBufferSize)
        {
            GPUScanCS = Resources.Load<ComputeShader>("GPU Operation/GPUScan");
            scanInBucketKernel = GPUScanCS.FindKernel("scanInBucket");
            scanBucketResultKernel = GPUScanCS.FindKernel("scanBucketResult");
            scanAddBucketResultKernel = GPUScanCS.FindKernel("scanAddBucketResult");
            GPUScanCS.GetKernelThreadGroupSizes(scanInBucketKernel, out scanInBucketGroupThreadNum, out _, out _);
            GPUScanCS.GetKernelThreadGroupSizes(scanAddBucketResultKernel, out scanAddBucketResultGroupThreadNum, out _, out _);

            foreach (var CacheName in CacheNames)
            {
                ComputeBuffer Cache = new ComputeBuffer((int)vScanBufferSize, sizeof(uint));
                Caches.Add(CacheName, Cache);
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
                GPUScanCS.SetBuffer(scanBucketResultKernel, "Output", Caches["ScanCache1"]);
                GPUScanCS.Dispatch(scanBucketResultKernel, GroupCount, 1, 1);

                GroupCount = (int)Mathf.Ceil((float)GroupCount / scanInBucketGroupThreadNum);
                if (GroupCount > 0)
                {
                    GPUScanCS.SetBuffer(scanBucketResultKernel, "Input", Caches["ScanCache1"]);
                    GPUScanCS.SetBuffer(scanBucketResultKernel, "Output", Caches["ScanCache2"]);
                    GPUScanCS.Dispatch(scanBucketResultKernel, GroupCount, 1, 1);

                    GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input", Caches["ScanCache1"]);
                    GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input1", Caches["ScanCache2"]);
                    GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Output", Caches["ScanCache1"]);
                    GPUScanCS.Dispatch(scanAddBucketResultKernel, GroupCount * (int)scanInBucketGroupThreadNum, 1, 1);
                }

                GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Input1", Caches["ScanCache1"]);
                GPUScanCS.SetBuffer(scanAddBucketResultKernel, "Output", voOffsetBuffer);
                GPUScanCS.Dispatch(scanAddBucketResultKernel, (int)Mathf.Ceil(((float)ScanArrayCount / scanAddBucketResultGroupThreadNum)), 1, 1);
            }
        }
    }
}
