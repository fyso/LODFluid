using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class HybridInvoker : Singleton<HybridInvoker>
    {
        private ComputeShader HybridSolverCS;
        private int ParticleToGridKernel;
        private int GridToParticleKernel;
        public HybridInvoker()
        {
            HybridSolverCS = Resources.Load<ComputeShader>("Solver/HybridSolver");
            ParticleToGridKernel = HybridSolverCS.FindKernel("particleToGrid");
            GridToParticleKernel = HybridSolverCS.FindKernel("gridToParticle");
        }

        public void CoupleParticleAndGrid(
            ParticleBuffer vParticle,
            ShallowWaterBuffer voShallowWater,
            ComputeBuffer vParticleIndirectArgment,
            float vTimeStep, float vBandWidth, Vector2 vShallowWaterMin, Vector2 vShallowWaterMax, float vShallowWaterCellLength)
        {
            HybridSolverCS.SetFloat("TimeStep", vTimeStep);
            HybridSolverCS.SetFloat("BandWidth", vBandWidth);
            HybridSolverCS.SetFloat("ShallowWaterCellLength", vShallowWaterCellLength);
            HybridSolverCS.SetVector("ShallowWaterMin", vShallowWaterMin);
            HybridSolverCS.SetVector("ShallowWaterMax", vShallowWaterMax);

            HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticleIndirectArgment_R", vParticleIndirectArgment);
            HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticlePosition_R", vParticle.ParticlePositionBuffer);
            HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticleVelocity_R", vParticle.ParticleVelocityBuffer);
            HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticleFilter_RW", vParticle.ParticleFilterBuffer);
            HybridSolverCS.SetTexture(ParticleToGridKernel, "StateMap_RW", voShallowWater.StateTexture);
            HybridSolverCS.SetTexture(ParticleToGridKernel, "VelocityMap_RW", voShallowWater.VelocityTexture);

            HybridSolverCS.DispatchIndirect(ParticleToGridKernel, vParticleIndirectArgment);

            HybridSolverCS.SetBuffer(GridToParticleKernel, "ParticleIndirectArgment_R", vParticleIndirectArgment);
            HybridSolverCS.SetBuffer(GridToParticleKernel, "ParticlePosition_R", vParticle.ParticlePositionBuffer);
            HybridSolverCS.SetBuffer(GridToParticleKernel, "ParticleVelocity_RW", vParticle.ParticleVelocityBuffer);
            HybridSolverCS.SetTexture(GridToParticleKernel, "StateMap_R", voShallowWater.StateTexture);
            HybridSolverCS.SetTexture(GridToParticleKernel, "VelocityMap_R", voShallowWater.VelocityTexture);

            HybridSolverCS.DispatchIndirect(GridToParticleKernel, vParticleIndirectArgment);
        }
    }
}