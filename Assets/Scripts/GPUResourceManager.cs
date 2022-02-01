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
        public ComputeBuffer ParticleCellIndexBuffer;
        public ComputeBuffer ParticleIndirectArgumentBuffer;
        public float ParticleRadius;
        public uint MaxParticleSize;

        public ParticleBuffer(uint vParticleBufferSize, float vParticleRadius, uint vDimension)
        {
            ParticlePositionBuffer = new ComputeBuffer((int)vParticleBufferSize, (int)vDimension * sizeof(float));
            ParticleVelocityBuffer = new ComputeBuffer((int)vParticleBufferSize, (int)vDimension * sizeof(float));
            ParticleDensityBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(float));
            ParticleCellIndexBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(uint));
            ParticleIndirectArgumentBuffer = new ComputeBuffer(7, sizeof(int));
            int[] ParticleIndirectArgumentCPU = new int[7] { Mathf.CeilToInt((float)vParticleBufferSize / GPUGlobalParameterManager.GetInstance().SPHThreadSize), 1, 1, 3, 0, 0, 0 };
            ParticleIndirectArgumentBuffer.SetData(ParticleIndirectArgumentCPU);
            ParticleRadius = vParticleRadius;
            MaxParticleSize = vParticleBufferSize;
        }

        ~ParticleBuffer()
        {
            ParticlePositionBuffer.Release();
            ParticleVelocityBuffer.Release();
            ParticleDensityBuffer.Release();
            ParticleCellIndexBuffer.Release();
            ParticleIndirectArgumentBuffer.Release();
        }
    }

    public class GPUResourceManager : Singleton<GPUResourceManager>
    {
        public ParticleBuffer DynamicSWPParticle;
        public ParticleBuffer Dynamic3DParticle;
        public GPUResourceManager()
        {
            Vector2Int SWPRes = GPUGlobalParameterManager.GetInstance().SWPReolution;
            DynamicSWPParticle = new ParticleBuffer(
                (uint)SWPRes.x * (uint)SWPRes.y,
                GPUGlobalParameterManager.GetInstance().SWPParticleRadius,
                2);

            Dynamic3DParticle = new ParticleBuffer(
                GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius,
                3);
        }
    }
}