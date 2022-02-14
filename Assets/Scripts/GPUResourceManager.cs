using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class ParticleBuffer
    {
        public ComputeBuffer ParticlePositionBuffer;
        public ComputeBuffer ParticleVelocityBuffer;
        public ComputeBuffer ParticleDensityBuffer;
        public float ParticleRadius;
        public uint MaxParticleSize;

        public ParticleBuffer(uint vParticleBufferSize, float vParticleRadius, uint vDimension)
        {
            ParticlePositionBuffer = new ComputeBuffer((int)vParticleBufferSize, (int)vDimension * sizeof(float));
            ParticleVelocityBuffer = new ComputeBuffer((int)vParticleBufferSize, (int)vDimension * sizeof(float));
            ParticleDensityBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(float));
            ParticleRadius = vParticleRadius;
            MaxParticleSize = vParticleBufferSize;
        }

        ~ParticleBuffer()
        {
            ParticlePositionBuffer.Release();
            ParticleVelocityBuffer.Release();
            ParticleDensityBuffer.Release();
        }
    }

    public class GPUResourceManager : Singleton<GPUResourceManager>
    {
        public ParticleBuffer Dynamic3DParticle;
        public ParticleBuffer DynamicSorted3DParticle;
        public ComputeBuffer Dynamic3DParticleIndirectArgumentBuffer;
        public ComputeBuffer Dynamic3DParticleCellIndexBuffer;
        public ComputeBuffer Dynamic3DParticleInnerSortBuffer;

        public ComputeBuffer HashGridCellParticleCountBuffer;
        public ComputeBuffer HashGridCellParticleOffsetBuffer;

        public ComputeBuffer ScanTempBuffer1;
        public ComputeBuffer ScanTempBuffer2;

        public GPUResourceManager()
        {
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

            Dynamic3DParticleCellIndexBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(uint));

            Dynamic3DParticleInnerSortBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(uint));

            Vector3 HashDia = GPUGlobalParameterManager.GetInstance().SimualtionRangeMax - GPUGlobalParameterManager.GetInstance().SimualtionRangeMin;
            float HashCellLength = GPUGlobalParameterManager.GetInstance().HashCellLength;
            int HashCellCount = Mathf.CeilToInt(HashDia.x / HashCellLength) * Mathf.CeilToInt(HashDia.y / HashCellLength) * Mathf.CeilToInt(HashDia.z / HashCellLength);
            HashGridCellParticleCountBuffer = new ComputeBuffer(HashCellCount, sizeof(uint));
            HashGridCellParticleOffsetBuffer = new ComputeBuffer(HashCellCount, sizeof(uint));

            uint SPhThreadSize = GPUGlobalParameterManager.GetInstance().SPHThreadSize;
            ScanTempBuffer1 = new ComputeBuffer((int)SPhThreadSize * (int)SPhThreadSize, sizeof(uint));
            ScanTempBuffer2 = new ComputeBuffer((int)SPhThreadSize, sizeof(uint));
        }

        ~GPUResourceManager()
        {
            Dynamic3DParticleIndirectArgumentBuffer.Release();
            Dynamic3DParticleCellIndexBuffer.Release();
            Dynamic3DParticleInnerSortBuffer.Release();
            HashGridCellParticleCountBuffer.Release();
            HashGridCellParticleOffsetBuffer.Release();
            ScanTempBuffer1.Release();
            ScanTempBuffer2.Release();
        }
    }
}