using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline 
{
	public SettingAsset m_Setting = default;
	CameraRenderer m_Renderer;

	public CustomRenderPipeline(CameraRenderer renderer, SettingAsset setting)
	{
		m_Renderer = renderer;
		m_Setting = setting;
		GraphicsSettings.useScriptableRenderPipelineBatching = m_Setting.m_UseSRPBatcher;
		GraphicsSettings.lightsUseLinearIntensity = true;
	}

	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
 		foreach (Camera camera in cameras)
		{
			m_Renderer.Render(context, camera, m_Setting);
		}
	}
}
