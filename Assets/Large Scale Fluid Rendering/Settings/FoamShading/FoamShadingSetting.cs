using UnityEngine;

[System.Serializable]
public class FoamShadingSetting
{
    [Range(0.01f, 0.1f)]
    public float m_FoamRadius = 0.02f;

    [Range(0, 1f)]
    public float m_Diffusion = 0.7f;

    [Range(0.01f, 2f)]
    public float m_MotionBlurScale = 1;

    public  Color m_FoamColor = Color.white;
    
    public void UpdateShaderProperty()
    {
        Shader.SetGlobalFloat("_FoamRadius", m_FoamRadius);
        Shader.SetGlobalFloat("_Diffusion", m_Diffusion);
        Shader.SetGlobalFloat("_MotionBlurScale", m_MotionBlurScale);
        Shader.SetGlobalColor("_FoamColor", m_FoamColor);
    }
}
