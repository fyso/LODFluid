using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class DFSPHSimulator : MonoBehaviour
    {
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(8, 1, 8);
        public Vector3 WaterGenerateInitVelocity = new Vector3(0, 0, 0);
        public Material SPHVisualMaterial;
        public List<GameObject> BoundaryObjects;

        [Range(0, 0.05f)]
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
        private CompactNSearchInvoker CompactNSearch;
        private DivergenceFreeSPHSolverInvoker DivergenceFreeSPHSolver;

        private void OnDrawGizmos()
        {
            Vector3 SimulationMin = GPUGlobalParameterManager.GetInstance().SimualtionRangeMin;
            Vector3 SimulationMax = GPUGlobalParameterManager.GetInstance().SimualtionRangeMin + (Vector3)GPUGlobalParameterManager.GetInstance().SimualtionRangeRes * GPUGlobalParameterManager.GetInstance().SearchRadius;
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
            Gizmos.DrawWireCube((SimulationMin + SimulationMax) * 0.5f, SimulationMax - SimulationMin);

            float ParticleRaius = GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius;
            Vector3 WaterGenerateBlockMax = WaterGeneratePosition + new Vector3(WaterGenerateResolution.x * ParticleRaius * 2.0f, WaterGenerateResolution.y * ParticleRaius * 2.0f, WaterGenerateResolution.z * ParticleRaius * 2.0f);
            Gizmos.color = new Color(1.0f, 1.0f, 0.0f);
            Gizmos.DrawWireCube((WaterGeneratePosition + WaterGenerateBlockMax) * 0.5f, WaterGenerateBlockMax - WaterGeneratePosition);
        }

        void Start()
        {
            CompactNSearch = new CompactNSearchInvoker(GPUGlobalParameterManager.GetInstance().Max3DParticleCount);
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

            DynamicParticleTool.AddParticleBlock(
                GPUResourceManager.GetInstance().Dynamic3DParticle,
                GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                WaterGeneratePosition,
                WaterGenerateResolution,
                WaterGenerateInitVelocity);
        }

        void Update()
        {
            GPUGlobalParameterManager.GetInstance().TimeStep = TimeStep;
            GPUGlobalParameterManager.GetInstance().Viscosity = Viscosity;
            GPUGlobalParameterManager.GetInstance().SurfaceTension = SurfaceTension;
            GPUGlobalParameterManager.GetInstance().Gravity = Gravity;

            if (Input.GetKeyDown(KeyCode.Space)/* && Time.frameCount % 20 == 0*/)
            {
                DynamicParticleTool.AddParticleBlock(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    WaterGeneratePosition,
                    WaterGenerateResolution,
                    WaterGenerateInitVelocity);
            }
        }

        /* compute morton code */
        private uint expandBits3D(uint v)
        {
            v &= 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
            v = (v ^ (v << 16)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
            v = (v ^ (v << 8)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
            v = (v ^ (v << 4)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
            v = (v ^ (v << 2)) & 0x09249249; // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
            return v;
        }

        private uint computeMorton3D(uint vCellIndex3DX, uint vCellIndex3DY, uint vCellIndex3DZ)
        {
            return ((expandBits3D(vCellIndex3DZ) << 2) +
                (expandBits3D(vCellIndex3DY) << 1) +
                expandBits3D(vCellIndex3DX));
        }

        private void FixedUpdate()
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

            Profiler.BeginSample("Compute MortonCode");
            CompactNSearch.ComputeMortonCode(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUGlobalParameterManager.GetInstance().HashGridMin,
                    GPUGlobalParameterManager.GetInstance().HashCellLength,
                    GPUGlobalParameterManager.GetInstance().HashResolution);
            Profiler.EndSample();

            if(Time.frameCount % 1 == 0)
            {
                Profiler.BeginSample("Sort by MortonCode");
                CompactNSearch.Sort(
                        ref GPUResourceManager.GetInstance().Dynamic3DParticle,
                        GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer);
                Profiler.EndSample();
            }

            Profiler.BeginSample("Generate hash data");
            CompactNSearch.GenerateHashData(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleOffsetBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleCountBuffer);
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
                for (int i = 0; i < 4; i++)
                {
                    EnforceBoundarySolverInvoker.GetInstance().ApplyBoundaryInfluence(
                            BoundaryObjects,
                            GPUResourceManager.GetInstance().Dynamic3DParticle,
                            GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                            GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius
                        );
                }
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
