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
        
        public void CountingSortFullKernel(
            ParticleBuffer vTarget,
            ParticleBuffer vSortedTarget,
            ComputeBuffer vParticleIndirectArgumentBuffer,
            ComputeBuffer vHashGridCellParticleCountBuffer,
            ComputeBuffer vHashGridCellParticleOffsetBuffer,
            ComputeBuffer vParticleCellIndexCache,
            ComputeBuffer vParticleInnerSortIndexCache,
            ComputeBuffer vHashScanCache1,
            ComputeBuffer vHashScanCache2,
            Vector3 vHashGridMin,
            float vHashGridCellLength)
        {
            GPUOperation.GetInstance().ClearUIntBufferWithZero(vHashGridCellParticleCountBuffer.count, vHashGridCellParticleCountBuffer);

            CompactNSearchCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            CompactNSearchCS.SetFloat("HashGridCellLength", vHashGridCellLength);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticlePosition_R", vTarget.ParticlePositionBuffer);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleCellIndex_RW", vParticleCellIndexCache);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", vHashGridCellParticleCountBuffer);
            CompactNSearchCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", vParticleInnerSortIndexCache);
            CompactNSearchCS.DispatchIndirect(insertParticleIntoHashGridKernel, vParticleIndirectArgumentBuffer);

            GPUOperation.GetInstance().Scan(vHashGridCellParticleCountBuffer.count, vHashGridCellParticleCountBuffer, vHashGridCellParticleOffsetBuffer, vHashScanCache1, vHashScanCache2);

            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleCellIndex_R", vParticleCellIndexCache);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffsetBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleInnerSortIndex_R", vParticleInnerSortIndexCache);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "SortedParticlePosition_RW", vSortedTarget.ParticlePositionBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "SortedParticleVelocity_RW", vSortedTarget.ParticleVelocityBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "SortedParticleDensity_RW", vSortedTarget.ParticleDensityBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticlePosition_R", vTarget.ParticlePositionBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleVelocity_R", vTarget.ParticleVelocityBuffer);
            CompactNSearchCS.SetBuffer(countingSortFullKernel, "ParticleDensity_R", vTarget.ParticleDensityBuffer);
            CompactNSearchCS.DispatchIndirect(countingSortFullKernel, vParticleIndirectArgumentBuffer);
        }
    }
}