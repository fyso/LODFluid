using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class DynamicParticleToolInvoker : Singleton<DynamicParticleToolInvoker>
    {
        private ComputeShader DynamicParticleToolCS;
        private int AddParticleBlockKernel;
        private int UpdateParticleCountArgmentKernel;
        public DynamicParticleToolInvoker()
        {
            DynamicParticleToolCS = Resources.Load<ComputeShader>("DynamicParticleTool");
            AddParticleBlockKernel = DynamicParticleToolCS.FindKernel("addParticleBlock");
            UpdateParticleCountArgmentKernel = DynamicParticleToolCS.FindKernel("updateParticleCountArgment");
        }

        public void AddParticleBlock(
            ParticleBuffer vTarget,
            ComputeBuffer vParticleIndirectArgumentBuffer,
            Vector3 vWaterGeneratePos,
            Vector3Int vWaterBlockRes)
        {
            int AddedParticleCount = vWaterBlockRes.x * vWaterBlockRes.y * vWaterBlockRes.z;
            DynamicParticleToolCS.SetFloats("WaterGeneratePos", vWaterGeneratePos.x, vWaterGeneratePos.y, vWaterGeneratePos.z);
            DynamicParticleToolCS.SetInt("WaterBlockResX", vWaterBlockRes.x);
            DynamicParticleToolCS.SetInt("WaterBlockResY", vWaterBlockRes.y);
            DynamicParticleToolCS.SetInt("WaterBlockResZ", vWaterBlockRes.z);
            DynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            DynamicParticleToolCS.SetInt("MaxParticleCount", (int)vTarget.MaxParticleSize);
            DynamicParticleToolCS.SetFloat("ParticleRadius", vTarget.ParticleRadius);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleCountArgment_RW", vParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticlePosition_RW", vTarget.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleVelocity_RW", vTarget.ParticleVelocityBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleDensity_RW", vTarget.ParticleDensityBuffer);
            DynamicParticleToolCS.Dispatch(AddParticleBlockKernel, (int)Mathf.Ceil((float)AddedParticleCount / GPUGlobalParameterManager.GetInstance().SPHThreadSize), 1, 1);
            
            DynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            DynamicParticleToolCS.SetInt("MaxParticleCount", (int)vTarget.MaxParticleSize);
            DynamicParticleToolCS.SetBuffer(UpdateParticleCountArgmentKernel, "ParticleCountArgment_RW", vParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.Dispatch(UpdateParticleCountArgmentKernel, 1, 1, 1);
        }

        public void NarrowParticleData(
            ParticleBuffer vTargetParticleBuffer,
            ParticleBuffer vNarrowParticleBuffer,
            ComputeBuffer vParticleIndirectArgumentBuffer,
            ComputeBuffer vParticleFilterBuffer,
            ComputeBuffer vParticleScatterOffsetCache)
        {

        }
    }
}
