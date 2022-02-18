using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class Simulator : MonoBehaviour
    {
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(8, 1, 8);
        public Material SPHVisualMaterial;
        public List<GameObject> BoundaryObjects;
        public int CurrrentParticleData = 0;
        public float TimeStep = 0.016666667f;
        public bool UseVolumeMapBoundary = true;
        public bool UseEnforceBoundary = true;

        private void OnDrawGizmos()
        {
            Vector3 SimulationMin = GPUGlobalParameterManager.GetInstance().SimualtionRangeMin;
            Vector3 SimulationMax = GPUGlobalParameterManager.GetInstance().SimualtionRangeMax;
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
            Gizmos.DrawWireCube((SimulationMin + SimulationMax) * 0.5f, SimulationMax - SimulationMin);

            float ParticleRaius = GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius;
            Vector3 WaterGenerateBlockMax = WaterGeneratePosition + new Vector3(WaterGenerateResolution.x * ParticleRaius * 2.0f, WaterGenerateResolution.y * ParticleRaius * 2.0f, WaterGenerateResolution.z * ParticleRaius * 2.0f);
            Gizmos.color = new Color(1.0f, 1.0f, 0.0f);
            Gizmos.DrawWireCube((WaterGeneratePosition + WaterGenerateBlockMax) * 0.5f, WaterGenerateBlockMax - WaterGeneratePosition);
        }

        void Start()
        {
            VolumeMapBoundarySloverInvoker.GetInstance().GenerateBoundaryMapData(
                BoundaryObjects,
                GPUResourceManager.GetInstance().Volume,
                GPUResourceManager.GetInstance().SignedDistance,
                GPUGlobalParameterManager.GetInstance().SearchRadius,
                GPUGlobalParameterManager.GetInstance().CubicZero);

            DynamicParticleToolInvoker.GetInstance().AddParticleBlock(
                GPUResourceManager.GetInstance().Dynamic3DParticle,
                GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                WaterGeneratePosition,
                WaterGenerateResolution);

            int[] ParticleIndirectArgumentCPU = new int[7];
            GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer.GetData(ParticleIndirectArgumentCPU);
            CurrrentParticleData = ParticleIndirectArgumentCPU[4];
        }

        void Update()
        {
            GPUGlobalParameterManager.GetInstance().TimeStep = TimeStep;

            if (Input.GetKey(KeyCode.Space) && Time.frameCount % 25 == 0)
            {
                DynamicParticleToolInvoker.GetInstance().AddParticleBlock(
                    GPUResourceManager.GetInstance().Dynamic3DParticle, 
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    WaterGeneratePosition,
                    WaterGenerateResolution);

                int[] ParticleIndirectArgumentCPU = new int[7];
                GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer.GetData(ParticleIndirectArgumentCPU);
                CurrrentParticleData = ParticleIndirectArgumentCPU[4];
            }

            Profiler.BeginSample("Counting sort");
            CompactNSearchInvoker.GetInstance().CountingSort(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleCountBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleOffsetBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleCellIndexBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleInnerSortBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityBuffer,
                    GPUResourceManager.GetInstance().ScanTempBuffer1,
                    GPUResourceManager.GetInstance().ScanTempBuffer2,
                    GPUGlobalParameterManager.GetInstance().HashGridMin,
                    GPUGlobalParameterManager.GetInstance().HashCellLength);
            Profiler.EndSample();

            if (UseVolumeMapBoundary)
            {
                Profiler.BeginSample("Query closest point and volume");
                VolumeMapBoundarySloverInvoker.GetInstance().QueryClosestPointAndVolume(
                        GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                        GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                        BoundaryObjects,
                        GPUResourceManager.GetInstance().Volume,
                        GPUResourceManager.GetInstance().SignedDistance,
                        GPUResourceManager.GetInstance().Dynamic3DParticleClosestPointAndVolumeBuffer,
                        GPUGlobalParameterManager.GetInstance().SearchRadius,
                        GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius);
                Profiler.EndSample();
            }

            if (UseEnforceBoundary)
            {
                Profiler.BeginSample("Apply boundary influence");
                EnforceBoundarySloverInvoker.GetInstance().ApplyBoundaryInfluence(
                        BoundaryObjects,
                        GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                        GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer
                    );
                Profiler.EndSample();
            }

            Profiler.BeginSample("Slove divergence-free SPH");
            DivergenceFreeSPHSloverInvoker.GetInstance().Slove(
                    GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleCountBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleOffsetBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleAlphaBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityChangeBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleDensityAdvBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleClosestPointAndVolumeBuffer,
                    GPUGlobalParameterManager.GetInstance().HashGridMin,
                    GPUGlobalParameterManager.GetInstance().HashCellLength,
                    GPUGlobalParameterManager.GetInstance().HashResolution,
                    GPUGlobalParameterManager.GetInstance().SearchRadius,
                    GPUGlobalParameterManager.GetInstance().ParticleVolume,
                    GPUGlobalParameterManager.GetInstance().TimeStep,
                    GPUGlobalParameterManager.GetInstance().Viscosity,
                    GPUGlobalParameterManager.GetInstance().Gravity,
                    UseVolumeMapBoundary,
                    3, 3
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
