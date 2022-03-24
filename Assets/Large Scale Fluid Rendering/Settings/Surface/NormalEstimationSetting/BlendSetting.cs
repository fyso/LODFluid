using UnityEngine;

[System.Serializable]
public class BlendSetting
{
    [Range(1, 100)]
    public int m_NumIteration = 10;
    [Range(0, 0.001f)]
    public float m_Precision = 0.0001f;

    [Range(0, 20)]
    public int m_BlendingD = 10;
    [Range(0, 2f)]
    public float m_BlendingK = 0.998f;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalInt("_NumIteration", m_NumIteration);
        Shader.SetGlobalFloat("_Precision", m_Precision);

        Shader.SetGlobalFloat("_BlendingD", m_BlendingD);
        Shader.SetGlobalFloat("_BlendingK", m_BlendingK);
    }
}