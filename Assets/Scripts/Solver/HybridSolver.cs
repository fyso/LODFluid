using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class HybridSolver
    {
        Hybrid HybridTool;
        public HybridSolver()
        {
            HybridTool = new Hybrid();
        }

        public void CoupleParticleAndGrid(
            DivergenceFreeSPHSolver vDFSPH, 
            ShallowWaterSolver vShallowWater,
            float vHybridBandWidth,
            float vTimeStep)
        {
            HybridTool.CoupleParticleAndGrid(
                vDFSPH.Dynamic3DParticle,
                vShallowWater,
                vDFSPH.Dynamic3DParticleIndirectArgumentBuffer,
                vDFSPH.HashGridCellParticleCountBuffer,
                vDFSPH.HashGridCellParticleOffsetBuffer,
                vDFSPH.HashGridMin,
                vDFSPH.HashGridCellLength,
                vDFSPH.HashGridRes,
                vTimeStep, vHybridBandWidth, vShallowWater.Min, vShallowWater.Max, vShallowWater.CellLength);
        }
    }
}