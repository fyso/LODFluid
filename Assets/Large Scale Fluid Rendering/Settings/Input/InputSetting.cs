using UnityEngine;

[System.Serializable]
public class InputSetting
{
    [Disabled]
    public int m_ParticleCount;
    [Disabled]
    public int m_DiffuseParticleCount;

    [Space]
    [Range(0, 0.1f)]
    public float m_ParticlesRadius = 0.05f;

    [Space]
    public bool m_DrawEllipsoids = true;
    
    public void UpdateShaderProperty()
    {
    }
}