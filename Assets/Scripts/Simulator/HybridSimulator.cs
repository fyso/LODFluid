using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class HybridSimulator : MonoBehaviour
    {
        private DFSPHSimulator m_DFSPHSimulator;
        private ShallowWaterSimulator m_ShallowWaterSimulator;

        private void Start()
        {
            m_DFSPHSimulator = GetComponent<DFSPHSimulator>();
            if (m_DFSPHSimulator == null)
            {
                Debug.LogError("no SPHSimulator!");
            }

            m_ShallowWaterSimulator = GetComponent<ShallowWaterSimulator>();
            if (m_ShallowWaterSimulator == null)
            {
                Debug.LogError("no ShallowWaterSimulator!");
            }
        }

        private void OnDrawGizmos()
        {
            //Vector3 SimulationMin = m_DFSPHSimulator.SimulationRangeMin;
            //Vector3 SimulationMax = (Vector3)m_DFSPHSimulator.SimulationRangeRes * GPUGlobalParameterManager.GetInstance().SearchRadius;
            //Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
            //Gizmos.DrawWireCube((SimulationMin + SimulationMax) * 0.5f, SimulationMax - SimulationMin);

            //float ParticleRaius = GPUGlobalParameterManager.GetInstance().Dynamic3DParticleRadius;
            //Vector3 WaterGenerateBlockMax = m_DFSPHSimulator.WaterGeneratePosition + new Vector3(m_DFSPHSimulator.WaterGenerateResolution.x * ParticleRaius * 2.0f, m_DFSPHSimulator.WaterGenerateResolution.y * ParticleRaius * 2.0f, m_DFSPHSimulator.WaterGenerateResolution.z * ParticleRaius * 2.0f);
            //Gizmos.color = new Color(1.0f, 1.0f, 0.0f);
            //Gizmos.DrawWireCube((m_DFSPHSimulator.WaterGeneratePosition + WaterGenerateBlockMax) * 0.5f, WaterGenerateBlockMax - m_DFSPHSimulator.WaterGeneratePosition);
        }
    }
}
