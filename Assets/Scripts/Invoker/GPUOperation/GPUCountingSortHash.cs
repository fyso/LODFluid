using UnityEngine;

namespace LODFluid
{
    public class GPUCountingSortHash
    {
        private ComputeShader GPUCountingHashSortCS;
        private int insertParticleIntoHashGridKernel;
        private int countingSortFullKernel;

        ComputeBuffer ParticleCellIndexCache;
        ComputeBuffer ParticleInnerSortIndexCache;
        ComputeBuffer SortedParticlePosCache;
        ComputeBuffer SortedParticleVelCache;
        ComputeBuffer SortedParticleFilterCache;
        ComputeBuffer SortedParticleMortonCodeCache;
        ComputeBuffer SortedParticleDensityCache;

        GPUScan GPUScanner;
        GPUBufferClear GPUBufferClearer;

        ~GPUCountingSortHash()
        {
            ParticleCellIndexCache.Release();
            ParticleInnerSortIndexCache.Release();
            SortedParticlePosCache.Release();
            SortedParticleVelCache.Release();
            SortedParticleFilterCache.Release();
            SortedParticleMortonCodeCache.Release();
            SortedParticleDensityCache.Release();
        }

        public GPUCountingSortHash(uint vMaxParticleCount)
        {
            GPUCountingHashSortCS = Resources.Load<ComputeShader>("Shaders/GPU Operation/GPUCountingSortHash");
            insertParticleIntoHashGridKernel = GPUCountingHashSortCS.FindKernel("insertParticleIntoHashGrid");
            countingSortFullKernel = GPUCountingHashSortCS.FindKernel("countingSortFull");

            SortedParticlePosCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            SortedParticleVelCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            SortedParticleFilterCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));
            SortedParticleMortonCodeCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));
            SortedParticleDensityCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            ParticleCellIndexCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));
            ParticleInnerSortIndexCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));

            GPUScanner = new GPUScan(vMaxParticleCount);
            GPUBufferClearer = new GPUBufferClear();
        }

        public void CountingHashSort(
            ref ParticleBuffer voTarget,
            ComputeBuffer voHashGridParticleCount,
            ComputeBuffer voHashGridParticleOffset,
            ComputeBuffer vArgumentBuffer,
            Vector3 vHashGridMin, float vHashGridCellLength)
        {
            GPUBufferClearer.ClearFloatBufferWithZero(voHashGridParticleCount.count, voHashGridParticleCount);

            GPUCountingHashSortCS.SetVector("HashGridMin", vHashGridMin);
            GPUCountingHashSortCS.SetFloat("HashGridCellLength", vHashGridCellLength);

            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleIndrectArgment_R", vArgumentBuffer);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleCellIndex_RW", ParticleCellIndexCache);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", voHashGridParticleCount);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", ParticleInnerSortIndexCache);
            GPUCountingHashSortCS.DispatchIndirect(insertParticleIntoHashGridKernel, vArgumentBuffer);

            GPUScanner.Scan(voHashGridParticleCount, voHashGridParticleOffset);

            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleIndrectArgment_R", vArgumentBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleCellIndex_R", ParticleCellIndexCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleInnerSortIndex_R", ParticleInnerSortIndexCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "HashGridCellParticleOffset_R", voHashGridParticleOffset);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleVelocity_R", voTarget.ParticleVelocityBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleFilter_R", voTarget.ParticleFilterBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleMortonCode_R", voTarget.ParticleMortonCodeBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleDensity_R", voTarget.ParticleDensityBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticlePosition_RW", SortedParticlePosCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleVelocity_RW", SortedParticleVelCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleFilter_RW", SortedParticleFilterCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleMortonCode_RW", SortedParticleMortonCodeCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleDensity_RW", SortedParticleDensityCache);
            GPUCountingHashSortCS.DispatchIndirect(countingSortFullKernel, vArgumentBuffer);

            Common.SwapComputeBuffer(ref SortedParticlePosCache, ref voTarget.ParticlePositionBuffer);
            Common.SwapComputeBuffer(ref SortedParticleVelCache, ref voTarget.ParticleVelocityBuffer);
            Common.SwapComputeBuffer(ref SortedParticleFilterCache, ref voTarget.ParticleFilterBuffer);
            Common.SwapComputeBuffer(ref SortedParticleMortonCodeCache, ref voTarget.ParticleMortonCodeBuffer);
            Common.SwapComputeBuffer(ref SortedParticleDensityCache, ref voTarget.ParticleDensityBuffer);
        }
    }
}
