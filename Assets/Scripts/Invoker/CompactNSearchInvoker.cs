using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class CompactNSearchInvoker : Singleton<CompactNSearchInvoker>
    {
        private ComputeShader CompactNSearchCS;
        private int computeZCodeOfParticle;
        private int bitonicMergeSortParticleIndexByZCode;
        private int assignParticleDataByParticleIndex;
        private int recordHashGridCellParticleOffset;
        private int recordHashGridCellParticlCount;

        public CompactNSearchInvoker()
        {
            CompactNSearchCS = Resources.Load<ComputeShader>("CompactNSearch");
            computeZCodeOfParticle = CompactNSearchCS.FindKernel("computeZCodeOfParticle");
            bitonicMergeSortParticleIndexByZCode = CompactNSearchCS.FindKernel("bitonicMergeSortParticleIndexByZCode");
            assignParticleDataByParticleIndex = CompactNSearchCS.FindKernel("assignParticleDataByParticleIndex");
            recordHashGridCellParticleOffset = CompactNSearchCS.FindKernel("recordHashGridCellParticleOffset");
            recordHashGridCellParticlCount = CompactNSearchCS.FindKernel("recordHashGridCellParticlCount");
        }

        public void ComputeZCodeOfParticle(
            ParticleBuffer vTarget,
            ComputeBuffer vTempParticleIndex,
            Vector3 vHashGridMin,
            float vHashGridCellLength)
        {
            CompactNSearchCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            CompactNSearchCS.SetFloat("HashGridCellLength", vHashGridCellLength);
            CompactNSearchCS.SetBuffer(computeZCodeOfParticle, "ParticleIndrectArgment", vTarget.ParticleIndirectArgumentBuffer);
            CompactNSearchCS.SetBuffer(computeZCodeOfParticle, "ParticleCellIndex_RW", vTarget.ParticleCellIndexBuffer);
            CompactNSearchCS.SetBuffer(computeZCodeOfParticle, "ParticleIndex_RW", vTempParticleIndex);

            CompactNSearchCS.DispatchIndirect(computeZCodeOfParticle, vTarget.ParticleIndirectArgumentBuffer, 0);
        }
    }
}