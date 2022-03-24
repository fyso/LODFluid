using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

public class FluidRenderer
{
    CommandBuffer m_CommandBuffer;
    RayTracingAccelerationStructure m_AccelerationStructure;
    RayTracingShader m_ShadeFluidShader = Resources.Load<RayTracingShader>("Shaders/ShadeFluid");
    Cubemap m_Skybox = GameObject.FindWithTag("Fluid").GetComponent<ShadingSettingManager>().m_Skybox;

    public FluidRenderer(string passName)
    {
        m_CommandBuffer = new CommandBuffer
        {
            name = passName
        };
        RayTracingAccelerationStructure.RASSettings setting = new RayTracingAccelerationStructure.RASSettings(
            RayTracingAccelerationStructure.ManagementMode.Automatic,
            RayTracingAccelerationStructure.RayTracingModeMask.Everything,
            -1 ^ (1 << 7));
        m_AccelerationStructure = new RayTracingAccelerationStructure(setting);
    }

    public void RenderFluid(ScriptableRenderContext context, RenderTexture outputRT, RenderTexture sceneDepthRT, RenderTexture fluidGBufferRT, RenderTexture fluidThicknessRT, RenderTexture foamRT)
    {
        m_AccelerationStructure.Build();
        SettingAsset setting = Resources.Load<SettingAsset>("SettingAsset");

        Profiler.BeginSample(m_CommandBuffer.name);
        m_CommandBuffer.SetRayTracingAccelerationStructure(m_ShadeFluidShader, "_AccelerationStructure", m_AccelerationStructure);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_Output", outputRT);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_SceneDepth", sceneDepthRT);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_FluidGBuffer", fluidGBufferRT);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_FluidThickness", fluidThicknessRT);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_Foam", foamRT);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_SkyboxTexture", m_Skybox);
        m_CommandBuffer.SetGlobalFloat("_ShadowBias", 0.1f);
        
        m_CommandBuffer.SetRayTracingShaderPass(m_ShadeFluidShader, "SceneHit");
        m_CommandBuffer.DispatchRays(m_ShadeFluidShader, "FluidRayGen", (uint)outputRT.width, (uint)outputRT.height, 1);
        Profiler.EndSample();

        context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }
}