using UnityEngine;

[System.Serializable]
public class FluidShadingSetting
{
    public Color m_Diffuse = new Color(0.0f, 0.1f, 0.7f, 1.0f);
    public Color m_GrazingDiffuse = new Color(0.2f, 0.4f, 0.6f);

    public float m_WaterIOF = 1.333f;
    
    [Range(0.0f, 2.0f)]
    public float m_DiffuseStrength = 1;  
    [Range(0.0f, 2.0f)]
    public float m_SpecularStrength = 1;  
    [Range(0.0f, 2.0f)]
    public float m_ReflecionStrength = 1;  
    [Range(0.0f, 2.0f)]
    public float m_RefractionStrength = 1;  
    [Range(0.0f, 30.0f)]
    public float m_AmbientStrength = 1;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalVector("_Diffuse", m_Diffuse);
        Shader.SetGlobalVector("_DiffuseGrazing", m_GrazingDiffuse);
        Shader.SetGlobalFloat("_WaterIOF", m_WaterIOF);
        Shader.SetGlobalFloat("_DiffuseStrength", m_DiffuseStrength);
        Shader.SetGlobalFloat("_SpecularStrength", m_SpecularStrength);
        Shader.SetGlobalFloat("_ReflecionStrength", m_ReflecionStrength);
        Shader.SetGlobalFloat("_RefractionStrength", m_RefractionStrength);
        Shader.SetGlobalFloat("_AmbientStrength", m_AmbientStrength);
    }
}