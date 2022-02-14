using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class GPUGlobalParameterManager : Singleton<GPUGlobalParameterManager>
    {
        public uint SPHThreadSize = 512;

        public Vector2Int ShallowWaterReolution = new Vector2Int(512, 512);
        public Vector2 ShallowWaterMin = new Vector2(0, 0);
        public float ShallowWaterCellLength = 1.0f;
        public float ShallowWaterParticleRadius = 0.25f;

        public uint Max3DParticleCount = 500000;
        public float Dynamic3DParticleRadius = 0.25f;
        public Vector3 SimualtionRangeMin = new Vector3(-32, -16, -16);
        public Vector3 SimualtionRangeMax = new Vector3(32, 16, 16);

        public float SearchRadius { get { return Dynamic3DParticleRadius * 4.0f; } }
        public float HashCellLength { get { return Dynamic3DParticleRadius * 4.0f; } }
        public Vector3 HashGridMin { get { return SimualtionRangeMin; } }
    }
}