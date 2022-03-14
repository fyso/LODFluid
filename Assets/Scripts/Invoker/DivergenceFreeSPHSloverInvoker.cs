using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class DivergenceFreeSPHSloverInvoker : Singleton<DivergenceFreeSPHSloverInvoker>
    {
        private ComputeShader DivergenceFreeSPHSloverCS;
        private int computeFluidPropertyKernel;
        private int computeDensityChangeKernel;
        private int sloveDivergenceIterationKernel;
        private int computeDensityAdvKernel;
        private int slovePressureIterationKernel;
        private int updateVelocityWithNoPressureForceKernel;
        private int advectAndSwapParticleBufferKernel;

        public DivergenceFreeSPHSloverInvoker()
        {
            DivergenceFreeSPHSloverCS = Resources.Load<ComputeShader>("Slover/DivergenceFreeSPHSlover");
            computeFluidPropertyKernel = DivergenceFreeSPHSloverCS.FindKernel("computeFluidProperty");
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
            ComputeBuffer vTargetParticleNormalCache,
            ComputeBuffer vTargetParticleClosestPointAndVolumeCache,
            ComputeBuffer vTargetParticleBoundaryVelocityBufferCache,
            Vector3 vHashGridMin, float HashGridCellLength, Vector3Int vHashGridResolution,
            float vSearchRadius, float vParticleVolume, float vTimeStep, float vViscosity, float vSurfaceTension, float vGravity, bool vUseVolumeMapBoundary,
            int vDivergenceFreeIterationCount = 3, int vPressureIterationCount = 2, bool EnableDivergenceFreeSlover = true)
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
            DivergenceFreeSPHSloverCS.SetFloat("SurfaceTension", vSurfaceTension);
            DivergenceFreeSPHSloverCS.SetFloat("Gravity", vGravity);
            DivergenceFreeSPHSloverCS.SetBool("UseVolumeMapBoundary", vUseVolumeMapBoundary);

            ///预计算迭代不变值（密度与Alpha）
            Profiler.BeginSample("Compute alpha and density");
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "TargetParticlePosition_R", vBackTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "Density_RW", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "Alpha_RW", vTargetParticleAlphaCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "Normal_RW", vTargetParticleNormalCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
            DivergenceFreeSPHSloverCS.DispatchIndirect(computeFluidPropertyKernel, vTargetParticleIndirectArgment);
            Profiler.EndSample();

            ///无散迭代
            if(EnableDivergenceFreeSlover)
            {
                Profiler.BeginSample("Divergence-free iteration");
                for (int i = 0; i < vDivergenceFreeIterationCount; i++)
                {
                    Profiler.BeginSample("Compute density change");
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticlePosition_R", vBackTarget.ParticlePositionBuffer);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticleVelocity_R", vBackTarget.ParticleVelocityBuffer);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "DensityChange_RW", vTargetParticleDensityChangeCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);
                    DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityChangeKernel, vTargetParticleIndirectArgment);
                    Profiler.EndSample();

                    Profiler.BeginSample("Solve divergence iteration");
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticlePosition_R", vBackTarget.ParticlePositionBuffer);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticleVelocity_RW", vBackTarget.ParticleVelocityBuffer);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "Density_R", vTargetParticleDensityCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "Alpha_R", vTargetParticleAlphaCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "DensityChange_R", vTargetParticleDensityChangeCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
                    DivergenceFreeSPHSloverCS.DispatchIndirect(sloveDivergenceIterationKernel, vTargetParticleIndirectArgment);
                    Profiler.EndSample();
                }
                Profiler.EndSample();
            }

            ///施加其它力
            Profiler.BeginSample("Update velocity with no pressure force");
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticlePosition_R", vBackTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticleVelocity_RW", vBackTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "Density_R", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "Normal_R", vTargetParticleNormalCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);
            DivergenceFreeSPHSloverCS.DispatchIndirect(updateVelocityWithNoPressureForceKernel, vTargetParticleIndirectArgment);
            Profiler.EndSample();

            ///压力迭代
            Profiler.BeginSample("Pressure iteration");
            for (int i = 0; i < vPressureIterationCount; i++)
            {
                Profiler.BeginSample("Compute density adv");
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticlePosition_R", vBackTarget.ParticlePositionBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticleVelocity_R", vBackTarget.ParticleVelocityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "Density_R", vTargetParticleDensityCache);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "DensityAdv_RW", vTargetParticleDensityAdvCache);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);
                DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityAdvKernel, vTargetParticleIndirectArgment);
                Profiler.EndSample();

                Profiler.BeginSample("Solve pressure iteration");
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticlePosition_R", vBackTarget.ParticlePositionBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleVelocity_RW", vBackTarget.ParticleVelocityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Density_R", vTargetParticleDensityCache);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Alpha_R", vTargetParticleAlphaCache);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "DensityAdv_R", vTargetParticleDensityAdvCache);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "ParticleClosestPointAndVolume_R", vTargetParticleClosestPointAndVolumeCache);
                DivergenceFreeSPHSloverCS.DispatchIndirect(slovePressureIterationKernel, vTargetParticleIndirectArgment);
                Profiler.EndSample();
            }
            Profiler.EndSample();

            ///更新位置并Swap ParticleBuffer
            Profiler.BeginSample("Advect and swap particle buffer");
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "BackParticlePosition_R", vBackTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "BackParticleVelocity_R", vBackTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "BackParticleFilter_R", vBackTarget.ParticleFilterBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "FrontParticlePosition_RW", vFrontTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "FrontParticleVelocity_RW", vFrontTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "FrontParticleFilter_RW", vFrontTarget.ParticleFilterBuffer);
            DivergenceFreeSPHSloverCS.DispatchIndirect(advectAndSwapParticleBufferKernel, vTargetParticleIndirectArgment);
            Profiler.EndSample();
        }
    }
}
