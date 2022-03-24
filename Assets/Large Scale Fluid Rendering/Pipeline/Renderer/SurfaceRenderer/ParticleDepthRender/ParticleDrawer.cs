using UnityEngine;
using UnityEngine.Rendering;

public class ParticleDrawer
{
    public ParticleDrawer(CommandBuffer commandBuffer)
    {
        m_CommandBuffer = commandBuffer;

    }
    protected CommandBuffer m_CommandBuffer;

    public void DrawParticles(
        ScriptableRenderContext context,
        RenderTexture depthVSRT,
        RenderTexture depthRT,
        RenderTexture sceneDepthRT)
    {
        Material material = Resources.Load<Material>("Materials/Component/DrawFluidParticles");
        SettingAsset setting = Resources.Load<SettingAsset>("SettingAsset");

        m_CommandBuffer.BeginSample("DrawParticlesDepth");
        m_CommandBuffer.SetRenderTarget(depthVSRT, depthRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalTexture("_SceneDepth", sceneDepthRT);
        m_CommandBuffer.SetGlobalFloat("_ParticlesRadius", setting.m_InputSetting.m_ParticlesRadius);
       
        m_CommandBuffer.DrawProcedural(
            Matrix4x4.identity,
            material, setting.m_InputSetting.m_DrawEllipsoids ? 0 : 1,
            MeshTopology.Quads, 4, setting.m_InputSetting.m_ParticleCount);
        m_CommandBuffer.EndSample("DrawParticlesDepth");
        context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }
}

