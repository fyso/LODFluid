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
            ///Ԥ�����������ֵ

            ///��ɢ����
            ///TODO: Warm Start Slover

            ///ʩ������

            ///ѹ������
            ///TODO: Warm Start Slover
            
            ///����λ�ò�Swap ParticleBuffer
        }
    }
}
