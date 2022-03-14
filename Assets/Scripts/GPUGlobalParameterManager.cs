using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class GPUGlobalParameterManager : Singleton<GPUGlobalParameterManager>
    {
        public Vector2Int ShallowWaterReolution = new Vector2Int(512, 512);
        public Vector2 ShallowWaterMin = new Vector2(0, 0);
        public float ShallowWaterCellLength = 1.0f;
        public float ShallowWaterParticleRadius = 0.25f;
        public float ShallowWaterTimeStep = 0.01666667f;
        public float ShallowWaterPipeLength = 1;
        public float ShallowWaterPipeArea = 5;

        public uint SPHThreadSize = 512;

        public uint Max3DParticleCount = 200000;
        public float Dynamic3DParticleRadius = 0.25f;
        public float TimeStep = 0.01666667f;
        public float Viscosity = 0.01f;
        public float SurfaceTension = 0.05f;
        public float Gravity = 9.8f;
        public float ParticleVolume { get { return 0.8f * Mathf.Pow(2.0f * Dynamic3DParticleRadius, 3.0f); } }
        public float SearchRadius { get { return Dynamic3DParticleRadius * 4.0f; } }
        public float CubicZero { get { return 8.0f / (Mathf.PI * Mathf.Pow(SearchRadius, 3.0f)); } }

        public Vector3 SimualtionRangeMin = new Vector3(0, 0, 0);
        public Vector3 SimualtionRangeRes = new Vector3(32, 16, 16);
        public float HashCellLength { get { return Dynamic3DParticleRadius * 4.0f; } }
        public Vector3 HashGridMin { get { return SimualtionRangeMin; } }
        public Vector3 HashGridMax { get { return SimualtionRangeRes * SearchRadius; } }
        public Vector3Int HashResolution { 
            get {
                Vector3 SimulationDia = HashGridMax - HashGridMin;
                int X = Mathf.CeilToInt(SimulationDia.x / HashCellLength);
                int Y = Mathf.CeilToInt(SimulationDia.y / HashCellLength);
                int Z = Mathf.CeilToInt(SimulationDia.z / HashCellLength);
                return new Vector3Int(X, Y, Z);
            } 
        }
    }
}