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
        public Vector3Int SimualtionRangeRes = new Vector3Int(32, 16, 16);
        public float HashCellLength { get { return Dynamic3DParticleRadius * 4.0f; } }
        public Vector3 HashGridMin { get { return SimualtionRangeMin; } }
        public Vector3 HashGridMax { get { return SimualtionRangeMin + (Vector3)SimualtionRangeRes * SearchRadius; } }
        public Vector3Int HashResolution { get { return SimualtionRangeRes; } }
    }
}