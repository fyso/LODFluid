using UnityEngine;
using UnityEngine.Rendering;

public class FlexFilter
{
    protected CommandBuffer m_CommandBuffer;
    Material m_FilterMaterial = Resources.Load<Material>("Materials/Component/Filter");

    public FlexFilter(CommandBuffer commandBuffer)
    {
        m_CommandBuffer = commandBuffer;
    }

    protected void SmoothPass(ScriptableRenderContext context, RenderTargetIdentifier source, RenderTargetIdentifier dest, int pass)
    {
        m_CommandBuffer.BeginSample("Smooth With Pass " + pass);
        m_CommandBuffer.SetRenderTarget(dest, dest);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalTexture("_ParticleGBuffer", source);
        m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_FilterMaterial, pass, MeshTopology.Triangles, 3);
        m_CommandBuffer.EndSample("Smooth With Pass " + pass);
        context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }
    public void Smooth(ScriptableRenderContext context, RenderTexture particlesRT, RenderTexture smoothParticlesRT)
    {
        SmoothPass(context, particlesRT, smoothParticlesRT, 0);
    }
}
