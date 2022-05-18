using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class ParticleBuffer
    {
        public ComputeBuffer ParticlePositionBuffer;
        public ComputeBuffer ParticleVelocityBuffer;
        public ComputeBuffer ParticleFilterBuffer;
        public ComputeBuffer ParticleMortonCodeBuffer;
        public ComputeBuffer ParticleDensityBuffer;

        public ParticleBuffer(uint vParticleBufferSize)
        {
            ParticlePositionBuffer = new ComputeBuffer((int)vParticleBufferSize, 3 * sizeof(float));
            ParticleVelocityBuffer = new ComputeBuffer((int)vParticleBufferSize, 3 * sizeof(float));
            ParticleFilterBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(uint));
            ParticleMortonCodeBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(uint));
            ParticleDensityBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(float));
        }

        ~ParticleBuffer()
        {
            ParticlePositionBuffer.Release();
            ParticleVelocityBuffer.Release();
            ParticleFilterBuffer.Release();
            ParticleMortonCodeBuffer.Release();
            ParticleDensityBuffer.Release();
        }
    }

    public class DivergenceFreeSPHSolver
    {
        public ParticleBuffer Dynamic3DParticle;

        public List<CubicMap> SignedDistance;
        public List<CubicMap> Volume;

        public ComputeBuffer Dynamic3DParticleIndirectArgumentBuffer;

        public ComputeBuffer HashGridCellParticleCountBuffer;
        public ComputeBuffer HashGridCellParticleOffsetBuffer;

        public ComputeBuffer Dynamic3DParticleAlphaBuffer;
        public ComputeBuffer Dynamic3DParticleDensityChangeBuffer;
        public ComputeBuffer Dynamic3DParticleDensityAdvBuffer;
        public ComputeBuffer Dynamic3DParticleNormalBuffer;
        public ComputeBuffer Dynamic3DParticleClosestPointBuffer;
        public ComputeBuffer Dynamic3DParticleDistanceBuffer;
        public ComputeBuffer Dynamic3DParticleVolumeBuffer;
        public ComputeBuffer Dynamic3DParticleBoundaryVelocityBuffer;

        private DynamicParticle DynamicParticleTool;
        private GPUCountingSortHash CompactNSearchTool;
        private DivergenceFreeSPH DivergenceFreeSPHTool;
        private VolumeMapBoundary VolumeMapBoundaryTool;
        private List<GameObject> BoundaryObjects;

        public Vector3 SimulationRangeMin;
        public Vector3Int SimulationRangeRes;
        public float ParticleRadius;
        public Vector3 HashGridMin { get { return SimulationRangeMin; } }
        public Vector3Int HashGridRes { get { return SimulationRangeRes; } }
        public float HashGridCellLength { get { return ParticleRadius * 4.0f; } }
        public float SearchRadius { get { return ParticleRadius * 4.0f; } }
        public float ParticleVolume { get { return 0.8f * Mathf.Pow(2.0f * ParticleRadius, 3.0f); } }
        public float CubicZero { get { return 8.0f / (Mathf.PI * Mathf.Pow(SearchRadius, 3.0f)); } }

        public DivergenceFreeSPHSolver(
            List<GameObject> vBoundaryObjects, 
            uint vMaxParticleCount, 
            Vector3 vSimulationRangeMin,
            Vector3Int vSimulationRangeRes,
            float vParticleRadius)
        {
            BoundaryObjects = vBoundaryObjects;
            ParticleRadius = vParticleRadius;
            SimulationRangeMin = vSimulationRangeMin;
            SimulationRangeRes = vSimulationRangeRes;

            CompactNSearchTool = new GPUCountingSortHash(vMaxParticleCount);
            DynamicParticleTool = new DynamicParticle(vMaxParticleCount);
            DivergenceFreeSPHTool = new DivergenceFreeSPH(vMaxParticleCount);

            SignedDistance = new List<CubicMap>();
            Volume = new List<CubicMap>();

            Dynamic3DParticleIndirectArgumentBuffer = new ComputeBuffer(7, sizeof(int), ComputeBufferType.IndirectArguments);
            int[] ParticleIndirectArgumentCPU = new int[7] { 1, 1, 1, 3, 0, 0, 0 };
            Dynamic3DParticleIndirectArgumentBuffer.SetData(ParticleIndirectArgumentCPU);

            Dynamic3DParticle = new ParticleBuffer(vMaxParticleCount);

            Dynamic3DParticleAlphaBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleDensityChangeBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleDensityAdvBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleNormalBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            Dynamic3DParticleClosestPointBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            Dynamic3DParticleDistanceBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleVolumeBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleBoundaryVelocityBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);

            HashGridCellParticleCountBuffer = new ComputeBuffer((int)vMaxParticleCount * 2, sizeof(uint));
            HashGridCellParticleOffsetBuffer = new ComputeBuffer((int)vMaxParticleCount * 2, sizeof(uint));

            VolumeMapBoundaryTool = new VolumeMapBoundary();
            VolumeMapBoundaryTool.GenerateBoundaryMapData(
                vBoundaryObjects,
                Volume,
                SignedDistance,
                SearchRadius, CubicZero);
        }

        ~DivergenceFreeSPHSolver()
        {
            Dynamic3DParticleIndirectArgumentBuffer.Release();
            HashGridCellParticleCountBuffer.Release();
            HashGridCellParticleOffsetBuffer.Release();
            Dynamic3DParticleAlphaBuffer.Release();
            Dynamic3DParticleDensityChangeBuffer.Release();
            Dynamic3DParticleDensityAdvBuffer.Release();
            Dynamic3DParticleNormalBuffer.Release();
            Dynamic3DParticleClosestPointBuffer.Release();
            Dynamic3DParticleDistanceBuffer.Release();
            Dynamic3DParticleVolumeBuffer.Release();
            Dynamic3DParticleBoundaryVelocityBuffer.Release();
        }

        public void AddParticleBlock(Vector3 WaterGeneratePosition, Vector3Int WaterGenerateResolution, Vector3 WaterGenerateInitVelocity)
        {
            DynamicParticleTool.AddParticleBlock(
                Dynamic3DParticle,
                Dynamic3DParticleIndirectArgumentBuffer,
                WaterGeneratePosition,
                WaterGenerateResolution,
                WaterGenerateInitVelocity,
                ParticleRadius);
        }

        public void Solve(int DivergenceIterationCount, int PressureIterationCount, float vTimeStep, float vViscosity, float vSurfaceTension, float vGravity)
        {
            DynamicParticleTool.DeleteParticleOutofRange(
                    Dynamic3DParticle,
                    Dynamic3DParticleIndirectArgumentBuffer,
                    Dynamic3DParticleDistanceBuffer,
                    HashGridMin,
                    HashGridCellLength,
                    HashGridRes);

            DynamicParticleTool.NarrowParticleData(
                    ref Dynamic3DParticle,
                    Dynamic3DParticleIndirectArgumentBuffer);

            CompactNSearchTool.CountingHashSort(
                ref Dynamic3DParticle,
                HashGridCellParticleCountBuffer,
                HashGridCellParticleOffsetBuffer,
                Dynamic3DParticleIndirectArgumentBuffer,
                HashGridMin,
                HashGridCellLength);

            VolumeMapBoundaryTool.QueryClosestPointAndVolume(
                    Dynamic3DParticleIndirectArgumentBuffer,
                    Dynamic3DParticle,
                    BoundaryObjects,
                    Volume,
                    SignedDistance,
                    Dynamic3DParticleClosestPointBuffer,
                    Dynamic3DParticleDistanceBuffer,
                    Dynamic3DParticleVolumeBuffer,
                    Dynamic3DParticleBoundaryVelocityBuffer,
                    SearchRadius,
                    ParticleRadius);

            DivergenceFreeSPHTool.Slove(
                    ref Dynamic3DParticle,
                    Dynamic3DParticleIndirectArgumentBuffer,
                    HashGridCellParticleCountBuffer,
                    HashGridCellParticleOffsetBuffer,
                    Dynamic3DParticleAlphaBuffer,
                    Dynamic3DParticleDensityChangeBuffer,
                    Dynamic3DParticleDensityAdvBuffer,
                    Dynamic3DParticleNormalBuffer,
                    Dynamic3DParticleClosestPointBuffer,
                    Dynamic3DParticleVolumeBuffer,
                    Dynamic3DParticleBoundaryVelocityBuffer,
                    HashGridMin, HashGridCellLength, HashGridRes, SearchRadius, ParticleVolume,
                    vTimeStep, vViscosity, vSurfaceTension, vGravity,
                    DivergenceIterationCount, PressureIterationCount
                );
        }

        public void Advect(float vTimeStep)
        {
            DivergenceFreeSPHTool.Advect(
                    ref Dynamic3DParticle,
                    Dynamic3DParticleIndirectArgumentBuffer,
                    vTimeStep);
        }
    }
}
