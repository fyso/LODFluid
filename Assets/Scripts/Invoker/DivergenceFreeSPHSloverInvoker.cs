using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class DivergenceFreeSPHSloverInvoker : Singleton<DivergenceFreeSPHSloverInvoker>
    {
        private ComputeShader DivergenceFreeSPHSloverCS;
        private int computeAlphaAndDensityKernel;
        private int computeDensityChangeKernel;
        private int sloveDivergenceIterationKernel;
        private int computeDensityAdvKernel;
        private int slovePressureIterationKernel;
        private int updateVelocityWithNoPressureForceKernel;
        private int advectAndSwapParticleBufferKernel;

        private ProfilerMarker DensityChangeProfiler = new ProfilerMarker("Density Change");

        public DivergenceFreeSPHSloverInvoker()
        {
            DivergenceFreeSPHSloverCS = Resources.Load<ComputeShader>("Slover/DivergenceFreeSPHSlover");
            computeAlphaAndDensityKernel = DivergenceFreeSPHSloverCS.FindKernel("computeAlphaAndDensity");
            computeDensityChangeKernel = DivergenceFreeSPHSloverCS.FindKernel("computeDensityChange");
            sloveDivergenceIterationKernel = DivergenceFreeSPHSloverCS.FindKernel("solveDivergenceIteration");
            computeDensityAdvKernel = DivergenceFreeSPHSloverCS.FindKernel("computeDensityAdv");
            slovePressureIterationKernel = DivergenceFreeSPHSloverCS.FindKernel("solvePressureIteration");
            updateVelocityWithNoPressureForceKernel = DivergenceFreeSPHSloverCS.FindKernel("updateVelocityWithNoPressureForce");
            advectAndSwapParticleBufferKernel = DivergenceFreeSPHSloverCS.FindKernel("advectAndSwapParticleBuffer");
        }

        public void Slove(
            ParticleBuffer vBackTarget,
            ParticleBuffer vFrontTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleAlphaCache,
            ComputeBuffer vTargetParticleDensityChangeCache,
            ComputeBuffer vTargetParticleDensityAdvCache,
            Vector3 vHashGridMin, float HashGridCellLength, Vector3Int vHashGridResolution,
            float vSearchRadius, float vParticleVolume, float vTimeStep, float vViscosity, float vGravity,
            int vDivergenceFreeIterationCount = 3, int vPressureIterationCount = 2)
        {
            DivergenceFreeSPHSloverCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            DivergenceFreeSPHSloverCS.SetFloat("HashGridCellLength", HashGridCellLength);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionX", vHashGridResolution.x);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionY", vHashGridResolution.y);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionZ", vHashGridResolution.z); 
            
            DivergenceFreeSPHSloverCS.SetFloat("SearchRadius", vSearchRadius);
            DivergenceFreeSPHSloverCS.SetFloat("ParticleVolume", vParticleVolume);
            DivergenceFreeSPHSloverCS.SetFloat("TimeStep", vTimeStep);
            DivergenceFreeSPHSloverCS.SetFloat("Viscosity", vViscosity);
            DivergenceFreeSPHSloverCS.SetFloat("Gravity", vGravity);

            ///预计算迭代不变值（密度与Alpha）
            Profiler.BeginSample("Compute alpha and density");
            ComputeAlphaAndDensity(
                vBackTarget,
                vTargetParticleIndirectArgment,
                vHashGridCellParticleCount,
                vHashGridCellParticleOffset,
                vTargetParticleDensityCache,
                vTargetParticleAlphaCache);
            Profiler.EndSample();

            ///无散迭代
            ///TODO: Warm Start Slover
            Profiler.BeginSample("Divergence-free iteration");
            for (int i = 0; i < vDivergenceFreeIterationCount; i++)
            {
                Profiler.BeginSample("Compute density change");
                DensityChangeProfiler.Begin();
                ComputeDensityChange(
                    vBackTarget,
                    vTargetParticleIndirectArgment,
                    vHashGridCellParticleCount,
                    vHashGridCellParticleOffset,
                    vTargetParticleDensityChangeCache);
                DensityChangeProfiler.End();
                Profiler.EndSample();

                Profiler.BeginSample("Solve divergence iteration");
                SolveDivergenceIteration(
                    vBackTarget,
                    vTargetParticleIndirectArgment,
                    vHashGridCellParticleCount,
                    vHashGridCellParticleOffset,
                    vTargetParticleDensityCache,
                    vTargetParticleAlphaCache,
                    vTargetParticleDensityChangeCache);
                Profiler.EndSample();
            }
            Profiler.EndSample();

            ///施加外力
            Profiler.BeginSample("Update velocity with no pressure force");
            UpdateVelocityWithNoPressureForce(vBackTarget, vTargetParticleIndirectArgment);
            Profiler.EndSample();

            ///压力迭代
            ///TODO: Warm Start Slover
            Profiler.BeginSample("Pressure iteration");
            for (int i = 0; i < vPressureIterationCount; i++)
            {
                Profiler.BeginSample("Compute density adv");
                ComputeDensityAdv(
                    vBackTarget,
                    vTargetParticleIndirectArgment,
                    vHashGridCellParticleCount,
                    vHashGridCellParticleOffset,
                    vTargetParticleDensityCache,
                    vTargetParticleDensityAdvCache);
                Profiler.EndSample();

                Profiler.BeginSample("Solve pressure iteration");
                SolvePressureIteration(
                    vBackTarget, 
                    vTargetParticleIndirectArgment, 
                    vHashGridCellParticleCount, 
                    vHashGridCellParticleOffset, 
                    vTargetParticleDensityCache, 
                    vTargetParticleAlphaCache, 
                    vTargetParticleDensityAdvCache);
                Profiler.EndSample();
            }
            Profiler.EndSample();

            ///更新位置并Swap ParticleBuffer
            Profiler.BeginSample("Advect and swap particle buffer");
            AdvectAndSwapParticleBuffer(vBackTarget, vFrontTarget, vTargetParticleIndirectArgment);
            Profiler.EndSample();
        }

        private void ComputeAlphaAndDensity(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleAlphaCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "Density_RW", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "Alpha_RW", vTargetParticleAlphaCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(computeAlphaAndDensityKernel, vTargetParticleIndirectArgment);
        }

        private void ComputeDensityChange(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityChangeCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticleVelocity_R", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "DensityChange_RW", vTargetParticleDensityChangeCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityChangeKernel, vTargetParticleIndirectArgment);
        }

        private void SolveDivergenceIteration(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleAlphaCache,
            ComputeBuffer vTargetParticleDensityChangeCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticleVelocity_RW", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "Density_R", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "Alpha_R", vTargetParticleAlphaCache);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "DensityChange_R", vTargetParticleDensityChangeCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(sloveDivergenceIterationKernel, vTargetParticleIndirectArgment);
        }

        private void ComputeDensityAdv(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleDensityAdvCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticleVelocity_R", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "Density_R", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "DensityAdv_RW", vTargetParticleDensityAdvCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityAdvKernel, vTargetParticleIndirectArgment);
        }

        private void SolvePressureIteration(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleAlphaCache,
            ComputeBuffer vTargetParticleDensityAdvCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleVelocity_RW", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Density_R", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Alpha_R", vTargetParticleAlphaCache);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "DensityAdv_R", vTargetParticleDensityAdvCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(slovePressureIterationKernel, vTargetParticleIndirectArgment);
        }

        private void UpdateVelocityWithNoPressureForce(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticleVelocity_RW", vTarget.ParticleVelocityBuffer);

            DivergenceFreeSPHSloverCS.DispatchIndirect(updateVelocityWithNoPressureForceKernel, vTargetParticleIndirectArgment);
        }

        private void AdvectAndSwapParticleBuffer(
            ParticleBuffer vBack,
            ParticleBuffer vFront,
            ComputeBuffer vTargetParticleIndirectArgment)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "BackParticlePosition_R", vBack.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "BackParticleVelocity_R", vBack.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "FrontParticlePosition_RW", vFront.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "FrontParticleVelocity_RW", vFront.ParticleVelocityBuffer);

            DivergenceFreeSPHSloverCS.DispatchIndirect(advectAndSwapParticleBufferKernel, vTargetParticleIndirectArgment);
        }
    }
}
