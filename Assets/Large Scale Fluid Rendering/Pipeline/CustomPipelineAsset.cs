using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Pipeline Asset")]
public class CustomPipelineAsset : RenderPipelineAsset
{
	public SettingAsset m_Setting;

	protected override RenderPipeline CreatePipeline()
	{
		CameraRenderer renderer = new CameraRenderer();
		return new CustomRenderPipeline(renderer, m_Setting);
	}
}