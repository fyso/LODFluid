using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class Hybrid
    {
        private ComputeShader HybridSolverCS;
        private int computeParticleThickOfCellKernel;
        private int sampleShallowWaterBoundaryKernel;

        public Hybrid()
        {
            HybridSolverCS = Resources.Load<ComputeShader>("Shaders/Solver/HybridSolver");
            computeParticleThickOfCellKernel = HybridSolverCS.FindKernel("computeParticleThickOfCell");
            sampleShallowWaterBoundaryKernel = HybridSolverCS.FindKernel("sampleShallowWaterBoundary");
        }

        public void CoupleParticleAndGrid(
            ParticleBuffer vParticle,
            ShallowWaterSolver vShallowWaterSolver,
            ComputeBuffer vParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            Vector3 vHashGridMin, float HashGridCellLength, Vector3Int vHashGridResolution,
            float vTimeStep, float vBandWidth, float vParticleVolume, Vector2 vShallowWaterMin, Vector2 vShallowWaterMax, Vector2Int vShallowWaterRes, float vShallowWaterCellLength)
        {
            RenderTexture rt = UnityEngine.RenderTexture.active;
            UnityEngine.RenderTexture.active = vShallowWaterSolver.ExternHeightTexture;
            GL.Clear(true, true, Color.clear);
            UnityEngine.RenderTexture.active = rt;

            HybridSolverCS.SetFloat("TimeStep", vTimeStep);
            HybridSolverCS.SetFloat("BandWidth", vBandWidth);
            HybridSolverCS.SetFloat("ParticleVolume", vParticleVolume);
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

            HybridSolverCS.SetBuffer(computeParticleThickOfCellKernel, "ParticlePosition_R", vParticle.ParticlePositionBuffer);
            HybridSolverCS.SetBuffer(computeParticleThickOfCellKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            HybridSolverCS.SetBuffer(computeParticleThickOfCellKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            HybridSolverCS.SetTexture(computeParticleThickOfCellKernel, "StateMap_R", vShallowWaterSolver.StateTexture);
            HybridSolverCS.SetTexture(computeParticleThickOfCellKernel, "ExternHeight_RW", vShallowWaterSolver.ExternHeightTexture);
            int GridX = Mathf.CeilToInt((float)vShallowWaterSolver.Resolution.x / Common.SWThreadCount);
            int GridY = Mathf.CeilToInt((float)vShallowWaterSolver.Resolution.y / Common.SWThreadCount);
            HybridSolverCS.Dispatch(computeParticleThickOfCellKernel, GridX, GridY, 1);

            //HybridSolverCS.SetBuffer(sampleShallowWaterBoundaryKernel, "ParticleIndirectArgment_R", vParticleIndirectArgment);
            //HybridSolverCS.SetBuffer(sampleShallowWaterBoundaryKernel, "ParticlePosition_RW", vParticle.ParticlePositionBuffer);
            //HybridSolverCS.SetBuffer(sampleShallowWaterBoundaryKernel, "ParticleVelocity_RW", vParticle.ParticleVelocityBuffer);
            //HybridSolverCS.SetTexture(sampleShallowWaterBoundaryKernel, "StateMap_RW", vShallowWaterSolver.StateTexture);
            //HybridSolverCS.SetTexture(sampleShallowWaterBoundaryKernel, "VelocityMap_RW", vShallowWaterSolver.VelocityTexture);
            //HybridSolverCS.SetTexture(sampleShallowWaterBoundaryKernel, "HeightChange_R", vShallowWaterSolver.HeightChangeTexture);
            //HybridSolverCS.DispatchIndirect(sampleShallowWaterBoundaryKernel, vParticleIndirectArgment);
        }
    }
}