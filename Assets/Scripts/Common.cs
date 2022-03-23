using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    class Common
    {
        public static void SwapComputeBuffer(ref ComputeBuffer Buffer1, ref ComputeBuffer Buffer2)
        {
            ComputeBuffer Temp = Buffer1;
            Buffer1 = Buffer2;
            Buffer2 = Temp;
        }
    }
}
