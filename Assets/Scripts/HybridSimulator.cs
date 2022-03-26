using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class HybridSimulator : MonoBehaviour
    {
        [Header("Hybrid Factor")]
        [Range(5, 10)]
        public uint HybridBandWidth = 5;

        [Header("Divergence-free SPH")]
        public Vector3 SimulationRangeMin = new Vector3(0, 0, 0);
        public Vector3Int SimulationRangeRes = new Vector3Int(64, 64, 64);
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(8, 1, 8);
        public Vector3 WaterGenerateInitVelocity = new Vector3(0, 0, 0);
        public Material SPHVisualMaterial;
        public List<GameObject> BoundaryObjects;

        [Range(0, 0.033f)]
        public float TimeStep = 0.016666667f;
        [Range(0, 0.03f)]
        public float Viscosity = 0.01f;
        [Range(0, 0.1f)]
        public float SurfaceTension = 0.05f;
        [Range(0, 10.0f)]
        public float Gravity = 9.8f;
        [Range(100000, 300000)]
        public uint MaxParticleCount = 250000;
        [Range(0.025f, 0.25f)]
        public float ParticleRadius = 0.25f;

        public int DivergenceIterationCount = 3;
        public int PressureIterationCount = 3;

        private DivergenceFreeSPHSolver DFSPH;

        [Header("Shallow Water")]
        public Material[] Materials;
        public ComputeShader ShallowWaterShader;
        public Texture2D InitialState;
        public Material InitHeightMap;
        public Vector2 ShallowWaterMin = new Vector2(0, 0);

        public Vector2Int Resolution = new Vector2Int(512, 512);
        [Range(1.0f, 10.0f)]
        public float CellLength = 1.0f;

        public float PipeArea = 5.0f;
        public float PipeLength = 1.0f;

        private ShallowWaterSolver ShallowWater;

        private void Start()
        {
            DFSPH = new DivergenceFreeSPHSolver(BoundaryObjects, MaxParticleCount, SimulationRangeMin, SimulationRangeRes, ParticleRadius);
            ShallowWater = new ShallowWaterSolver(Resolution, CellLength, ShallowWaterMin);

            Camera.main.depthTextureMode = DepthTextureMode.Depth;
            if (InitialState != null)
            {
                if (InitHeightMap != null)
                    Graphics.Blit(InitialState, ShallowWater.StateTexture, InitHeightMap);
                else
                    Graphics.Blit(InitialState, ShallowWater.StateTexture);
            }

            foreach (var material in Materials)
            {
                material.SetTexture("_StateTex", ShallowWater.StateTexture);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                DFSPH.AddParticleBlock(WaterGeneratePosition, WaterGenerateResolution, WaterGenerateInitVelocity);
            }
        }

        private void FixedUpdate()
        {
            //Shallow Water Step
            ShallowWater.Solve(TimeStep, Gravity, PipeArea, PipeLength);

            //SPH Step
            DFSPH.Solve(DivergenceIterationCount, PressureIterationCount, TimeStep, Viscosity, SurfaceTension, Gravity);

            //Hybrid step
            HybridInvoker.GetInstance().CoupleParticleAndGrid(
                DFSPH.Dynamic3DParticle,
                ShallowWater,
                DFSPH.Dynamic3DParticleIndirectArgumentBuffer,
                DFSPH.HashGridCellParticleCountBuffer,
                DFSPH.HashGridMin,
                DFSPH.HashGridCellLength,
                DFSPH.HashGridRes,
                TimeStep, HybridBandWidth, ShallowWater.Min, ShallowWater.Max, ShallowWater.CellLength);
        }

        private void OnDrawGizmos()
        {
            Vector3 SimulationMin = SimulationRangeMin;
            Vector3 SimulationMax = SimulationRangeMin + (Vector3)SimulationRangeRes * ParticleRadius * 4.0f;
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
            Gizmos.DrawWireCube((SimulationMin + SimulationMax) * 0.5f, SimulationMax - SimulationMin);

            Vector3 WaterGenerateBlockMax = WaterGeneratePosition + new Vector3(WaterGenerateResolution.x * ParticleRadius * 2.0f, WaterGenerateResolution.y * ParticleRadius * 2.0f, WaterGenerateResolution.z * ParticleRadius * 2.0f);
            Gizmos.color = new Color(1.0f, 1.0f, 0.0f);
            Gizmos.DrawWireCube((WaterGeneratePosition + WaterGenerateBlockMax) * 0.5f, WaterGenerateBlockMax - WaterGeneratePosition);
        }

        void OnRenderObject()
        {
            SPHVisualMaterial.SetPass(0);
            SPHVisualMaterial.SetBuffer("_particlePositionBuffer", DFSPH.Dynamic3DParticle.ParticlePositionBuffer);
            SPHVisualMaterial.SetBuffer("_particleVelocityBuffer", DFSPH.Dynamic3DParticle.ParticleVelocityBuffer);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, DFSPH.Dynamic3DParticleIndirectArgumentBuffer, 12);
        }

    }
}
