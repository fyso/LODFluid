using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class HybridSimulator : MonoBehaviour
    {
        [Header("Hybrid")]
        [Range(1, 5)]
        public uint HybridBandWidth = 5;

        [Header("Divergence-free SPH")]
        public Vector3 SimulationRangeMin = new Vector3(0, 0, 0);
        public Vector3Int SimulationRangeRes = new Vector3Int(64, 64, 64);
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(8, 1, 8);
        public Vector3 WaterGenerateInitVelocity = new Vector3(0, 0, 0);
        public bool DrawParticle = true;
        public Material SPHVisualMaterial;
        public List<GameObject> BoundaryObjects;

        [Range(0.005f, 0.05f)]
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

        [Header("Shallow Water")]
        public ChunkedPlane ChunkedPlane;
        public Texture2D TerrianHeightTexture;
        public Vector2 ShallowWaterMin = new Vector2(0, 0);

        public Vector2Int Resolution = new Vector2Int(512, 512);
        [Range(1.0f, 10.0f)]
        public float CellLength = 1.0f;

        public float PipeArea = 5.0f;
        public float PipeLength = 1.0f;

        private DivergenceFreeSPHSolver DFSPH;
        private ShallowWaterSolver ShallowWater;
        private HybridSolver Hybrid;

        private void Start()
        {
            DFSPH = new DivergenceFreeSPHSolver(BoundaryObjects, MaxParticleCount, SimulationRangeMin, SimulationRangeRes, ParticleRadius);
            ShallowWater = new ShallowWaterSolver(TerrianHeightTexture, ChunkedPlane, Resolution, CellLength, ShallowWaterMin);
            Hybrid = new HybridSolver();
            DFSPH.AddParticleBlock(WaterGeneratePosition, WaterGenerateResolution, WaterGenerateInitVelocity);
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
            ShallowWater.Solve(TimeStep, Gravity, PipeArea, PipeLength);
            DFSPH.Solve(DivergenceIterationCount, PressureIterationCount, TimeStep, Viscosity, SurfaceTension, 0.0f);
            Hybrid.CoupleParticleAndGrid(DFSPH, ShallowWater, HybridBandWidth * CellLength, TimeStep);
            DFSPH.Advect(TimeStep);
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
            if(DrawParticle)
            {
                SPHVisualMaterial.SetPass(0);
                SPHVisualMaterial.SetBuffer("_particlePositionBuffer", DFSPH.Dynamic3DParticle.ParticlePositionBuffer);
                SPHVisualMaterial.SetBuffer("_particleVelocityBuffer", DFSPH.Dynamic3DParticle.ParticleVelocityBuffer);
                Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, DFSPH.Dynamic3DParticleIndirectArgumentBuffer, 12);
            }
        }

    }
}
