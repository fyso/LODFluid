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
        public ComputeBuffer ParticleMortonCodeBuffer;

        public ParticleBuffer(uint vParticleBufferSize)
        {
            ParticlePositionBuffer = new ComputeBuffer((int)vParticleBufferSize, 3 * sizeof(float));
            ParticleVelocityBuffer = new ComputeBuffer((int)vParticleBufferSize, 3 * sizeof(float));
            ParticleFilterBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(uint));
            ParticleMortonCodeBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(uint));
        }

        ~ParticleBuffer()
        {
            ParticlePositionBuffer.Release();
            ParticleVelocityBuffer.Release();
            ParticleFilterBuffer.Release();
            ParticleMortonCodeBuffer.Release();
        }
    }

    public class ShallowWaterBuffer
    {
        // State texture ARGBFloat
        // R - surface height  [0, +inf]
        // G - water over surface height [0, +inf]
        // B - Suspended sediment amount [0, +inf]
        // A - Hardness of the surface [0, 1]
        public RenderTexture StateTexture;

        // represents how much water is OUTGOING in each direction LRTB
        public RenderTexture WaterOutFluxTexture;

        // R - X velocity ; G - Y-velocity
        public RenderTexture VelocityTexture;

        public RenderTexture ExternHeightTexture;
        
        public ShallowWaterBuffer(Vector2Int vResolution)
        {
            int Width = vResolution.x;
            int Height = vResolution.y;

            StateTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            WaterOutFluxTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            VelocityTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            ExternHeightTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        ~ShallowWaterBuffer()
        {
            StateTexture.Release();
            WaterOutFluxTexture.Release();
            VelocityTexture.Release();
            ExternHeightTexture.Release();
        }
    }

    public class GPUResourceManager : Singleton<GPUResourceManager>
    {
        public ParticleBuffer Dynamic3DParticle;

        public List<CubicMap> SignedDistance;
        public List<CubicMap> Volume;

        public ComputeBuffer Dynamic3DParticleIndirectArgumentBuffer;

        public ComputeBuffer HashGridCellParticleCountBuffer;
        public ComputeBuffer HashGridCellParticleOffsetBuffer;

        public ComputeBuffer Dynamic3DParticleDensityBuffer;
        public ComputeBuffer Dynamic3DParticleAlphaBuffer;
        public ComputeBuffer Dynamic3DParticleDensityChangeBuffer;
        public ComputeBuffer Dynamic3DParticleDensityAdvBuffer;
        public ComputeBuffer Dynamic3DParticleNormalBuffer;
        public ComputeBuffer Dynamic3DParticleClosestPointBuffer;
        public ComputeBuffer Dynamic3DParticleDistanceBuffer;
        public ComputeBuffer Dynamic3DParticleVolumeBuffer;
        public ComputeBuffer Dynamic3DParticleBoundaryVelocityBuffer;

        public ShallowWaterBuffer ShallowWaterResources;

        ~GPUResourceManager()
        {
            Dynamic3DParticleIndirectArgumentBuffer.Release();
            HashGridCellParticleCountBuffer.Release();
            HashGridCellParticleOffsetBuffer.Release();
            Dynamic3DParticleDensityBuffer.Release();
            Dynamic3DParticleAlphaBuffer.Release();
            Dynamic3DParticleDensityChangeBuffer.Release();
            Dynamic3DParticleDensityAdvBuffer.Release();
            Dynamic3DParticleNormalBuffer.Release();
            Dynamic3DParticleClosestPointBuffer.Release();
            Dynamic3DParticleDistanceBuffer.Release();
            Dynamic3DParticleVolumeBuffer.Release();
            Dynamic3DParticleBoundaryVelocityBuffer.Release();
        }

        public GPUResourceManager()
        {
            SignedDistance = new List<CubicMap>();
            Volume = new List<CubicMap>();

            Dynamic3DParticleIndirectArgumentBuffer = new ComputeBuffer(7, sizeof(int), ComputeBufferType.IndirectArguments);
            int[] ParticleIndirectArgumentCPU = new int[7] { 1, 1, 1, 3, 0, 0, 0 };
            Dynamic3DParticleIndirectArgumentBuffer.SetData(ParticleIndirectArgumentCPU);

            Dynamic3DParticle = new ParticleBuffer(GPUGlobalParameterManager.GetInstance().Max3DParticleCount);

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

            Dynamic3DParticleClosestPointBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float) * 3);

            Dynamic3DParticleDistanceBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float));

            Dynamic3DParticleVolumeBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float));

            Dynamic3DParticleBoundaryVelocityBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount,
                sizeof(float) * 3);

            HashGridCellParticleCountBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount * 2, 
                sizeof(uint));

            HashGridCellParticleOffsetBuffer = new ComputeBuffer(
                (int)GPUGlobalParameterManager.GetInstance().Max3DParticleCount * 2,
                sizeof(uint));

            ShallowWaterResources = new ShallowWaterBuffer(GPUGlobalParameterManager.GetInstance().ShallowWaterReolution);
        }
    }
}