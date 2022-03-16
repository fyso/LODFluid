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

        // Brush
        private float BrushRadius = 0.05f;
        private float BrushX;
        private float BrushY;
        private Plane Floor = new Plane(Vector3.up, Vector3.zero);
        private Vector4 InputControls;

        void Start()
        {
            Camera.main.depthTextureMode = DepthTextureMode.Depth;
            Initialize();
        }
        void Update()
        {
            BrushRadius = Mathf.Clamp(BrushRadius + Input.mouseScrollDelta.y * Time.deltaTime * 0.2f, 0.01f, 1f);

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var amount = 0f;
            if(Floor.Raycast(ray, out var enter))
            {
                var hitPoint = ray.GetPoint(enter);
                BrushX = hitPoint.x / GPUGlobalParameterManager.GetInstance().ShallowWaterReolution.x;
                BrushY = hitPoint.z / GPUGlobalParameterManager.GetInstance().ShallowWaterReolution.y;

                if (Input.GetMouseButton(0)) amount = 8;
            }
            else
            {
                amount = 0;
            }
            InputControls = new Vector4(BrushX, BrushY, BrushRadius, amount);

        }

        void FixedUpdate()
        {
            ShallowWaterSolverInvoker.GetInstance().AddWater(
                GPUResourceManager.GetInstance().ShallowWaterResources.StateTexture,
                InputControls,
                Time.fixedDeltaTime,
                GPUGlobalParameterManager.GetInstance().ShallowWaterReolution
                );

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
