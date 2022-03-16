using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class HybridSimulator : MonoBehaviour
    {
        [Header("Divergence-free SPH")]
        public Vector3 SimulationRangeMin = new Vector3(0, 0, 0);
        public Vector3Int SimulationRangeRes = new Vector3Int(64, 64, 64);
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(8, 1, 8);
        public Vector3 WaterGenerateInitVelocity = new Vector3(0, 0, 0);
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

        public int DivergenceIterationCount = 3;
        public int PressureIterationCount = 3;

        [Header("Shallow Water")]
        public Material[] Materials;
        public ComputeShader ShallowWaterShader;
        public Texture2D InitialState;
        public Material InitHeightMap;

        // Rendering stuff
        private const string StateTextureKey = "_StateTex";

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

        void OnRenderObject()
        {
            SPHVisualMaterial.SetPass(0);
            SPHVisualMaterial.SetFloat("_ParticleRadius", GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius);
            SPHVisualMaterial.SetBuffer("_particlePositionBuffer", GPUResourceManager.GetInstance().Dynamic3DParticle.ParticlePositionBuffer);
            SPHVisualMaterial.SetBuffer("_particleVelocityBuffer", GPUResourceManager.GetInstance().Dynamic3DParticle.ParticleVelocityBuffer);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer, 12);
        }

        private void Start()
        {
            Camera.main.depthTextureMode = DepthTextureMode.Depth;
            if (InitialState != null)
            {
                if (InitHeightMap != null)
                    Graphics.Blit(InitialState, GPUResourceManager.GetInstance().ShallowWaterResources.StateTexture, InitHeightMap);
                else
                    Graphics.Blit(InitialState, GPUResourceManager.GetInstance().ShallowWaterResources.StateTexture);
            }

            foreach (var material in Materials)
            {
                material.SetTexture(StateTextureKey, GPUResourceManager.GetInstance().ShallowWaterResources.StateTexture);
            }

            VolumeMapBoundarySolverInvoker.GetInstance().GenerateBoundaryMapData(
                BoundaryObjects,
                GPUResourceManager.GetInstance().Volume,
                GPUResourceManager.GetInstance().SignedDistance,
                GPUGlobalParameterManager.GetInstance().SearchRadius,
                GPUGlobalParameterManager.GetInstance().CubicZero);
        }
        bool OK = false;
        uint Count = 0;
        private void Update()
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
                    WaterGenerateResolution,
                    WaterGenerateInitVelocity);
                OK = true;
            }
            if (OK)
                Count++;
            if(Count == 1200)
            {
                //Time.timeScale = 0.0f;
            }
        }

        private void FixedUpdate()
        {
            //Shallow Water Step
            ShallowWaterSolverInvoker.GetInstance().Solve(
                GPUResourceManager.GetInstance().ShallowWaterResources.StateTexture,
                GPUResourceManager.GetInstance().ShallowWaterResources.VelocityTexture,
                GPUResourceManager.GetInstance().ShallowWaterResources.WaterOutFluxTexture,
                GPUGlobalParameterManager.GetInstance().ShallowWaterReolution,
                GPUGlobalParameterManager.GetInstance().TimeStep,
                GPUGlobalParameterManager.GetInstance().Gravity,
                GPUGlobalParameterManager.GetInstance().ShallowWaterPipeArea,
                GPUGlobalParameterManager.GetInstance().ShallowWaterPipeLength,
                GPUGlobalParameterManager.GetInstance().ShallowWaterCellLength);

            //SPH Step
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
                    DivergenceIterationCount, PressureIterationCount
                );
            Profiler.EndSample();

            HybridInvoker.GetInstance().CoupleParticleAndGrid(
                GPUResourceManager.GetInstance().Dynamic3DParticle,
                GPUResourceManager.GetInstance().ShallowWaterResources,
                GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                GPUGlobalParameterManager.GetInstance().TimeStep,
                GPUGlobalParameterManager.GetInstance().BandWidth,
                GPUGlobalParameterManager.GetInstance().ShallowWaterMin,
                GPUGlobalParameterManager.GetInstance().ShallowWaterMax,
                GPUGlobalParameterManager.GetInstance().ShallowWaterCellLength);
        }
    }
}
