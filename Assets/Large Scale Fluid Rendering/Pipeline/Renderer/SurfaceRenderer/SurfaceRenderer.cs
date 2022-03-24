using UnityEngine;
using UnityEngine.Rendering;

public class SurfaceRender 
{
    public CommandBuffer m_CommandBuffer;
    protected RenderTexture m_FluidDepthRT;
    protected RenderTexture m_FluidDepthVSRT;
    protected RenderTexture m_FluidGBufferRT;
    protected RenderTexture m_FluidThicknessRT;
    protected RenderTexture m_SmoothFluidDepthRT;

    public FlexFilter m_FlexFilter;
    public ParticleDrawer m_ParticleDrawer;
    public ThicknessCreater m_ThicknessCreater;

    public SurfaceRender(string passName)
    {
        m_CommandBuffer = new CommandBuffer
        {
            name = passName
        };
        m_FlexFilter = new FlexFilter(m_CommandBuffer);
        m_ParticleDrawer = new ParticleDrawer(m_CommandBuffer);
        m_ThicknessCreater = new ThicknessCreater(m_CommandBuffer);
    }

    public void RenderGBuffer(ScriptableRenderContext context, Camera camera, RenderTexture sceneDepthRT, out RenderTexture fluidGbufferRT, out RenderTexture fluidThicknessRT)
    {
        SettingAsset setting = Resources.Load<SettingAsset>("SettingAsset");
        m_FluidDepthRT = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 32, RenderTextureFormat.Depth);
        m_FluidDepthVSRT = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.RFloat);
        m_SmoothFluidDepthRT = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.RFloat);
        m_FluidGBufferRT = fluidGbufferRT = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.ARGBHalf);
        if (setting.m_DrawThickness) m_FluidThicknessRT = fluidThicknessRT = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.RFloat);
        else fluidThicknessRT = null;

        Shader.SetGlobalInt("_IsScale", 0);
        m_ParticleDrawer.DrawParticles(context, m_FluidDepthVSRT, m_FluidDepthRT, sceneDepthRT);
        m_FlexFilter.Smooth(context, m_FluidDepthVSRT, m_SmoothFluidDepthRT);
        GenerateNoramal(context, m_SmoothFluidDepthRT);
        if (setting.m_DrawThickness) m_ThicknessCreater.GenerateThickness(context, m_FluidThicknessRT, sceneDepthRT);
    }

    void GenerateNoramal(ScriptableRenderContext context, RenderTexture depthRT)
    {
        m_CommandBuffer.BeginSample("GenerateNoramal");
        m_CommandBuffer.SetRenderTarget(m_FluidGBufferRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalTexture("_DepthTex", depthRT);

        Material generateNoramalMaterial = Resources.Load<Material>("Materials/Component/GenerateNoramal");
        m_CommandBuffer.DrawProcedural(Matrix4x4.identity, generateNoramalMaterial,
            0, MeshTopology.Triangles, 3);
        m_CommandBuffer.EndSample("GenerateNoramal");
        context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }

    public void Clear()
    {
        if (m_FluidDepthRT) RenderTexture.ReleaseTemporary(m_FluidDepthRT);
        if (m_FluidDepthVSRT) RenderTexture.ReleaseTemporary(m_FluidDepthVSRT);
        if (m_FluidGBufferRT) RenderTexture.ReleaseTemporary(m_FluidGBufferRT);
        if (m_FluidThicknessRT) RenderTexture.ReleaseTemporary(m_FluidThicknessRT);
        if (m_SmoothFluidDepthRT) RenderTexture.ReleaseTemporary(m_SmoothFluidDepthRT);
    }
}

