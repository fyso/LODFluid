using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class Simulator : MonoBehaviour
    {
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(64, 64, 64);
        public Material SPHVisualMaterial;
        public List<GameObject> BoundaryObjects;

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                DynamicParticleToolInvoker.GetInstance().AddParticleBlock(
                    GPUResourceManager.GetInstance().Dynamic3DParticle, 
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    WaterGeneratePosition,
                    WaterGenerateResolution);
            }

            Profiler.BeginSample("CountingSort");
            CompactNSearchInvoker.GetInstance().CountingSort(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().DynamicSorted3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleCountBuffer,
                    GPUResourceManager.GetInstance().HashGridCellParticleOffsetBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleCellIndexBuffer,
                    GPUResourceManager.GetInstance().Dynamic3DParticleInnerSortBuffer,
                    GPUResourceManager.GetInstance().ScanTempBuffer1,
                    GPUResourceManager.GetInstance().ScanTempBuffer2,
                    GPUGlobalParameterManager.GetInstance().HashGridMin,
                    GPUGlobalParameterManager.GetInstance().HashCellLength);
            Profiler.EndSample();

            EnforceBoundarySloverInvoker.GetInstance().ApplyBoundaryInfluence(
                    BoundaryObjects,
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    GPUResourceManager.GetInstance().Dynamic3DParticleIndirectArgumentBuffer
                );
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
