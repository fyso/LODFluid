using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class CompactNSearchInvoker : Singleton<CompactNSearchInvoker>
    {
        private ComputeShader CompactNSearchCS;
        private int insertParticleIntoHashGridKernel;
        private int countingSortFullKernel;

        public CompactNSearchInvoker()
        {
            CompactNSearchCS = Resources.Load<ComputeShader>("CompactNSearch");
            insertParticleIntoHashGridKernel = CompactNSearchCS.FindKernel("insertParticleIntoHashGrid");
            countingSortFullKernel = CompactNSearchCS.FindKernel("countingSortFull");
        }
        
        public void CountingSort(
            ParticleBuffer vTarget,
            ParticleBuffer voSortedTarget,
            ComputeBuffer vParticleIndirectArgumentBuffer,
            ComputeBuffer voHashGridCellParticleCountBuffer,
            ComputeBuffer voHashGridCellParticleOffsetBuffer,
            ComputeBuffer vParticleCellIndexCache,
            ComputeBuffer vParticleInnerSortIndexCache,
            ComputeBuffer vHashScanCache1,
            ComputeBuffer vHashScanCache2,
            Vector3 vHashGridMin,
            float vHashGridCellLength)
        {
            GPUOperation.GetInstance().ClearUIntBufferWithZero(voHashGridCellParticleCountBuffer.count, voHashGridCellParticleCountBuffer);

            CompactNSearchCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            CompactNSearchCS.SetFloat("HashGridCellLength", vHashGridCellLength);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticlePosition_R", vTarget.ParticlePositionBuffer);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleCellIndex_RW", vParticleCellIndexCache);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", voHashGridCellParticleCountBuffer);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", vParticleInnerSortIndexCache);
            CompactNSearchCS.DispatchIndirect(insertParticleIntoHashGridKernel, vParticleIndirectArgumentBuffer);

            GPUOperation.GetInstance().Scan(voHashGridCellParticleCountBuffer.count, voHashGridCellParticleCountBuffer, voHashGridCellParticleOffsetBuffer, vHashScanCache1, vHashScanCache2);

            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleCellIndex_R", vParticleCellIndexCache);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "HashGridCellParticleOffset_R", voHashGridCellParticleOffsetBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleInnerSortIndex_R", vParticleInnerSortIndexCache);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "SortedParticlePosition_RW", voSortedTarget.ParticlePositionBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "SortedParticleVelocity_RW", voSortedTarget.ParticleVelocityBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "SortedParticleDensity_RW", voSortedTarget.ParticleDensityBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticlePosition_R", vTarget.ParticlePositionBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleVelocity_R", vTarget.ParticleVelocityBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleDensity_R", vTarget.ParticleDensityBuffer);
            CompactNSearchCS.DispatchIndirect(countingSortFullKernel, vParticleIndirectArgumentBuffer);
        }
    }
}