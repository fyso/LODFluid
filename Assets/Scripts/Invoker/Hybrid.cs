using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class Hybrid
    {
        private ComputeShader HybridSolverCS;
        private int computeParticleThickOfCellKernel;
        private int rasterizeParticlePropertyKernel;
        private int ParticleToGridKernel;
        private int GridToParticleKernel;

        public Hybrid()
        {
            HybridSolverCS = Resources.Load<ComputeShader>("Shaders/Solver/HybridSolver");
            computeParticleThickOfCellKernel = HybridSolverCS.FindKernel("computeParticleThickOfCell");
            rasterizeParticlePropertyKernel = HybridSolverCS.FindKernel("rasterizeParticleProperty");
            ParticleToGridKernel = HybridSolverCS.FindKernel("particleToGrid");
            GridToParticleKernel = HybridSolverCS.FindKernel("gridToParticle");
        }

        public void CoupleParticleAndGrid(
            ParticleBuffer vParticle,
            ShallowWaterSolver vShallowWaterSolver,
            ComputeBuffer vParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            Vector3 vHashGridMin, float HashGridCellLength, Vector3Int vHashGridResolution,
            float vTimeStep, float vBandWidth, Vector2 vShallowWaterMin, Vector2 vShallowWaterMax, Vector2Int vShallowWaterRes, float vShallowWaterCellLength)
        {
            RenderTexture rt = UnityEngine.RenderTexture.active;
            UnityEngine.RenderTexture.active = vShallowWaterSolver.ExternHeightTexture;
            GL.Clear(true, true, Color.clear);
            UnityEngine.RenderTexture.active = rt;

            HybridSolverCS.SetFloat("TimeStep", vTimeStep);
            HybridSolverCS.SetFloat("BandWidth", vBandWidth);
            HybridSolverCS.SetFloat("ShallowWaterCellLength", vShallowWaterCellLength);
            HybridSolverCS.SetVector("ShallowWaterMin", vShallowWaterMin);
            HybridSolverCS.SetVector("ShallowWaterMax", vShallowWaterMax);
            HybridSolverCS.SetInt("ShallowWaterResX", vShallowWaterRes.x);
            HybridSolverCS.SetInt("ShallowWaterResY", vShallowWaterRes.y);

            HybridSolverCS.SetVector("HashGridMin", vHashGridMin);
            HybridSolverCS.SetFloat("HashGridCellLength", HashGridCellLength);
            HybridSolverCS.SetInt("HashGridResolutionX", vHashGridResolution.x);
            HybridSolverCS.SetInt("HashGridResolutionY", vHashGridResolution.y);
            HybridSolverCS.SetInt("HashGridResolutionZ", vHashGridResolution.z);

            HybridSolverCS.SetBuffer(computeParticleThickOfCellKernel, "ParticleIndirectArgment_R", vParticleIndirectArgment);
            HybridSolverCS.SetBuffer(computeParticleThickOfCellKernel, "ParticlePosition_R", vParticle.ParticlePositionBuffer);
            HybridSolverCS.SetBuffer(computeParticleThickOfCellKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            HybridSolverCS.SetBuffer(computeParticleThickOfCellKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            HybridSolverCS.SetTexture(computeParticleThickOfCellKernel, "StateMap_R", vShallowWaterSolver.StateTexture);
            HybridSolverCS.SetTexture(computeParticleThickOfCellKernel, "ExternHeight_RW", vShallowWaterSolver.ExternHeightTexture);
            int GridX = Mathf.CeilToInt((float)vShallowWaterSolver.Resolution.x / Common.SWThreadCount);
            int GridY = Mathf.CeilToInt((float)vShallowWaterSolver.Resolution.y / Common.SWThreadCount);
            HybridSolverCS.Dispatch(computeParticleThickOfCellKernel, GridX, GridY, 1);

            //HybridSolverCS.SetBuffer(rasterizeParticlePropertyKernel, "ParticleIndirectArgment_R", vParticleIndirectArgment);
            //HybridSolverCS.SetBuffer(rasterizeParticlePropertyKernel, "ParticlePosition_R", vParticle.ParticlePositionBuffer);
            //HybridSolverCS.SetBuffer(rasterizeParticlePropertyKernel, "ParticleCellIndex_R", vParticle.ParticleMortonCodeBuffer);
            //HybridSolverCS.SetTexture(rasterizeParticlePropertyKernel, "StateMap_R", vShallowWaterSolver.StateTexture);
            //HybridSolverCS.SetTexture(rasterizeParticlePropertyKernel, "ExternHeight_RW", vShallowWaterSolver.ExternHeightTexture);
            //HybridSolverCS.DispatchIndirect(rasterizeParticlePropertyKernel, vParticleIndirectArgment);

            //HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticleIndirectArgment_R", vParticleIndirectArgment);
            //HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticlePosition_R", vParticle.ParticlePositionBuffer);
            //HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticleVelocity_R", vParticle.ParticleVelocityBuffer);
            //HybridSolverCS.SetBuffer(ParticleToGridKernel, "ParticleFilter_RW", vParticle.ParticleFilterBuffer);
            //HybridSolverCS.SetTexture(ParticleToGridKernel, "StateMap_RW", vShallowWaterSolver.StateTexture);
            //HybridSolverCS.SetTexture(ParticleToGridKernel, "VelocityMap_RW", vShallowWaterSolver.VelocityTexture);
            //HybridSolverCS.DispatchIndirect(ParticleToGridKernel, vParticleIndirectArgment);

            //HybridSolverCS.SetBuffer(GridToParticleKernel, "ParticleIndirectArgment_R", vParticleIndirectArgment);
            //HybridSolverCS.SetBuffer(GridToParticleKernel, "ParticlePosition_R", vParticle.ParticlePositionBuffer);
            //HybridSolverCS.SetBuffer(GridToParticleKernel, "ParticleVelocity_RW", vParticle.ParticleVelocityBuffer);
            //HybridSolverCS.SetTexture(GridToParticleKernel, "StateMap_R", vShallowWaterSolver.StateTexture);
            //HybridSolverCS.SetTexture(GridToParticleKernel, "VelocityMap_R", vShallowWaterSolver.VelocityTexture);
            //HybridSolverCS.DispatchIndirect(GridToParticleKernel, vParticleIndirectArgment);
        }
    }
}