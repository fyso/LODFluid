using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class HybridInvoker : Singleton<HybridInvoker>
    {
        private ComputeShader HybridSolverCS;
        private int ComputeParticleThickOfCellKernel;
        private int ParticleToGridKernel;
        private int GridToParticleKernel;
        public HybridInvoker()
        {
            HybridSolverCS = Resources.Load<ComputeShader>("Shaders/Solver/HybridSolver");
            ComputeParticleThickOfCellKernel = HybridSolverCS.FindKernel("computeParticleThickOfCell");
            ParticleToGridKernel = HybridSolverCS.FindKernel("particleToGrid");
            GridToParticleKernel = HybridSolverCS.FindKernel("gridToParticle");
        }

        public void CoupleParticleAndGrid(
            ParticleBuffer vParticle,
            ShallowWaterSolver vShallowWaterSolver,
            ComputeBuffer vParticleIndirectArgment,
            ComputeBuffer vHashGridParticleCount,
            Vector3 vHashGridMin, float HashGridCellLength, Vector3Int vHashGridResolution,
            float vTimeStep, float vBandWidth, Vector2 vShallowWaterMin, Vector2 vShallowWaterMax, float vShallowWaterCellLength)
        {
            HybridSolverCS.SetFloat("TimeStep", vTimeStep);
            HybridSolverCS.SetFloat("BandWidth", vBandWidth);
            HybridSolverCS.SetFloat("ShallowWaterCellLength", vShallowWaterCellLength);
            HybridSolverCS.SetVector("ShallowWaterMin", vShallowWaterMin);
            HybridSolverCS.SetVector("ShallowWaterMax", vShallowWaterMax);

            HybridSolverCS.SetVector("HashGridMin", vHashGridMin);
            HybridSolverCS.SetFloat("HashGridCellLength", HashGridCellLength);
            HybridSolverCS.SetInt("HashGridResolutionX", vHashGridResolution.x);
            HybridSolverCS.SetInt("HashGridResolutionY", vHashGridResolution.y);
            HybridSolverCS.SetInt("HashGridResolutionZ", vHashGridResolution.z);

            HybridSolverCS.SetTexture(ComputeParticleThickOfCellKernel, "StateMap_RW", vShallowWaterSolver.StateTexture);
            HybridSolverCS.SetTexture(ComputeParticleThickOfCellKernel, "ParticleHeight_RW", vShallowWaterSolver.ExternHeightTexture);
            HybridSolverCS.SetBuffer(ComputeParticleThickOfCellKernel, "HashGridCellParticleCount_R", vHashGridParticleCount);
            int GridX = Mathf.CeilToInt((float)vShallowWaterSolver.Resolution.x / Common.SWThreadCount);
            int GridY = Mathf.CeilToInt((float)vShallowWaterSolver.Resolution.y / Common.SWThreadCount);
            HybridSolverCS.Dispatch(ComputeParticleThickOfCellKernel, GridX, GridY, 1);

            HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticleIndirectArgment_R", vParticleIndirectArgment);
            HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticlePosition_R", vParticle.ParticlePositionBuffer);
            HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticleVelocity_R", vParticle.ParticleVelocityBuffer);
            HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticleFilter_RW", vParticle.ParticleFilterBuffer);
            HybridSolverCS.SetTexture(ParticleToGridKernel, "StateMap_RW", vShallowWaterSolver.StateTexture);
            HybridSolverCS.SetTexture(ParticleToGridKernel, "VelocityMap_RW", vShallowWaterSolver.VelocityTexture);

            HybridSolverCS.DispatchIndirect(ParticleToGridKernel, vParticleIndirectArgment);

            HybridSolverCS.SetBuffer(GridToParticleKernel, "ParticleIndirectArgment_R", vParticleIndirectArgment);
            HybridSolverCS.SetBuffer(GridToParticleKernel, "ParticlePosition_R", vParticle.ParticlePositionBuffer);
            HybridSolverCS.SetBuffer(GridToParticleKernel, "ParticleVelocity_RW", vParticle.ParticleVelocityBuffer);
            HybridSolverCS.SetTexture(GridToParticleKernel, "StateMap_R", vShallowWaterSolver.StateTexture);
            HybridSolverCS.SetTexture(GridToParticleKernel, "VelocityMap_R", vShallowWaterSolver.VelocityTexture);

            HybridSolverCS.DispatchIndirect(GridToParticleKernel, vParticleIndirectArgment);
        }
    }
}