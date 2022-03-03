using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class DFSPHSimulator : MonoBehaviour
    {
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(8, 1, 8);
        public Material SPHVisualMaterial;
        public List<GameObject> BoundaryObjects;
        public float TimeStep = 0.016666667f;
        public float Viscosity = 0.01f;
        public int DivergenceIterationCount = 6;
        public int PressureIterationCount = 6;
        public bool UseVolumeMapBoundary = true;
        public bool UseEnforceBoundary = true;
        public bool DivergenceFreeIteration = true;
        public bool FasterSlover = false;
        public bool LogSloverConverge = true;

        public int CurrrentParticleData = 0;

        private float DivergenceFreeErrorSum = 0;
        public float DivergenceFreeError = 0;

        private float PressureErrorSum = 0;
        public float PressureError = 0;

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
            GPUGlobalParameterManager.GetInstance().Viscosity = Viscosity;

            if (Input.GetKeyDown(KeyCode.Space))
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
                    EnforceBoundarySloverInvoker.GetInstance().ApplyBoundaryInfluence(
                            BoundaryObjects,
                            GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                            GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                            GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius
                        );
                }
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
                    GPUResourceManager.GetInstance().Dynamic3DParticleBoundaryVelocityBuffer,
                    GPUGlobalParameterManager.GetInstance().HashGridMin,
                    GPUGlobalParameterManager.GetInstance().HashCellLength,
                    GPUGlobalParameterManager.GetInstance().HashResolution,
                    GPUGlobalParameterManager.GetInstance().SearchRadius,
                    GPUGlobalParameterManager.GetInstance().ParticleVolume,
                    GPUGlobalParameterManager.GetInstance().TimeStep,
                    GPUGlobalParameterManager.GetInstance().Viscosity,
                    GPUGlobalParameterManager.GetInstance().Gravity,
                    UseVolumeMapBoundary,
                    DivergenceIterationCount, PressureIterationCount, DivergenceFreeIteration, FasterSlover
                );
            Profiler.EndSample();

            if (LogSloverConverge && Time.frameCount <= 500)
            {
                int[] ParticleIndirectArgumentCPU = new int[7];
                GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer.GetData(ParticleIndirectArgumentCPU);
                CurrrentParticleData = ParticleIndirectArgumentCPU[4];

                float[] DensityChange = new float[GPUGlobalParameterManager.GetInstance().Max3DParticleCount];
                GPUResourceManager.GetInstance().Dynamic3DParticleDensityChangeBuffer.GetData(DensityChange);
                float DensityChangeSum = 0.0f;
                for (int i = 0; i < CurrrentParticleData; i++)
                {
                    DensityChangeSum += Mathf.Abs(DensityChange[i]);
                }
                DivergenceFreeErrorSum = DensityChangeSum / CurrrentParticleData;
                DivergenceFreeError = DensityChangeSum / Time.frameCount;

                float[] DensityAdv = new float[GPUGlobalParameterManager.GetInstance().Max3DParticleCount];
                GPUResourceManager.GetInstance().Dynamic3DParticleDensityAdvBuffer.GetData(DensityAdv);
                float Density_error_Sum = 0.0f;
                for (int i = 0; i < CurrrentParticleData; i++)
                {
                    Density_error_Sum += Mathf.Abs(DensityAdv[i] - 1.0f);
                }
                PressureErrorSum += Density_error_Sum / CurrrentParticleData;
                PressureError = PressureErrorSum / Time.frameCount;
            }
        }

        void FixedUpdate()
        {
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
