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

        public void ComputeZCodeOfParticle()
        {

        }
    }
}