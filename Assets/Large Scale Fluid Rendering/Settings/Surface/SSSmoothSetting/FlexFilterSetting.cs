using UnityEngine;
[System.Serializable]
public class FlexFilterSetting
{
    [Range(0.0f, 0.1f)]
    public float m_BlurRadiusWorld = 0.05f;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalFloat("_BlurRadiusWorld", m_BlurRadiusWorld);
    }
}