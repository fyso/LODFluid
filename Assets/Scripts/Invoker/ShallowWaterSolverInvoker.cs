using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class ShallowWaterSolverInvoker
    {
        private ComputeShader ShallowWaterSolverCS;
        private int fluxComputationKernel;
        private int fluxApplyKernel;

        private uint ThreadGroupX;
        private uint ThreadGroupY;

        public ShallowWaterSolverInvoker()
        {
            ShallowWaterSolverCS = Resources.Load<ComputeShader>("Shaders/Solver/ShallowWaterSolver");
            fluxComputationKernel = ShallowWaterSolverCS.FindKernel("FluxComputation");
            fluxApplyKernel = ShallowWaterSolverCS.FindKernel("FluxApply");
            ShallowWaterSolverCS.GetKernelThreadGroupSizes(fluxComputationKernel, out ThreadGroupX, out ThreadGroupY, out _);
        }
        public void Solve(
            RenderTexture vStateTexture, 
            RenderTexture vVelocityTexture, 
            RenderTexture vFluxMap, 
            RenderTexture vExternHeight, 
            Vector2Int vReslotion,
            float vTimeDelta,
            float vGravity,
            float vPipeArea,
            float vPipeLength,
            float vCellSize
            )
        {
            Vector2 CellSize = new Vector2(vCellSize, vCellSize);
            ShallowWaterSolverCS.SetInt("_Width", vReslotion.x);
            ShallowWaterSolverCS.SetInt("_Height", vReslotion.y);
            ShallowWaterSolverCS.SetFloat("_TimeDelta", vTimeDelta);
            ShallowWaterSolverCS.SetFloat("_Gravity", vGravity);
            ShallowWaterSolverCS.SetVector("_CellSize", CellSize);
            ShallowWaterSolverCS.SetFloat("_PipeArea", vPipeArea);
            ShallowWaterSolverCS.SetFloat("_PipeLength", vPipeLength);

            ShallowWaterSolverCS.SetTexture(fluxComputationKernel, "HeightMap", vStateTexture);
            ShallowWaterSolverCS.SetTexture(fluxComputationKernel, "VelocityMap", vVelocityTexture);
            ShallowWaterSolverCS.SetTexture(fluxComputationKernel, "FluxMap", vFluxMap);
            ShallowWaterSolverCS.SetTexture(fluxComputationKernel, "ExternHeight", vExternHeight);
            ShallowWaterSolverCS.Dispatch(fluxComputationKernel, (int)Mathf.Ceil((float)vReslotion.x/ThreadGroupX), (int)Mathf.Ceil((float)vReslotion.y/ThreadGroupY), 1);

            ShallowWaterSolverCS.SetTexture(fluxApplyKernel, "HeightMap", vStateTexture);
            ShallowWaterSolverCS.SetTexture(fluxApplyKernel, "VelocityMap", vVelocityTexture);
            ShallowWaterSolverCS.SetTexture(fluxApplyKernel, "FluxMap", vFluxMap);
            ShallowWaterSolverCS.SetTexture(fluxApplyKernel, "ExternHeight", vExternHeight);
            ShallowWaterSolverCS.Dispatch(fluxApplyKernel, (int)Mathf.Ceil((float)vReslotion.x / ThreadGroupX), (int)Mathf.Ceil((float)vReslotion.y / ThreadGroupY), 1);
        }
    }
}