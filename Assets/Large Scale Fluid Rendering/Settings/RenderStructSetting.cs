using UnityEngine;

[System.Serializable]
public class RenderStructSetting
{
    public struct SRenderStruct
    {
        public Mesh m_Mesh;
        public ComputeBuffer m_ParticleBuffer;
        public ComputeBuffer m_DiffuseBuffer;
        public uint m_ParticleCount;
        public uint m_DiffuseCount;
    };
    public SRenderStruct m_RenderStruct;
    public void shareValueToGlobal(SettingAsset settingAsset)
    {
        Shader.SetGlobalBuffer("_ParticlesBuffer", m_RenderStruct.m_ParticleBuffer);
        Shader.SetGlobalBuffer("_DiffuseParticlesBuffer", m_RenderStruct.m_DiffuseBuffer);
        settingAsset.m_InputSetting.m_DiffuseParticleCount = (int)m_RenderStruct.m_DiffuseCount;
        settingAsset.m_InputSetting.m_ParticleCount = (int)m_RenderStruct.m_ParticleCount;
    }
}