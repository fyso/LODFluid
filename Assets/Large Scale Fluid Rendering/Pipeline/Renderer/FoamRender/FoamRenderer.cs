using UnityEngine;
using UnityEngine.Rendering;
class FoamRenderer
{
    ScriptableRenderContext m_Context;
    CommandBuffer m_CommandBuffer;
    Material m_FoamMaterial = Resources.Load<Material>("Materials/Component/Foam");
    RenderTexture m_FoamRT;
    public FoamRenderer(string passName)
    {
        m_CommandBuffer = new CommandBuffer
        {
            name = passName
        };
    }

    public void RenderInsideFoam(ScriptableRenderContext context, Camera camera, out RenderTexture FoamRT, RenderTexture sceneDepthRT)
    {
        m_FoamRT = FoamRT = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.ARGBHalf);

        m_Context = context;
        SettingAsset setting = Resources.Load<SettingAsset>("SettingAsset");

        m_CommandBuffer.BeginSample("Inside Foam");
        m_CommandBuffer.SetRenderTarget(FoamRT, sceneDepthRT);
        m_CommandBuffer.ClearRenderTarget(false, true, Color.clear);

        m_CommandBuffer.DisableShaderKeyword("_OUTSIDE_FOAM");

        m_CommandBuffer.DrawProcedural(
            Matrix4x4.identity, m_FoamMaterial, 0, MeshTopology.Quads,
            4, setting.m_InputSetting.m_DiffuseParticleCount);

        m_CommandBuffer.EndSample("Inside Foam");
        ExecuteBuffer();
    }

    public void RenderOutsideFoam(RenderTexture sceneColorRT, RenderTexture sceneDepthRT, RenderTexture FluidGBufferRT)
    {
        SettingAsset setting = Resources.Load<SettingAsset>("SettingAsset");

        m_CommandBuffer.BeginSample("Outside Foam");
        m_CommandBuffer.SetRenderTarget(sceneColorRT, sceneDepthRT);
        m_CommandBuffer.ClearRenderTarget(false, false, Color.clear);

        m_CommandBuffer.EnableShaderKeyword("_OUTSIDE_FOAM");
        m_CommandBuffer.SetGlobalTexture("_FluidGBuffer", FluidGBufferRT);

        m_CommandBuffer.DrawProcedural(
            Matrix4x4.identity, m_FoamMaterial, 0, MeshTopology.Quads,
            4, setting.m_InputSetting.m_DiffuseParticleCount);

        m_CommandBuffer.EndSample("Outside Foam");
        ExecuteBuffer();
    }

    void ExecuteBuffer()
    {
        m_Context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }

    public void Clear()
    {
        if(m_FoamRT) RenderTexture.ReleaseTemporary(m_FoamRT);
    }
}

