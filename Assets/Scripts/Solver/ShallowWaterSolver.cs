using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class ShallowWaterSolver
    {
        // State texture ARGBFloat
        // R - surface height  [0, +inf]
        // G - water over surface height [0, +inf]
        // B - Suspended sediment amount [0, +inf]
        // A - Hardness of the surface [0, 1]
        public RenderTexture StateTexture;

        // represents how much water is OUTGOING in each direction LRTB
        public RenderTexture WaterOutFluxTexture;

        // R - X velocity ; G - Z-velocity
        public RenderTexture VelocityTexture;

        public RenderTexture ExternHeightTexture;
        public RenderTexture HeightChangeTexture;

        public Material InitHeightMaterial;

        public Vector2 Min;
        public Vector2 Max { get { return Min + (Vector2)Resolution * CellLength; } }
        public Vector2Int Resolution;
        public float CellLength;

        private ShallowWaterSolverInvoker ShallowWater;

        public ShallowWaterSolver(Texture2D vTerrianHeightMap, ChunkedPlane vTargetChunkedPlane, Vector2Int vResolution, float vCellLength, Vector2 vShallowWaterMin)
        {
            Min = vShallowWaterMin;
            Resolution = vResolution;
            CellLength = vCellLength;
            int Width = vResolution.x;
            int Height = vResolution.y;

            ShallowWater = new ShallowWaterSolverInvoker();

            InitHeightMaterial = Resources.Load<Material>("Materials/InitStateTexture");

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

            HeightChangeTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Camera.main.depthTextureMode = DepthTextureMode.Depth;
            if (vTerrianHeightMap != null)
            {
                if (InitHeightMaterial != null)
                    Graphics.Blit(vTerrianHeightMap, StateTexture, InitHeightMaterial);
                else
                    Graphics.Blit(vTerrianHeightMap, StateTexture);
            }

            foreach (var material in vTargetChunkedPlane.Materials)
            {
                material.SetTexture("_StateTex", StateTexture);
            }
        }

        public void Solve(float vTimeStep, float vGravity, float PipeArea, float vPipeLength)
        {
            ShallowWater.Solve(
                StateTexture,
                VelocityTexture,
                WaterOutFluxTexture,
                ExternHeightTexture,
                HeightChangeTexture,
                Resolution, vTimeStep, vGravity, PipeArea, vPipeLength, CellLength);
        }

        ~ShallowWaterSolver()
        {
            StateTexture.Release();
            WaterOutFluxTexture.Release();
            VelocityTexture.Release();
            ExternHeightTexture.Release();
        }

    }
}