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

        private ComputeShader DivergenceFreeSPHLocalSloverCS;
        private int divergenceLoopKernel;

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

            DivergenceFreeSPHLocalSloverCS = Resources.Load<ComputeShader>("Slover/DivergenceFreeSPHLocalslover");
            divergenceLoopKernel = DivergenceFreeSPHLocalSloverCS.FindKernel("divergenceLoop");
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
            ComputeBuffer vTargetParticleClosestPointAndVolumeCache,
            ComputeBuffer vTargetParticleBoundaryVelocityBufferCache,
            Vector3 vHashGridMin, float HashGridCellLength, Vector3Int vHashGridResolution,
            float vSearchRadius, float vParticleVolume, float vTimeStep, float vViscosity, float vGravity, bool vUseVolumeMapBoundary,
            int vDivergenceFreeIterationCount = 3, int vPressureIterationCount = 2, bool EnableDivergenceFreeSlover = true, bool EnableFasterSlover = false)
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
            DivergenceFreeSPHSloverCS.SetBool("UseVolumeMapBoundary", vUseVolumeMapBoundary);

            DivergenceFreeSPHLocalSloverCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            DivergenceFreeSPHLocalSloverCS.SetFloat("HashGridCellLength", HashGridCellLength);
            DivergenceFreeSPHLocalSloverCS.SetInt("HashGridResolutionX", vHashGridResolution.x);
            DivergenceFreeSPHLocalSloverCS.SetInt("HashGridResolutionY", vHashGridResolution.y);
            DivergenceFreeSPHLocalSloverCS.SetInt("HashGridResolutionZ", vHashGridResolution.z);
            DivergenceFreeSPHLocalSloverCS.SetFloat("SearchRadius", vSearchRadius);
            DivergenceFreeSPHLocalSloverCS.SetFloat("ParticleVolume", vParticleVolume);
            DivergenceFreeSPHLocalSloverCS.SetFloat("TimeStep", vTimeStep);
            DivergenceFreeSPHLocalSloverCS.SetFloat("Viscosity", vViscosity);
            DivergenceFreeSPHLocalSloverCS.SetFloat("Gravity", vGravity);
            DivergenceFreeSPHLocalSloverCS.SetBool("UseVolumeMapBoundary", vUseVolumeMapBoundary);

            ///预计算迭代不变值（密度与Alpha）
            Profiler.BeginSample("Compute alpha and density");
            ComputeAlphaAndDensity(
                vBackTarget,
                vTargetParticleIndirectArgment,
                vHashGridCellParticleCount,
                vHashGridCellParticleOffset,
                vTargetParticleDensityCache,
                vTargetParticleAlphaCache,
                vTargetParticleClosestPointAndVolumeCache);
            Profiler.EndSample();

            ///无散迭代
            if(EnableDivergenceFreeSlover)
            {
                Profiler.BeginSample("Divergence-free iteration");
                for (int i = 0; i < vDivergenceFreeIterationCount; i++)
                {
                    Profiler.BeginSample("Compute density change");
                    ComputeDensityChange(
                        vBackTarget,
                        vTargetParticleIndirectArgment,
                        vHashGridCellParticleCount,
                        vHashGridCellParticleOffset,
                        vTargetParticleDensityChangeCache,
                        vTargetParticleClosestPointAndVolumeCache,
                        vTargetParticleBoundaryVelocityBufferCache);
                    Profiler.EndSample();

                    Profiler.BeginSample("Solve divergence iteration");
                    SolveDivergenceIteration(
                        vBackTarget,
                        vTargetParticleIndirectArgment,
                        vHashGridCellParticleCount,
                        vHashGridCellParticleOffset,
                        vTargetParticleDensityCache,
                        vTargetParticleAlphaCache,
                        vTargetParticleDensityChangeCache,
                        vTargetParticleClosestPointAndVolumeCache);
                    Profiler.EndSample();
                }
                Profiler.EndSample();
            }

            ///施加其它力
            Profiler.BeginSample("Update velocity with no pressure force");
            UpdateVelocityWithNoPressureForce(
                vBackTarget,
                vTargetParticleIndirectArgment,
                vHashGridCellParticleCount,
                vHashGridCellParticleOffset,
                vTargetParticleDensityCache,
                vTargetParticleClosestPointAndVolumeCache, 
                vTargetParticleBoundaryVelocityBufferCache);
            Profiler.EndSample();

            ///压力迭代
            Profiler.BeginSample("Pressure iteration");

            if (EnableFasterSlover)
            {
                for (int i = 0; i < 6; i++)
                {
                    DivergenceFreeSPHLocalSloverCS.SetInt("ThreadOffset", i % 2 == 0 ? 0 : (int)(GPUGlobalParameterManager.GetInstance().SPHThreadSize * 0.5f)) ;
                    DivergenceFreeSPHLocalSloverCS.SetInt("InBlockIterationCount", 2);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "TargetParticlePosition_R", vBackTarget.ParticlePositionBuffer);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "TargetParticleVelocity_RW", vBackTarget.ParticleVelocityBuffer);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "Density_R", vTargetParticleDensityCache);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "Alpha_R", vTargetParticleAlphaCache);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "DensityAdv_RW", vTargetParticleDensityAdvCache);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
                    DivergenceFreeSPHLocalSloverCS.SetBuffer(divergenceLoopKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);

                    DivergenceFreeSPHLocalSloverCS.DispatchIndirect(divergenceLoopKernel, vTargetParticleIndirectArgment);
                }
            }
            else
            {
                for (int i = 0; i < vPressureIterationCount; i++)
                {
                    Profiler.BeginSample("Compute density adv");
                    ComputeDensityAdv(
                        vBackTarget,
                        vTargetParticleIndirectArgment,
                        vHashGridCellParticleCount,
                        vHashGridCellParticleOffset,
                        vTargetParticleDensityCache,
                        vTargetParticleDensityAdvCache,
                        vTargetParticleClosestPointAndVolumeCache,
                        vTargetParticleBoundaryVelocityBufferCache);
                    Profiler.EndSample();

                    Profiler.BeginSample("Solve pressure iteration");
                    SolvePressureIteration(
                        vBackTarget,
                        vTargetParticleIndirectArgment,
                        vHashGridCellParticleCount,
                        vHashGridCellParticleOffset,
                        vTargetParticleDensityCache,
                        vTargetParticleAlphaCache,
                        vTargetParticleDensityAdvCache,
                        vTargetParticleClosestPointAndVolumeCache);
                    Profiler.EndSample();
                }
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
            ComputeBuffer vTargetParticleAlphaCache,
            ComputeBuffer vTargetParticleClosestPointAndVolumeCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "Density_RW", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "Alpha_RW", vTargetParticleAlphaCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeAlphaAndDensityKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(computeAlphaAndDensityKernel, vTargetParticleIndirectArgment);
        }

        private void ComputeDensityChange(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityChangeCache,
            ComputeBuffer vTargetParticleClosestPointAndVolumeCache,
            ComputeBuffer vTargetParticleBoundaryVelocityBufferCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticleVelocity_R", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "DensityChange_RW", vTargetParticleDensityChangeCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityChangeKernel, vTargetParticleIndirectArgment);
        }

        private void SolveDivergenceIteration(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleAlphaCache,
            ComputeBuffer vTargetParticleDensityChangeCache,
            ComputeBuffer vTargetParticleClosestPointAndVolumeCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticleVelocity_RW", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "Density_R", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "Alpha_R", vTargetParticleAlphaCache);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "DensityChange_R", vTargetParticleDensityChangeCache);
            DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(sloveDivergenceIterationKernel, vTargetParticleIndirectArgment);
        }

        private void ComputeDensityAdv(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleDensityAdvCache,
            ComputeBuffer vTargetParticleClosestPointAndVolumeCache,
            ComputeBuffer vTargetParticleBoundaryVelocityBufferCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticleVelocity_R", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "Density_R", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "DensityAdv_RW", vTargetParticleDensityAdvCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityAdvKernel, vTargetParticleIndirectArgment);
        }

        private void SolvePressureIteration(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleAlphaCache,
            ComputeBuffer vTargetParticleDensityAdvCache,
            ComputeBuffer vTargetParticleClosestPointAndVolumeCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleVelocity_RW", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Density_R", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Alpha_R", vTargetParticleAlphaCache);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "DensityAdv_R", vTargetParticleDensityAdvCache);
            DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(slovePressureIterationKernel, vTargetParticleIndirectArgment);
        }

        private void UpdateVelocityWithNoPressureForce(
            ParticleBuffer vTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleClosestPointAndVolumeCache,
            ComputeBuffer vTargetParticleBoundaryVelocityBufferCache)
        {
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticleVelocity_RW", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "Density_R", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);

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
