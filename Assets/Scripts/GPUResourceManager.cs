using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class ParticleBuffer
    {
        public ComputeBuffer ParticlePositionBuffer;
        public ComputeBuffer ParticleVelocityBuffer;
        public ComputeBuffer ParticleFilterBuffer;
        public float ParticleRadius;
        public uint MaxParticleSize;

        public ParticleBuffer(uint vParticleBufferSize, float vParticleRadius, uint vDimension)
        {
            ParticlePositionBuffer = new ComputeBuffer((int)vParticleBufferSize, (int)vDimension * sizeof(float));
            ParticleVelocityBuffer = new ComputeBuffer((int)vParticleBufferSize, (int)vDimension * sizeof(float));
            ParticleFilterBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(uint));
            ParticleRadius = vParticleRadius;
            MaxParticleSize = vParticleBufferSize;
        }

        ~ParticleBuffer()
        {
            ParticlePositionBuffer.Release();
            ParticleVelocityBuffer.Release();
            ParticleFilterBuffer.Release();
        }
    }

    public class GPUResourceManager : Singleton<GPUResourceManager>
    {
        public ParticleBuffer Dynamic3DParticle;
        public ParticleBuffer DynamicSorted3DParticle;
        public ParticleBuffer DynamicNarrow3DParticle;

        public List<CubicMap> SignedDistance;
        public List<CubicMap> Volume;

        public ComputeBuffer Dynamic3DParticleIndirectArgumentBuffer;

        public ComputeBuffer Dynamic3DParticleCellIndexBuffer;
        public ComputeBuffer Dynamic3DParticleInnerSortBuffer;

        public ComputeBuffer ScanTempBuffer1;
        public ComputeBuffer ScanTempBuffer2;

        public ComputeBuffer HashGridCellParticleCountBuffer;
        public ComputeBuffer HashGridCellParticleOffsetBuffer;

        public ComputeBuffer Dynamic3DParticleDensityBuffer;
        public ComputeBuffer Dynamic3DParticleAlphaBuffer;
        public ComputeBuffer Dynamic3DParticleDensityChangeBuffer;
        public ComputeBuffer Dynamic3DParticleDensityAdvBuffer;
        public ComputeBuffer Dynamic3DParticleNormalBuffer;
        public ComputeBuffer Dynamic3DParticleClosestPointAndVolumeBuffer;
        public ComputeBuffer Dynamic3DParticleBoundaryVelocityBuffer;
        public ComputeBuffer Dynamic3DParticleScatterOffsetBuffer;

        ~GPUResourceManager()
        {
            Dynamic3DParticleIndirectArgumentBuffer.Release();
            Dynamic3DParticleCellIndexBuffer.Release();
            Dynamic3DParticleInnerSortBuffer.Release();
            HashGridCellParticleCountBuffer.Release();
            HashGridCellParticleOffsetBuffer.Release();
            ScanTempBuffer1.Release();
            ScanTempBuffer2.Release();
            Dynamic3DParticleDensityBuffer.Release();
            Dynamic3DParticleAlphaBuffer.Release();
            Dynamic3DParticleDensityChangeBuffer.Release();
            Dynamic3DParticleDensityAdvBuffer.Release();
            Dynamic3DParticleNormalBuffer.Release();
            Dynamic3DParticleClosestPointAndVolumeBuffer.Release();
            Dynamic3DParticleBoundaryVelocityBuffer.Release();
            Dynamic3DParticleScatterOffsetBuffer.Release();
        }

        public GPUResourceManager()
        {
            SignedDistance = new List<CubicMap>();
            Volume = new List<CubicMap>();

            Dynamic3DParticleIndirectArgumentBuffer = new ComputeBuffer(7, sizeof(int), ComputeBufferType.IndirectArguments);
            int[] ParticleIndirectArgumentCPU = new int[7] { 1, 1, 1, 3, 0, 0, 0 };
            Dynamic3DParticleIndirectArgumentBuffer.SetData(ParticleIndirectArgumentCPU);

            Dynamic3DParticle = new ParticleBuffer(
                GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius,
                3);

            DynamicSorted3DParticle = new ParticleBuffer(
                GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius,
                3);

            DynamicNarrow3DParticle = new ParticleBuffer(
                GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius,
                3);

            Dynamic3DParticleCellIndexBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(uint));

            Dynamic3DParticleInnerSortBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(uint));

            Dynamic3DParticleDensityBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float));

            Dynamic3DParticleAlphaBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float));

            Dynamic3DParticleDensityChangeBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float));

            Dynamic3DParticleDensityAdvBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float));

            Dynamic3DParticleNormalBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float) * 3);

            Dynamic3DParticleClosestPointAndVolumeBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float) * 4);

            Dynamic3DParticleBoundaryVelocityBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float) * 3);

            Dynamic3DParticleScatterOffsetBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(uint));

            Vector3Int HashResolution = GPUGlobalParameterManager.GetInstance().HashResolution;
            int HashCellCount = HashResolution.x * HashResolution.y * HashResolution.z;
            HashGridCellParticleCountBuffer = new ComputeBuffer(HashCellCount, sizeof(uint));
            HashGridCellParticleOffsetBuffer = new ComputeBuffer(HashCellCount, sizeof(uint));

            uint SPhThreadSize = GPUGlobalParameterManager.GetInstance().SPHThreadSize;
            ScanTempBuffer1 = new ComputeBuffer((int)SPhThreadSize * (int)SPhThreadSize, sizeof(uint));
            ScanTempBuffer2 = new ComputeBuffer((int)SPhThreadSize, sizeof(uint));
        }
    }
}