using UnityEngine;
using UnityEngine.Rendering;

class SceneRenderer
{
    protected CommandBuffer m_CommandBuffer;
    RenderTexture m_SceneDepthRT;
    RenderTexture m_SceneColorRT;
    static ShaderTagId m_SceneShaderTagId = new ShaderTagId("Raster");

    public SceneRenderer(string passName)
    {
        m_CommandBuffer = new CommandBuffer
        {
            name = passName
        };
    }

    public void RenderScene(
        ScriptableRenderContext context, Camera camera, CullingResults cullingResults,
        out RenderTexture sceneColorRT, out RenderTexture sceneDepthRT)
    {
        m_SceneColorRT = sceneColorRT = RenderTexture.GetTemporary(new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight, RenderTextureFormat.ARGBHalf) { enableRandomWrite = true });
        m_SceneDepthRT = sceneDepthRT = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 32, RenderTextureFormat.Depth);

        m_CommandBuffer.SetRenderTarget(sceneColorRT, sceneDepthRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.black);

        context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();

        var drawingSettings = new DrawingSettings(m_SceneShaderTagId, new SortingSettings(camera));
        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    public void Clear()
    {
        if(m_SceneDepthRT) RenderTexture.ReleaseTemporary(m_SceneDepthRT);
        if(m_SceneColorRT) RenderTexture.ReleaseTemporary(m_SceneColorRT);

    }
}

