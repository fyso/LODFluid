using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class GPUGlobalParameterManager : Singleton<GPUGlobalParameterManager>
    {
        public uint SPHThreadSize = 512;

        public Vector2Int SWPReolution = new Vector2Int(512, 512);
        public Vector2 SWPMin = new Vector2(0, 0);
        public float SWPParticleRadius = 0.25f;

        public uint Max3DParticleCount = 500000;
        public float Dynamic3DParticleRadius = 0.25f;
    }
}