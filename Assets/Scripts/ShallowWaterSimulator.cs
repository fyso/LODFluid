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


        }
    }
}
