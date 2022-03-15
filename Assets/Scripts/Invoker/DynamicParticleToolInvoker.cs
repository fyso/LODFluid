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
        private int DeleteParticleOutofRangeKernel;

        public DynamicParticleToolInvoker()
        {
            DynamicParticleToolCS = Resources.Load<ComputeShader>("DynamicParticleTool");
            AddParticleBlockKernel = DynamicParticleToolCS.FindKernel("addParticleBlock");
            UpdateParticleCountArgmentKernel = DynamicParticleToolCS.FindKernel("updateParticleCountArgment");
            ScatterParticleDataKernel = DynamicParticleToolCS.FindKernel("scatterParticleData");
            UpdateParticleNarrowCountArgmentKernel = DynamicParticleToolCS.FindKernel("updateParticleNarrowCountArgment");
            DeleteParticleOutofRangeKernel = DynamicParticleToolCS.FindKernel("deleteParticleOutofRange");
        }

        public void AddParticleBlock(
            ParticleBuffer voTarget,
            ComputeBuffer voParticleIndirectArgumentBuffer,
            Vector3 vWaterGeneratePos,
            Vector3Int vWaterBlockRes,
            Vector3 vInitParticleVel)
        {
            int AddedParticleCount = vWaterBlockRes.x * vWaterBlockRes.y * vWaterBlockRes.z;
            DynamicParticleToolCS.SetFloats("WaterGeneratePos", vWaterGeneratePos.x, vWaterGeneratePos.y, vWaterGeneratePos.z);
            DynamicParticleToolCS.SetInt("WaterBlockResX", vWaterBlockRes.x);
            DynamicParticleToolCS.SetInt("WaterBlockResY", vWaterBlockRes.y);
            DynamicParticleToolCS.SetInt("WaterBlockResZ", vWaterBlockRes.z);
            DynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            DynamicParticleToolCS.SetInt("MaxParticleCount", (int)voTarget.MaxParticleSize);
            DynamicParticleToolCS.SetFloat("ParticleRadius", voTarget.ParticleRadius);
            DynamicParticleToolCS.SetFloats("ParticleInitVel", vInitParticleVel.x, vInitParticleVel.y, vInitParticleVel.z);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleIndrectArgment_RW", voParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticlePosition_RW", voTarget.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleVelocity_RW", voTarget.ParticleVelocityBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleFilter_RW", voTarget.ParticleFilterBuffer);
            DynamicParticleToolCS.Dispatch(AddParticleBlockKernel, (int)Mathf.Ceil((float)AddedParticleCount / GPUGlobalParameterManager.GetInstance().SPHThreadSize), 1, 1);
            
            DynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            DynamicParticleToolCS.SetInt("MaxParticleCount", (int)voTarget.MaxParticleSize);
            DynamicParticleToolCS.SetBuffer(UpdateParticleCountArgmentKernel, "ParticleIndrectArgment_RW", voParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.Dispatch(UpdateParticleCountArgmentKernel, 1, 1, 1);
        }
        public void DeleteParticleOutofRange(
             ParticleBuffer voTarget,
             ComputeBuffer vParticleIndirectArgumentBuffer,
             Vector3 vHashGridMin,
             float vHashGridCellLength,
             Vector3Int vHashGridResolution)
        {
            DynamicParticleToolCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            DynamicParticleToolCS.SetFloat("HashGridCellLength", vHashGridCellLength);
            DynamicParticleToolCS.SetInts("HashGridResolution", vHashGridResolution.x, vHashGridResolution.y, vHashGridResolution.z);
            DynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleFilter_RW", voTarget.ParticleFilterBuffer);
            DynamicParticleToolCS.DispatchIndirect(DeleteParticleOutofRangeKernel, vParticleIndirectArgumentBuffer);
        }

        public void NarrowParticleData(
            ParticleBuffer vTargetParticleBuffer,
            ParticleBuffer voNarrowParticleBuffer,
            ComputeBuffer vParticleIndirectArgumentBuffer,
            ComputeBuffer vParticleScatterOffsetCache)
        {
            GPUOperation.GetInstance().Scan(
                vTargetParticleBuffer.ParticleFilterBuffer.count,
                vTargetParticleBuffer.ParticleFilterBuffer,
                vParticleScatterOffsetCache,
                GPUResourceManager.GetInstance().ScanTempBuffer1,
                GPUResourceManager.GetInstance().ScanTempBuffer2);

            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "ParticleScatterOffset_R", vParticleScatterOffsetCache);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticlePosition_RW", voNarrowParticleBuffer.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleVelocity_RW", voNarrowParticleBuffer.ParticleVelocityBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleFilter_RW", voNarrowParticleBuffer.ParticleFilterBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticlePosition_R", vTargetParticleBuffer.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleVelocity_R", vTargetParticleBuffer.ParticleVelocityBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleFilter_R", vTargetParticleBuffer.ParticleFilterBuffer);
            DynamicParticleToolCS.DispatchIndirect(ScatterParticleDataKernel, vParticleIndirectArgumentBuffer);

            DynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleScatterOffset_R", vParticleScatterOffsetCache);
            DynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleIndrectArgment_RW", vParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.Dispatch(UpdateParticleNarrowCountArgmentKernel, 1, 1, 1);

        }
    }
}
