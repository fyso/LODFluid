using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class DivergenceFreeSPHSloverInvoker : Singleton<DivergenceFreeSPHSloverInvoker>
    {
        private ComputeShader DivergenceFreeSPHSloverCS;
        private int computeAlphaAndDensityKernel;
        private int computeDensityChangeKernel;
        private int solveDivergenceIterationKernel;
        private int computeDensityAdvKernel;
        private int solvePressureIterationKernel;

        public DivergenceFreeSPHSloverInvoker()
        {
            DivergenceFreeSPHSloverCS = Resources.Load<ComputeShader>("Slover/DivergenceFreeSPHSlover");
            computeAlphaAndDensityKernel = DivergenceFreeSPHSloverCS.FindKernel("computeAlphaAndDensity");
            computeDensityChangeKernel = DivergenceFreeSPHSloverCS.FindKernel("computeDensityChange");
            solveDivergenceIterationKernel = DivergenceFreeSPHSloverCS.FindKernel("solveDivergenceIteration");
            computeDensityAdvKernel = DivergenceFreeSPHSloverCS.FindKernel("computeDensityAdv");
            solvePressureIterationKernel = DivergenceFreeSPHSloverCS.FindKernel("solvePressureIteration");
        }

        public void Slove()
        {
            ///预计算迭代不变值

            ///无散迭代
            ///TODO: Warm Start Slover

            ///施加外力

            ///压力迭代
            ///TODO: Warm Start Slover
            
            ///更新位置并Swap ParticleBuffer
        }
    }
}
