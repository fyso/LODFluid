using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LODFluid
{
    public class Simulator : MonoBehaviour
    {
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(64, 64, 64);
        public Material SPHVisualMaterial;

        void Start()
        {
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                DynamicParticleToolInvoker.GetInstance().AddParticleBlock3D(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    WaterGeneratePosition,
                    WaterGenerateResolution);
                DynamicParticleToolInvoker.GetInstance().UpdateParticleCountArgment(
                    GPUResourceManager.GetInstance().Dynamic3DParticle,
                    WaterGenerateResolution.x * WaterGenerateResolution.y * WaterGenerateResolution.z);
            }
        }

        void OnRenderObject()
        {
            //TODO:DrawProceduralIndirectNow dont work
            int[] ParticleIndirectArgumentCPU = new int[7];
            GPUResourceManager.GetInstance().Dynamic3DParticle.ParticleIndirectArgumentBuffer.GetData(ParticleIndirectArgumentCPU);
            if (ParticleIndirectArgumentCPU[4] != 0)
            {
                SPHVisualMaterial.SetPass(0);
                SPHVisualMaterial.SetBuffer("_particlePositionBuffer", GPUResourceManager.GetInstance().Dynamic3DParticle.ParticlePositionBuffer);
                SPHVisualMaterial.SetBuffer("_particleVelocityBuffer", GPUResourceManager.GetInstance().Dynamic3DParticle.ParticleVelocityBuffer);
                //Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, GPUResourceManager.GetInstance().Dynamic3DParticle.ParticleIndirectArgumentBuffer, 12);
                Graphics.DrawProceduralNow(MeshTopology.Triangles, 3, (int)ParticleIndirectArgumentCPU[4]);
            }
        }
    }
}
