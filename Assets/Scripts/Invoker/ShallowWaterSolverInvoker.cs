using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class ShallowWaterSolverInvoker : Singleton<ShallowWaterSolverInvoker>
    {
        private ComputeShader ShallowWaterSolverCS;
        private int ComputeShallowWaterKernel;

        private uint ThreadGroupX;
        private uint ThreadGroupY;

        public ShallowWaterSolverInvoker()
        {
            ShallowWaterSolverCS = Resources.Load<ComputeShader>("Slover/ShallowWaterSolver");
            ComputeShallowWaterKernel = ShallowWaterSolverCS.FindKernel("ComputeShallowWater");

            ShallowWaterSolverCS.GetKernelThreadGroupSizes(ComputeShallowWaterKernel, out ThreadGroupX, out ThreadGroupY, out _);
        }

        public void AddRandomWater(
            ComputeBuffer vHeightBuffer,
            Vector2Int vReslotion
            )
        {
            return;
        }

        public void ComputeShallowWater(
            ComputeBuffer vHeightBuffer,
            ComputeBuffer vOldHeightBuffer,
            ComputeBuffer vNewHeightBuffer,
            Vector2Int vReslotion
            )
        {
            ShallowWaterSolverCS.SetInts("Resolution", vReslotion.x, vReslotion.y);

            // Scale: dt^2 * H * h / dt^2
            //float Scale = vTimeDelta * vTimeDelta * vConstH * vGravity / (CellSize.x * CellSize.x);
            float Scale = 0.005f;
            ShallowWaterSolverCS.SetFloat("_Scale", Scale);
            float vDamping = 0.996f;
            ShallowWaterSolverCS.SetFloat("_Damping", vDamping);


            ShallowWaterSolverCS.SetBuffer(ComputeShallowWaterKernel, "HeightBuffer", vHeightBuffer);
            ShallowWaterSolverCS.SetBuffer(ComputeShallowWaterKernel, "OldHeightBuffer", vOldHeightBuffer);
            ShallowWaterSolverCS.SetBuffer(ComputeShallowWaterKernel, "NewHeightBuffer", vNewHeightBuffer);
            ShallowWaterSolverCS.Dispatch(ComputeShallowWaterKernel, (int)Mathf.Ceil((float)vReslotion.x / ThreadGroupX), (int)Mathf.Ceil((float)vReslotion.y / ThreadGroupY), 1);
        }

        public void ConjugateGradient(ComputeBuffer vMask, ComputeBuffer vB, ComputeBuffer voX, int li, int ui, int lj, int uj)
        {
            return;
        }
    }
}