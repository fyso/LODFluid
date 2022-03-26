using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class GPURadixSort
    {
        public struct KayValuePair
        {
            public ComputeBuffer Key;
            public ComputeBuffer Value;
        }

        private ComputeShader GPURadixSortCS;
        private int fourWayRadixSortKernel;
        private int shuffleKernel;
        private uint RadixSortThreadNum = 0;

        private string[] CacheNames = new string[5]
        {
            "KeyCache",
            "ValueCache",
            "PrefixSum",
            "BlockSum",
            "BlockScanSum"
        };
        private Dictionary<string, ComputeBuffer> Caches = new Dictionary<string, ComputeBuffer>();

        private GPUScan GPUScanner;

        ~GPURadixSort()
        {
            foreach (var Pair in Caches)
                Pair.Value.Release();
        }

        public GPURadixSort(uint vRadixSortCacheSize)
        {
            GPURadixSortCS = Resources.Load<ComputeShader>("Shaders/GPU Operation/GPURadixSort");
            fourWayRadixSortKernel = GPURadixSortCS.FindKernel("fourWayRadixSort");
            shuffleKernel = GPURadixSortCS.FindKernel("shuffleResult");

            GPURadixSortCS.GetKernelThreadGroupSizes(fourWayRadixSortKernel, out RadixSortThreadNum, out _, out _);

            foreach (var CacheName in CacheNames)
            {
                if (CacheName == "BlockSum" || CacheName == "BlockScanSum")
                {
                    int GroupCount = Mathf.CeilToInt((float)vRadixSortCacheSize / RadixSortThreadNum);
                    ComputeBuffer Cache = new ComputeBuffer(GroupCount * 4, sizeof(uint));
                    Caches.Add(CacheName, Cache);
                }
                else
                {
                    ComputeBuffer Cache = new ComputeBuffer((int)vRadixSortCacheSize, sizeof(uint));
                    Caches.Add(CacheName, Cache);
                }
            }

            GPUScanner = new GPUScan(vRadixSortCacheSize);
        }

        public void RadixSort(
            ref ComputeBuffer voKey,
            ref ComputeBuffer voValue,
            ComputeBuffer vParticleIndrectArgment)
        {
            KayValuePair[] DoubleKeyValuePair = new KayValuePair[2];
            DoubleKeyValuePair[0].Key = voKey;
            DoubleKeyValuePair[0].Value = voValue;
            DoubleKeyValuePair[1].Key = Caches["KeyCache"];
            DoubleKeyValuePair[1].Value = Caches["ValueCache"];

            int Old = 0;
            int New = 1;

            for (int i = 0; i <= 30; i += 2)
            {
                if (i != 0)
                {
                    int Temp = Old;
                    Old = New;
                    New = Temp;
                }
                GPURadixSortCS.SetInt("ShiftWidth", i);
                GPURadixSortCS.SetBuffer(fourWayRadixSortKernel, "OldKey_RW", DoubleKeyValuePair[Old].Key);
                GPURadixSortCS.SetBuffer(fourWayRadixSortKernel, "OldValue_RW", DoubleKeyValuePair[Old].Value);
                GPURadixSortCS.SetBuffer(fourWayRadixSortKernel, "IndrectArgment_R", vParticleIndrectArgment);
                GPURadixSortCS.SetBuffer(fourWayRadixSortKernel, "BlockSum_RW", Caches["BlockSum"]);
                GPURadixSortCS.SetBuffer(fourWayRadixSortKernel, "PrefixSum_RW", Caches["PrefixSum"]);
                GPURadixSortCS.DispatchIndirect(fourWayRadixSortKernel, vParticleIndrectArgment);

                GPUScanner.Scan(Caches["BlockSum"], Caches["BlockScanSum"]);

                GPURadixSortCS.SetBuffer(shuffleKernel, "OldKey_RW", DoubleKeyValuePair[Old].Key);
                GPURadixSortCS.SetBuffer(shuffleKernel, "OldValue_RW", DoubleKeyValuePair[Old].Value);
                GPURadixSortCS.SetBuffer(shuffleKernel, "IndrectArgment_R", vParticleIndrectArgment);
                GPURadixSortCS.SetBuffer(shuffleKernel, "NewKey_RW", DoubleKeyValuePair[New].Key);
                GPURadixSortCS.SetBuffer(shuffleKernel, "NewValue_RW", DoubleKeyValuePair[New].Value);
                GPURadixSortCS.SetBuffer(shuffleKernel, "PrefixSum_R", Caches["PrefixSum"]);
                GPURadixSortCS.SetBuffer(shuffleKernel, "ScanBlockSum_R", Caches["BlockScanSum"]);
                GPURadixSortCS.DispatchIndirect(shuffleKernel, vParticleIndrectArgment);
            }

            Caches["KeyCache"] = DoubleKeyValuePair[Old].Key;
            Caches["ValueCache"] = DoubleKeyValuePair[Old].Value;
            voKey = DoubleKeyValuePair[New].Key;
            voValue = DoubleKeyValuePair[New].Value;
        }
    }
}