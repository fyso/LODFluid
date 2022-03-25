using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class DFSPHInvoker
    {
        private DynamicParticleToolInvoker DynamicParticleTool;
        private GPUCountingSortHash CompactNSearch;
        private DivergenceFreeSPHSolverInvoker DivergenceFreeSPHSolver;
        private List<GameObject> BoundaryObjects;

        public DFSPHInvoker(List<GameObject> vBoundaryObjects)
        {
            CompactNSearch = new GPUCountingSortHash(GPUGlobalParameterManager.GetInstance().Max3DParticleCount);
            DynamicParticleTool = new DynamicParticleToolInvoker(
                GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius);
            DivergenceFreeSPHSolver = new DivergenceFreeSPHSolverInvoker(GPUGlobalParameterManager.GetInstance().Max3DParticleCount);

            VolumeMapBoundarySolverInvoker.GetInstance().GenerateBoundaryMapData(
                vBoundaryObjects,
                GPUResourceManager.GetInstance().Volume,
                GPUResourceManager.GetInstance().SignedDistance,
                GPUGlobalParameterManager.GetInstance().SearchRadius,
                GPUGlobalParameterManager.GetInstance().CubicZero);

            BoundaryObjects = vBoundaryObjects;
        }

        public void AddParticleBlock(Vector3 WaterGeneratePosition, Vector3Int WaterGenerateResolution, Vector3 WaterGenerateInitVelocity)
        {
            DynamicParticleTool.AddParticleBlock(
                GPUResourceManager.GetInstance().Dynamic3DParticle,
                GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                WaterGeneratePosition,
                WaterGenerateResolution,
                WaterGenerateInitVelocity);
        }

        public void Solve(int DivergenceIterationCount, int PressureIterationCount)
        {
            Profiler.BeginSample("Delete out of range particle");
            DynamicParticleTool.DeleteParticleOutofRange(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDistanceBuffer,
                    GPUGlobalParameterManager.GetInstance().HashGridMin,
                    GPUGlobalParameterManager.GetInstance().HashCellLength,
                    GPUGlobalParameterManager.GetInstance().HashResolution);
            Profiler.EndSample();

            Profiler.BeginSample("Narrow");
            DynamicParticleTool.NarrowParticleData(
                    ref GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer);
            Profiler.EndSample();

            Profiler.BeginSample("Sort by MortonCode");
            CompactNSearch.CountingHashSort(
                ref GPUResourceManager.GetInstance().Dynamic3DParticle,
                GPUResourceManager.GetInstance().HashGridCellParticleCountBuffer,
                GPUResourceManager.GetInstance().HashGridCellParticleOffsetBuffer,
                GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                GPUGlobalParameterManager.GetInstance().HashGridMin,
                GPUGlobalParameterManager.GetInstance().HashCellLength);
            Profiler.EndSample();

            Profiler.BeginSample("Query closest point and volume");
            VolumeMapBoundarySolverInvoker.GetInstance().QueryClosestPointAndVolume(
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    BoundaryObjects,
                    GPUResourceManager.GetInstance().Volume,
                    GPUResourceManager.GetInstance().SignedDistance,
                    GPUResourceManager.GetInstance().Dynamic3DParticleClosestPointBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDistanceBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleVolumeBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleBoundaryVelocityBuffer,
                    GPUGlobalParameterManager.GetInstance().SearchRadius,
                    GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius);
            Profiler.EndSample();

            Profiler.BeginSample("Slove divergence-free SPH");
            DivergenceFreeSPHSolver.Slove(
                    ref GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleCountBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleOffsetBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleAlphaBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityChangeBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityAdvBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleNormalBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleClosestPointBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleVolumeBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleBoundaryVelocityBuffer,
                    GPUGlobalParameterManager.GetInstance().HashGridMin,
                    GPUGlobalParameterManager.GetInstance().HashCellLength,
                    GPUGlobalParameterManager.GetInstance().HashResolution,
                    GPUGlobalParameterManager.GetInstance().SearchRadius,
                    GPUGlobalParameterManager.GetInstance().ParticleVolume,
                    GPUGlobalParameterManager.GetInstance().TimeStep,
                    GPUGlobalParameterManager.GetInstance().Viscosity,
                    GPUGlobalParameterManager.GetInstance().SurfaceTension,
                    GPUGlobalParameterManager.GetInstance().Gravity,
                    DivergenceIterationCount, PressureIterationCount
                );
            Profiler.EndSample();
        }
    }
}
