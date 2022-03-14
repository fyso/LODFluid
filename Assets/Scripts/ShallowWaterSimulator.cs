using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Utils;
using System;

namespace LODFluid
{   
    public class ShallowWaterSimulator : MonoBehaviour
    {
        [Header("References")]
        public Material[] Materials;
        public ComputeShader ShallowWaterShader;
        public Texture2D InitialState;
        public Material InitHeightMap;

        // Rendering stuff
        private const string StateTextureKey = "_StateTex";


        void Start()
        {
            Camera.main.depthTextureMode = DepthTextureMode.Depth;
            Initialize();
        }
        void Update()
        {
            ShallowWaterSolverInvoker.GetInstance().Solve(
                GPUResourceManager.GetInstance().ShallowWaterResources.StateTexture,
                GPUResourceManager.GetInstance().ShallowWaterResources.VelocityTexture,
                GPUResourceManager.GetInstance().ShallowWaterResources.WaterOutFluxTexture,
                GPUGlobalParameterManager.GetInstance().ShallowWaterReolution,
                GPUGlobalParameterManager.GetInstance().ShallowWaterTimeStep,
                GPUGlobalParameterManager.GetInstance().Gravity,
                GPUGlobalParameterManager.GetInstance().ShallowWaterPipeArea,
                GPUGlobalParameterManager.GetInstance().ShallowWaterPipeLength
                );
        }
        public void Initialize()
        {
            if (InitialState != null)
            {
                if (InitHeightMap != null)
                    Graphics.Blit(InitialState, GPUResourceManager.GetInstance().ShallowWaterResources.StateTexture, InitHeightMap);
                else
                    Graphics.Blit(InitialState, GPUResourceManager.GetInstance().ShallowWaterResources.StateTexture);
            }

            foreach (var material in Materials)
            {
                material.SetTexture(StateTextureKey, GPUResourceManager.GetInstance().ShallowWaterResources.StateTexture);
            }
        }
    }
}
