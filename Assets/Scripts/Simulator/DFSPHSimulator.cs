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
        public Vector3 WaterGenerateInitVelocity = new Vector3(0, 0, 0);
        public Material SPHVisualMaterial;
        public List<GameObject> BoundaryObjects;

        [Range(0.005f, 0.05f)]
        public float TimeStep = 0.016666667f;

        [Range(0, 0.03f)]
        public float Viscosity = 0.01f;

        [Range(0, 0.1f)]
        public float SurfaceTension = 0.05f;

        [Range(0, 10f)]
        public float Gravity = 9.8f;

        public int DivergenceIterationCount = 3;
        public int PressureIterationCount = 1;
        public bool UseVolumeMapBoundary = true;
        public bool UseEnforceBoundary = true;
        public bool DivergenceFreeIteration = true;

        private DynamicParticleToolInvoker DynamicParticleTool;
        private GPUCountingSortHash CompactNSearch;
        private DivergenceFreeSPHSolverInvoker DivergenceFreeSPHSolver;

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
            CompactNSearch = new GPUCountingSortHash(GPUGlobalParameterManager.GetInstance().Max3DParticleCount);
            DynamicParticleTool = new DynamicParticleToolInvoker(
                GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius);
            DivergenceFreeSPHSolver = new DivergenceFreeSPHSolverInvoker(GPUGlobalParameterManager.GetInstance().Max3DParticleCount);

            VolumeMapBoundarySolverInvoker.GetInstance().GenerateBoundaryMapData(
                BoundaryObjects,
                GPUResourceManager.GetInstance().Volume,
                GPUResourceManager.GetInstance().SignedDistance,
                GPUGlobalParameterManager.GetInstance().SearchRadius,
                GPUGlobalParameterManager.GetInstance().CubicZero);
        }

        private bool Emit = false;
        void Update()
        {
            GPUGlobalParameterManager.GetInstance().SimualtionRangeMin = SimulationRangeMin;
            GPUGlobalParameterManager.GetInstance().SimualtionRangeRes = SimulationRangeRes;
            GPUGlobalParameterManager.GetInstance().TimeStep = TimeStep;
            GPUGlobalParameterManager.GetInstance().Viscosity = Viscosity;
            GPUGlobalParameterManager.GetInstance().SurfaceTension = SurfaceTension;
            GPUGlobalParameterManager.GetInstance().Gravity = Gravity;

            if (Input.GetKeyDown(KeyCode.Space))
                Emit = !Emit;

            if (Emit && Time.frameCount % 10 == 0)
            {
                DynamicParticleTool.AddParticleBlock(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    WaterGeneratePosition,
                    WaterGenerateResolution,
                    WaterGenerateInitVelocity);
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                uint[] ArgumentCPU = new uint[7];
                GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer.GetData(ArgumentCPU);
                Debug.Log(ArgumentCPU[4]);
            }
        }

        private void FixedUpdate()
        {
            DFSPHSolve();
        }

        private void DFSPHSolve()
        {
            Profiler.BeginSample("Delete out of range particle");
            DynamicParticleTool.DeleteParticleOutofRange(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
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

            if (UseVolumeMapBoundary)
            {
                Profiler.BeginSample("Query closest point and volume");
                VolumeMapBoundarySolverInvoker.GetInstance().QueryClosestPointAndVolume(
                        GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                        GPUResourceManager.GetInstance().Dynamic3DParticle,
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
                EnforceBoundarySolverInvoker.GetInstance().ApplyBoundaryInfluence(
                        BoundaryObjects,
                        GPUResourceManager.GetInstance().Dynamic3DParticle,
                        GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                        GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius
                    );
                Profiler.EndSample();
            }

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
