using UnityEngine;
using UnityEngine.Rendering;

using static SettingAsset.Output;

public partial class CameraRenderer
{
    ScriptableRenderContext m_Context;
    Camera m_Camera;
    SettingAsset m_Setting;
    CullingResults m_CullingResults;

    LightManager  m_LightManager;
    SceneRenderer m_SceneRenderer;
    SurfaceRender   m_FluidSurfaceRenderer;
    FluidRenderer m_FluidRenderer;
    FoamRenderer  m_FoamRenderer;

    public CameraRenderer()
    {
        m_LightManager = new LightManager("Light and Shadow");
        m_SceneRenderer = new SceneRenderer("Scene");
        m_FluidRenderer = new FluidRenderer("Fluid");
        m_FluidSurfaceRenderer = new SurfaceRender("Fluid Surface");
        m_FoamRenderer = new FoamRenderer("Foam");
    }

    public void Render(ScriptableRenderContext context, Camera camera, SettingAsset setting)
    {
        m_Context = context;
        m_Camera = camera;
        m_Setting = setting;

        if (!Cull()) return;

        RenderMainScene();
        DrawUnsupportedShaders();
        DrawGizmos();

        m_Context.Submit();
        Clear();
    }

    bool Cull()
    {
        if (m_Camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            m_CullingResults = m_Context.Cull(ref p);
            return true;
        }
        return false;
    }

    void Show(RenderTexture outputRT, int level = 0)
    {
        CommandBuffer commandBuffer = new CommandBuffer()
        {
            name = "Show"
        };
        commandBuffer.Blit(new RenderTargetIdentifier(outputRT, level), m_Camera.targetTexture, Vector2.one, Vector2.zero);
        m_Context.ExecuteCommandBuffer(commandBuffer);
    }

    void Clear()
    {
        if (m_LightManager != null) m_LightManager.Clear();
        if (m_SceneRenderer != null) m_SceneRenderer.Clear();
        if (m_FluidSurfaceRenderer != null) m_FluidSurfaceRenderer.Clear();
        if (m_FoamRenderer != null) m_FoamRenderer.Clear();
    }

    void RenderMainScene()
    {
        m_Setting.UpdateShaderProperty();
        m_LightManager.PrepareLight(m_Context, m_Camera, m_Setting.m_ShadowSetting);
        m_Setting.UpdateCameraData(m_Context, m_Camera);

        m_SceneRenderer.RenderScene(m_Context, m_Camera, m_CullingResults, out RenderTexture resultRT, out RenderTexture depthRT);
        if (!Application.isPlaying || m_Setting.m_Output == Scene) { Show(resultRT); return; }


        RenderTexture foamRT = null;
        if (m_Setting.m_DrawFoam)
        {
            m_FoamRenderer.RenderInsideFoam(m_Context, m_Camera, out RenderTexture insideFoamRT, depthRT);
            foamRT = insideFoamRT;
        }
        if (m_Setting.m_Output == Foam) { Show(foamRT); return; }

        m_FluidSurfaceRenderer.RenderGBuffer(m_Context, m_Camera, depthRT, out RenderTexture fluidGBufferRT, out RenderTexture fluidThicknessRT);
        switch (m_Setting.m_Output)
        {
            case FuildNormal:
                Show(fluidGBufferRT); return;
            case FuildThickness:
                Show(fluidThicknessRT); return;
        }

        m_FluidRenderer.RenderFluid(m_Context, resultRT, depthRT, fluidGBufferRT, fluidThicknessRT, foamRT);
        if (m_Setting.m_Output == Fluid) { Show(resultRT); return; }

        if (m_Setting.m_DrawFoam) m_FoamRenderer.RenderOutsideFoam(resultRT, depthRT, fluidGBufferRT);
        if (m_Setting.m_Output == Result) { Show(resultRT); return; }
    }
}