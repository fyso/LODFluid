using UnityEngine;
using UnityEngine.Rendering;

public class ThicknessCreater
{
    public ThicknessCreater(CommandBuffer commandBuffer)
    {
        m_CommandBuffer = commandBuffer;
    }
    protected CommandBuffer m_CommandBuffer;
    public void GenerateThickness(
        ScriptableRenderContext context,
        RenderTexture fluidThicknessRT,
        RenderTexture sceneDepthRT)
    {
        SettingAsset setting = Resources.Load<SettingAsset>("SettingAsset");
        Material material = Resources.Load<Material>("Materials/Component/CalculateThickness");

        m_CommandBuffer.BeginSample("GenerateThickness");
        m_CommandBuffer.SetRenderTarget(fluidThicknessRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalTexture("_SceneDepth", sceneDepthRT);
        m_CommandBuffer.SetGlobalFloat("_ParticlesRadius", setting.m_InputSetting.m_ParticlesRadius * 2f);
        m_CommandBuffer.DrawProcedural(
            Matrix4x4.identity,
            material, 0,
            MeshTopology.Quads, 4, setting.m_InputSetting.m_ParticleCount);

        m_CommandBuffer.EndSample("GenerateThickness");
        context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }
}

