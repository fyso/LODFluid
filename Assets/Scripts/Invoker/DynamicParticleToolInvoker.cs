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
        private int ScatterParticleDataKernel;
        private int UpdateParticleNarrowCountArgmentKernel;

        public DynamicParticleToolInvoker()
        {
            DynamicParticleToolCS = Resources.Load<ComputeShader>("DynamicParticleTool");
            AddParticleBlockKernel = DynamicParticleToolCS.FindKernel("addParticleBlock");
            UpdateParticleCountArgmentKernel = DynamicParticleToolCS.FindKernel("updateParticleCountArgment");
            ScatterParticleDataKernel = DynamicParticleToolCS.FindKernel("scatterParticleData");
            UpdateParticleNarrowCountArgmentKernel = DynamicParticleToolCS.FindKernel("updateParticleNarrowCountArgment");
        }

        public void AddParticleBlock(
            ParticleBuffer voTarget,
            ComputeBuffer voParticleIndirectArgumentBuffer,
            ComputeBuffer voParticleFilterBuffer,
            Vector3 vWaterGeneratePos,
            Vector3Int vWaterBlockRes)
        {
            int AddedParticleCount = vWaterBlockRes.x * vWaterBlockRes.y * vWaterBlockRes.z;
            DynamicParticleToolCS.SetFloats("WaterGeneratePos", vWaterGeneratePos.x, vWaterGeneratePos.y, vWaterGeneratePos.z);
            DynamicParticleToolCS.SetInt("WaterBlockResX", vWaterBlockRes.x);
            DynamicParticleToolCS.SetInt("WaterBlockResY", vWaterBlockRes.y);
            DynamicParticleToolCS.SetInt("WaterBlockResZ", vWaterBlockRes.z);
            DynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            DynamicParticleToolCS.SetInt("MaxParticleCount", (int)voTarget.MaxParticleSize);
            DynamicParticleToolCS.SetFloat("ParticleRadius", voTarget.ParticleRadius);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleCountArgment_RW", voParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticlePosition_RW", voTarget.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleVelocity_RW", voTarget.ParticleVelocityBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleFilter_RW", voParticleFilterBuffer);
            DynamicParticleToolCS.Dispatch(AddParticleBlockKernel, (int)Mathf.Ceil((float)AddedParticleCount / GPUGlobalParameterManager.GetInstance().SPHThreadSize), 1, 1);
            
            DynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            DynamicParticleToolCS.SetInt("MaxParticleCount", (int)voTarget.MaxParticleSize);
            DynamicParticleToolCS.SetBuffer(UpdateParticleCountArgmentKernel, "ParticleCountArgment_RW", voParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.Dispatch(UpdateParticleCountArgmentKernel, 1, 1, 1);
        }

        public void NarrowParticleData(
            ParticleBuffer vTargetParticleBuffer,
            ParticleBuffer voNarrowParticleBuffer,
            ComputeBuffer vParticleIndirectArgumentBuffer,
            ComputeBuffer vParticleFilterBuffer,
            ComputeBuffer vParticleScatterOffsetCache)
        {
            GPUOperation.GetInstance().Scan(
                vParticleFilterBuffer.count,
                vParticleFilterBuffer,
                vParticleScatterOffsetCache,
                GPUResourceManager.GetInstance().ScanTempBuffer1,
                GPUResourceManager.GetInstance().ScanTempBuffer2);

            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "ParticleScatterOffset_R", vParticleScatterOffsetCache);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticlePosition_RW", voNarrowParticleBuffer.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleVelocity_RW", voNarrowParticleBuffer.ParticleVelocityBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticlePosition_R", vTargetParticleBuffer.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleVelocity_R", vTargetParticleBuffer.ParticleVelocityBuffer);
            DynamicParticleToolCS.DispatchIndirect(ScatterParticleDataKernel, vParticleIndirectArgumentBuffer);

            DynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleScatterOffset_R", vParticleScatterOffsetCache);
            DynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleCountArgment_RW", vParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.Dispatch(UpdateParticleNarrowCountArgmentKernel, 1, 1, 1);

        }
    }
}
