using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class GPURadixSortHash
    {
        private ComputeShader CompactNSearchCS;
        private int computeMortonCodeKernel;
        private int assignParticleKernel;
        private int computeHashGridParticleOffsetKernel;
        private int computeHashGridParticleCountKernel;

        private ComputeBuffer ParticleIndexCache;
        private ComputeBuffer SortedParticlePosCache;
        private ComputeBuffer SortedParticleVelCache;
        private ComputeBuffer SortedParticleFilterCache;

        private GPURadixSort GPURadixSorter;
        private GPUBufferClear GPUBufferClearer;

        ~GPURadixSortHash()
        {
            ParticleIndexCache.Release();
            SortedParticlePosCache.Release();
            SortedParticleVelCache.Release();
            SortedParticleFilterCache.Release();
        }

        public GPURadixSortHash(uint vMaxParticleSize)
        {
            CompactNSearchCS = Resources.Load<ComputeShader>("Shaders/GPU Operation/GPURadixSortHash");
            computeMortonCodeKernel = CompactNSearchCS.FindKernel("computeMortonCode");
            assignParticleKernel = CompactNSearchCS.FindKernel("assignParticle");
            computeHashGridParticleOffsetKernel = CompactNSearchCS.FindKernel("computeHashGridParticleOffset");
            computeHashGridParticleCountKernel = CompactNSearchCS.FindKernel("computeHashGridParticleCount");

            ParticleIndexCache = new ComputeBuffer((int)vMaxParticleSize, sizeof(uint));
            SortedParticlePosCache = new ComputeBuffer((int)vMaxParticleSize, sizeof(float) * 3);
            SortedParticleVelCache = new ComputeBuffer((int)vMaxParticleSize, sizeof(float) * 3);
            SortedParticleFilterCache = new ComputeBuffer((int)vMaxParticleSize, sizeof(uint));

            GPURadixSorter = new GPURadixSort(vMaxParticleSize);
            GPUBufferClearer = new GPUBufferClear();
        }

        public void ComputeMortonCode(
            ParticleBuffer voTarget,
            ComputeBuffer vParticleIndirectArgumentBuffer,
            Vector3 vHashGridMin,
            float vHashGridCellLength,
            Vector3Int vHashGridResolution)
        {
            CompactNSearchCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            CompactNSearchCS.SetFloat("HashGridCellLength", vHashGridCellLength);
            CompactNSearchCS.SetInts("HashGridResolution", vHashGridResolution.x, vHashGridResolution.y, vHashGridResolution.z);
            CompactNSearchCS.SetBuffer(computeMortonCodeKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            CompactNSearchCS.SetBuffer(computeMortonCodeKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            CompactNSearchCS.SetBuffer(computeMortonCodeKernel, "ParticleCellIndex_RW", voTarget.ParticleMortonCodeBuffer);
            CompactNSearchCS.DispatchIndirect(computeMortonCodeKernel, vParticleIndirectArgumentBuffer);
        }

        public void Sort(
            ref ParticleBuffer voTarget,
            ComputeBuffer vParticleIndirectArgumentBuffer)
        {
            GPUBufferClearer.ClearUIntBufferWithSequence(ParticleIndexCache.count, ParticleIndexCache);

            GPURadixSorter.RadixSort(
                ref voTarget.ParticleMortonCodeBuffer,
                ref ParticleIndexCache,
                vParticleIndirectArgumentBuffer);

            CompactNSearchCS.SetBuffer(assignParticleKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            CompactNSearchCS.SetBuffer(assignParticleKernel, "NewIndex_R", ParticleIndexCache);
            CompactNSearchCS.SetBuffer(assignParticleKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            CompactNSearchCS.SetBuffer(assignParticleKernel, "ParticleVelocity_R", voTarget.ParticleVelocityBuffer);
            CompactNSearchCS.SetBuffer(assignParticleKernel, "ParticleFilter_R", voTarget.ParticleFilterBuffer);
            CompactNSearchCS.SetBuffer(assignParticleKernel, "SortedParticlePosition_RW", SortedParticlePosCache);
            CompactNSearchCS.SetBuffer(assignParticleKernel, "SortedParticleVelocity_RW", SortedParticleVelCache);
            CompactNSearchCS.SetBuffer(assignParticleKernel, "SortedParticleFilter_RW", SortedParticleFilterCache);
            CompactNSearchCS.DispatchIndirect(assignParticleKernel, vParticleIndirectArgumentBuffer);

            Common.SwapComputeBuffer(ref voTarget.ParticlePositionBuffer, ref SortedParticlePosCache);
            Common.SwapComputeBuffer(ref voTarget.ParticleVelocityBuffer, ref SortedParticleVelCache);
            Common.SwapComputeBuffer(ref voTarget.ParticleFilterBuffer, ref SortedParticleFilterCache);
        }

        public void GenerateHashData(
            ParticleBuffer vTarget,
            ComputeBuffer vParticleIndirectArgumentBuffer,
            ComputeBuffer vHashGridParticleOffsetBuffer,
            ComputeBuffer vHashGridParticleCountBuffer)
        {
            GPUBufferClearer.ClearUIntBufferWithZero(vHashGridParticleOffsetBuffer.count, vHashGridParticleOffsetBuffer);
            GPUBufferClearer.ClearUIntBufferWithZero(vHashGridParticleCountBuffer.count, vHashGridParticleCountBuffer);

            CompactNSearchCS.SetBuffer(computeHashGridParticleOffsetKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            CompactNSearchCS.SetBuffer(computeHashGridParticleOffsetKernel, "ParticleCellIndex_R", vTarget.ParticleMortonCodeBuffer);
            CompactNSearchCS.SetBuffer(computeHashGridParticleOffsetKernel, "HashGridParticleOffset_RW", vHashGridParticleOffsetBuffer);
            CompactNSearchCS.DispatchIndirect(computeHashGridParticleOffsetKernel, vParticleIndirectArgumentBuffer);

            CompactNSearchCS.SetInt("HashCellCount", vHashGridParticleOffsetBuffer.count);
            CompactNSearchCS.SetBuffer(computeHashGridParticleCountKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            CompactNSearchCS.SetBuffer(computeHashGridParticleCountKernel, "ParticleCellIndex_R", vTarget.ParticleMortonCodeBuffer);
            CompactNSearchCS.SetBuffer(computeHashGridParticleCountKernel, "HashGridParticleOffset_R", vHashGridParticleOffsetBuffer);
            CompactNSearchCS.SetBuffer(computeHashGridParticleCountKernel, "HashGridParticleCount_RW", vHashGridParticleCountBuffer);
            CompactNSearchCS.DispatchIndirect(computeHashGridParticleCountKernel, vParticleIndirectArgumentBuffer);
        }
    }
}