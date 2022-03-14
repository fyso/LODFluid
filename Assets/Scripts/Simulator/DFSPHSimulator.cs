using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class DFSPHSimulator : MonoBehaviour
    {
        public Vector3 SimulationRangeMin = new Vector3(0, 0, 0);
        public Vector3Int SimulationRangeRes = new Vector3Int(64, 64, 64);
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(8, 1, 8);
        public Material SPHVisualMaterial;
        public List<GameObject> BoundaryObjects;

        [Range(0, 0.033f)]
        public float TimeStep = 0.016666667f;

        [Range(0, 0.03f)]
        public float Viscosity = 0.01f;

        [Range(0, 0.1f)]
        public float SurfaceTension = 0.05f;

        [Range(0, 10f)]
        public float Gravity = 9.8f;

        private int DivergenceIterationCount = 3;
        private int PressureIterationCount = 3;
        public bool UseVolumeMapBoundary = true;
        public bool UseEnforceBoundary = true;
        public bool DivergenceFreeIteration = true;

        private void OnDrawGizmos()
        {
            Vector3 SimulationMin = SimulationRangeMin;
            Vector3 SimulationMax = SimulationRangeMin + (Vector3)SimulationRangeRes * GPUGlobalParameterManager.GetInstance().SearchRadius;
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
            Gizmos.DrawWireCube((SimulationMin + SimulationMax) * 0.5f, SimulationMax - SimulationMin);

            float ParticleRaius = GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius;
            Vector3 WaterGenerateBlockMax = WaterGeneratePosition + new Vector3(WaterGenerateResolution.x * ParticleRaius * 2.0f, WaterGenerateResolution.y * ParticleRaius * 2.0f, WaterGenerateResolution.z * ParticleRaius * 2.0f);
            Gizmos.color = new Color(1.0f, 1.0f, 0.0f);
            Gizmos.DrawWireCube((WaterGeneratePosition + WaterGenerateBlockMax) * 0.5f, WaterGenerateBlockMax - WaterGeneratePosition);
        }

        void Start()
        {
            VolumeMapBoundarySolverInvoker.GetInstance().GenerateBoundaryMapData(
                BoundaryObjects,
                GPUResourceManager.GetInstance().Volume,
                GPUResourceManager.GetInstance().SignedDistance,
                GPUGlobalParameterManager.GetInstance().SearchRadius,
                GPUGlobalParameterManager.GetInstance().CubicZero);
        }

        void Update()
        {
            GPUGlobalParameterManager.GetInstance().SimualtionRangeMin = SimulationRangeMin;
            GPUGlobalParameterManager.GetInstance().SimualtionRangeRes = SimulationRangeRes;
            GPUGlobalParameterManager.GetInstance().TimeStep = TimeStep;
            GPUGlobalParameterManager.GetInstance().Viscosity = Viscosity;
            GPUGlobalParameterManager.GetInstance().SurfaceTension = SurfaceTension;
            GPUGlobalParameterManager.GetInstance().Gravity = Gravity;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                DynamicParticleToolInvoker.GetInstance().AddParticleBlock(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    WaterGeneratePosition,
                    WaterGenerateResolution);
            }
        }

        void FixedUpdate()
        {
            Profiler.BeginSample("Delete out of range particle");
            DynamicParticleToolInvoker.GetInstance().DeleteParticleOutofRange(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUGlobalParameterManager.GetInstance().HashGridMin,
                    GPUGlobalParameterManager.GetInstance().HashCellLength,
                    GPUGlobalParameterManager.GetInstance().HashResolution);
            Profiler.EndSample();

            Profiler.BeginSample("Narrow");
            DynamicParticleToolInvoker.GetInstance().NarrowParticleData(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().DynamicNarrow3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleScatterOffsetBuffer);
            Profiler.EndSample();

            Profiler.BeginSample("Counting sort");
            CompactNSearchInvoker.GetInstance().CountingSort(
                    GPUResourceManager.GetInstance().DynamicNarrow3DParticle,
                    GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleCountBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleOffsetBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleCellIndexBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleInnerSortBuffer,
                    GPUResourceManager.GetInstance().ScanTempBuffer1,
                    GPUResourceManager.GetInstance().ScanTempBuffer2,
                    GPUGlobalParameterManager.GetInstance().HashGridMin,
                    GPUGlobalParameterManager.GetInstance().HashCellLength,
                    GPUGlobalParameterManager.GetInstance().HashResolution);
            Profiler.EndSample();

            if (UseVolumeMapBoundary)
            {
                Profiler.BeginSample("Query closest point and volume");
                VolumeMapBoundarySolverInvoker.GetInstance().QueryClosestPointAndVolume(
                        GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                        GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                        BoundaryObjects,
                        GPUResourceManager.GetInstance().Volume,
                        GPUResourceManager.GetInstance().SignedDistance,
                        GPUResourceManager.GetInstance().Dynamic3DParticleClosestPointAndVolumeBuffer,
                        GPUResourceManager.GetInstance().Dynamic3DParticleBoundaryVelocityBuffer,
                        GPUGlobalParameterManager.GetInstance().SearchRadius,
                        GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius);
                Profiler.EndSample();
            }

            if (UseEnforceBoundary)
            {
                Profiler.BeginSample("Apply boundary influence");
                for (int i = 0; i < 4; i++)
                {
                    EnforceBoundarySolverInvoker.GetInstance().ApplyBoundaryInfluence(
                            BoundaryObjects,
                            GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                            GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                            GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius
                        );
                }
                Profiler.EndSample();
            }

            Profiler.BeginSample("Slove divergence-free SPH");
            DivergenceFreeSPHSolverInvoker.GetInstance().Slove(
                    GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleCountBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleOffsetBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleAlphaBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityChangeBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityAdvBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleNormalBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleClosestPointAndVolumeBuffer,
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
                    DivergenceIterationCount, PressureIterationCount,
                    UseVolumeMapBoundary, DivergenceFreeIteration
                );
            Profiler.EndSample();
        }

        void OnRenderObject()
        {
            SPHVisualMaterial.SetPass(0);
            SPHVisualMaterial.SetBuffer("_particlePositionBuffer", GPUResourceManager.GetInstance().Dynamic3DParticle.ParticlePositionBuffer);
            SPHVisualMaterial.SetBuffer("_particleVelocityBuffer", GPUResourceManager.GetInstance().Dynamic3DParticle.ParticleVelocityBuffer);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer, 12);
        }
    }
}
