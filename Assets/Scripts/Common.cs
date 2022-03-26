using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    class Common
    {
        public static uint SPHThreadCount = 512;
        public static uint SWThreadCount = 32;
        public static uint ParticleCountArgumentOffset = 4;
        public static uint ParticleXGridCountArgumentOffset = 0;

        public static void SwapComputeBuffer(ref ComputeBuffer Buffer1, ref ComputeBuffer Buffer2)
        {
            ComputeBuffer Temp = Buffer1;
            Buffer1 = Buffer2;
            Buffer2 = Temp;
        }
    }
}
