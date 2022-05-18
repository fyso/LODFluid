using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class DFSPHSimulator : MonoBehaviour
    {
        public Vector3 SimulationRangeMin = new Vector3(0, 0, 0);
        public Vector3Int SimulationRangeRes = new Vector3Int(64, 64, 64);
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(8, 1, 8);
        public Vector3 WaterGenerateInitVelocity = new Vector3(0, 0, 0);
        public Material SPHVisualMaterial;
        public List<GameObject> BoundaryObjects;

        [Range(0.025f, 0.25f)]
        public float ParticleRadius = 0.25f;

        [Range(0.005f, 0.05f)]
        public float TimeStep = 0.016666667f;

        [Range(0, 0.03f)]
        public float Viscosity = 0.01f;

        [Range(0, 0.1f)]
        public float SurfaceTension = 0.05f;

        [Range(0, 10f)]
        public float Gravity = 9.8f;

        [Range(100000, 300000)]
        public uint MaxParticleCount = 250000;

        public int DivergenceIterationCount = 3;
        public int PressureIterationCount = 1;

        private DivergenceFreeSPHSolver DFSPH;

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

        void Start()
        {
            DFSPH = new DivergenceFreeSPHSolver(BoundaryObjects, MaxParticleCount, SimulationRangeMin, SimulationRangeRes, ParticleRadius);
        }

        private bool Emit = false;
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Emit = !Emit;

            if (Emit && Time.frameCount % 10 == 0)
            {
                DFSPH.AddParticleBlock(
                    WaterGeneratePosition,
                    WaterGenerateResolution,
                    WaterGenerateInitVelocity);
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                uint[] ArgumentCPU = new uint[7];
                DFSPH.Dynamic3DParticleIndirectArgumentBuffer.GetData(ArgumentCPU);
                Debug.Log(ArgumentCPU[4]);
            }
        }

        private void FixedUpdate()
        {
            DFSPH.Solve(DivergenceIterationCount, PressureIterationCount, TimeStep, Viscosity, SurfaceTension, Gravity);
            DFSPH.Advect(TimeStep);
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
