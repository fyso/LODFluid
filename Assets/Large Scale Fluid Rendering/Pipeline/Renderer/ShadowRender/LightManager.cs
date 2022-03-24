using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightManager 
{
    ScriptableRenderContext m_Context;
    Camera m_Camera;
    CommandBuffer m_CommandBuffer;
    ShadowSetting m_ShadowSetting;

    RenderTexture m_ShadowRT = null;
    CullingResults m_CullingResults;
    static ShaderTagId m_ShadowShaderTagId = new ShaderTagId("ShadowCaster");
    public Matrix4x4 m_LightViewMatrix;
    public Matrix4x4 m_LightProjectionMatrix;

    public LightManager(string passName)
    {
        m_CommandBuffer = new CommandBuffer
        {
            name = passName
        };
    }

    public void PrepareLight(ScriptableRenderContext context, Camera camera, ShadowSetting shadowSetting)
    {
        m_Context = context;
        m_Camera = camera;
        m_ShadowSetting = shadowSetting;

        m_Camera.TryGetCullingParameters(out ScriptableCullingParameters cullParam);
        cullParam.isOrthographic = false;
        cullParam.shadowDistance = Mathf.Min(m_ShadowSetting.m_MaxShadowDistance, m_Camera.farClipPlane);
        m_CullingResults = m_Context.Cull(ref cullParam);

        foreach (var light in m_CullingResults.visibleLights)
        {
            if (light.lightType == LightType.Directional)
            {
                m_CommandBuffer.SetGlobalVector("_WorldSpaceLightDir0", -light.localToWorldMatrix.GetColumn(2));
                m_CommandBuffer.SetGlobalColor("_LightColor0", light.finalColor);
                m_CommandBuffer.SetGlobalInt("_IsSpotLight", 0);
                m_CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    0, 0, 1, Vector3.zero, (int)m_ShadowSetting.m_TextureSize, 0f,
                    out m_LightViewMatrix, out m_LightProjectionMatrix, out ShadowSplitData splitData);
                m_CommandBuffer.SetGlobalMatrix("_DirectionalShadowMatrices", m_LightProjectionMatrix * m_LightViewMatrix);

                break;
            }

            if (light.lightType == LightType.Spot)
            {
                m_CommandBuffer.SetGlobalVector("_WorldSpaceLightDir0", -light.localToWorldMatrix.GetColumn(2));
                m_CommandBuffer.SetGlobalVector("_WorldSpaceLightPos0", light.localToWorldMatrix.GetColumn(3));
                m_CommandBuffer.SetGlobalColor("_LightColor0", light.finalColor);
                m_CommandBuffer.SetGlobalInt("_IsSpotLight", 1);

                m_CullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(0, out m_LightViewMatrix, out m_LightProjectionMatrix, out ShadowSplitData splitData);
                m_CommandBuffer.SetGlobalMatrix("_DirectionalShadowMatrices", m_LightProjectionMatrix * m_LightViewMatrix);

                break;
            }
        }

        if (m_ShadowSetting.m_ShadowType != ShadowSetting.ShadowType._NO_SHADOW) RenderShadow();
        m_CommandBuffer.SetGlobalInt("_ShadowType", (int)m_ShadowSetting.m_ShadowType);

        ExecuteBuffer();
    }

    void RenderShadow()
    {
        m_ShadowRT = RenderTexture.GetTemporary((int)m_ShadowSetting.m_TextureSize, (int)m_ShadowSetting.m_TextureSize, 32, RenderTextureFormat.Depth);
        m_ShadowRT.filterMode = FilterMode.Bilinear;
        m_CommandBuffer.SetRenderTarget(m_ShadowRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);

        m_Context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();

        var drawingSettings = new DrawingSettings(m_ShadowShaderTagId, new SortingSettings(m_Camera));
        var filteringSettings = FilteringSettings.defaultValue;
        m_CommandBuffer.SetViewProjectionMatrices(m_LightViewMatrix, m_LightProjectionMatrix);
        ExecuteBuffer();

        m_Context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);


        m_CommandBuffer.BeginSample("DrawSpriteParticlesForShadow");
        m_CommandBuffer.SetRenderTarget(m_ShadowRT);
        m_CommandBuffer.ClearRenderTarget(false, false, Color.clear);
        Material drawFluidParticlesMaterial = Resources.Load<Material>("Materials/Component/DrawFluidParticles");
        SettingAsset setting = Resources.Load<SettingAsset>("SettingAsset");
        m_CommandBuffer.SetGlobalFloat("_ParticlesRadius", setting.m_InputSetting.m_ParticlesRadius * 0.6f);
        m_CommandBuffer.DrawProcedural(
            Matrix4x4.identity,
            drawFluidParticlesMaterial, 2,
            MeshTopology.Quads, 4, setting.m_InputSetting.m_ParticleCount);
        m_CommandBuffer.EndSample("DrawSpriteParticlesForShadow");

        m_CommandBuffer.SetGlobalTexture("_Shadow", m_ShadowRT);
    }

    void ExecuteBuffer()
    {
        m_Context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }

    public void Clear()
    {
        if(m_ShadowRT) RenderTexture.ReleaseTemporary(m_ShadowRT);
    }
}
