using UnityEngine;

[System.Serializable]
public class PCASetting
{
    [Range(1, 20)]
    public int m_NumIteration = 10;
    [Range(0, 0.1f)]
    public float m_Precision = 0.01f;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalInt("_NumIteration", m_NumIteration);
        Shader.SetGlobalFloat("_Precision", m_Precision);
    }
}