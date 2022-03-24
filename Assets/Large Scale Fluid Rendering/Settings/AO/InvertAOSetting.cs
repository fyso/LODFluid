using UnityEngine;

[System.Serializable]
public class InvertAOSetting
{
    int m_LastFrameKernelSize = 0;
    [Range(16, 64)]
    public int m_KernelSize = 16;
    [Range(1.0f, 10.0f)]
    public float m_KernelRadius = 5.0f;
    [Range(0.5f, 5.0f)]
    public float m_InvertAOStrength = 0.5f;

    Vector4[] m_Kernels;
    Texture2D m_NoiseTex;

    public void SetupKernels()
    {
        //TODO using dcy
        m_Kernels = new Vector4[m_KernelSize];
        for (int i = 0; i < m_KernelSize; i++)
        {
            float random = Random.Range(0.0f, 1.0f);
            Vector4 sample = new Vector4(random * 2.0f - 1.0f, random * 2.0f - 1.0f, random, 0.0f);
            sample = sample.normalized;
            sample *= Random.Range(0.0f, 1.0f);
            float scale = i / m_KernelSize;
            scale = Mathf.Lerp(0.1f, 1.0f, scale * scale);
            sample *= scale;
            m_Kernels[i] = sample;
        }
        Shader.SetGlobalVectorArray("_InvertAOKernels", m_Kernels);
    }

    public void SetupNoiseData()
    {
        //TODO using dcy
        Vector3[] noises = new Vector3[16];
        for (int i = 0; i < 9; i++)
        {
            float random = Random.Range(0.0f, 1.0f);
            noises[i] = new Vector3(random, random, 0.0f); //TextureFormat.RGB24: per channle range is 0 to 1,so should change it to [-1.1] in ssao shader
        }

        m_NoiseTex = new Texture2D(3, 3, TextureFormat.RGB24, false, true);
        m_NoiseTex.filterMode = FilterMode.Point;
        m_NoiseTex.wrapMode = TextureWrapMode.Repeat;
        m_NoiseTex.SetPixelData(noises, 0, 0);
        m_NoiseTex.Apply();
        Shader.SetGlobalTexture("_InvertAONoiseTex", m_NoiseTex);

        m_LastFrameKernelSize = m_KernelSize;
    }

    public void UpdateShaderProperty()
    {
        if (m_NoiseTex == null)
        {
            SetupNoiseData();
        }

        if (m_LastFrameKernelSize != m_KernelSize || m_Kernels == null)
        {
            SetupKernels();
            m_LastFrameKernelSize = m_KernelSize;
        }
        Shader.SetGlobalInt("_InvertAOKernelSize", m_KernelSize);
        Shader.SetGlobalFloat("_InvertAORadius", m_KernelRadius);
    }

    public Vector2 GetNoiseTexSize()
    {
        return new Vector2(m_NoiseTex.width, m_NoiseTex.height);
    }
}
