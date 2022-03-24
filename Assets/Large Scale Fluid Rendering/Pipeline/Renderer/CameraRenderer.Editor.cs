using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
	partial void DrawUnsupportedShaders();
	partial void DrawGizmos();


#if UNITY_EDITOR

	static ShaderTagId[] legacyShaderTagIds =
	{
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Unlit"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};
	static Material errorMaterial;

	partial void DrawUnsupportedShaders()
	{
		if (errorMaterial == null) errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));

		var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(m_Camera))
		{
			overrideMaterial = errorMaterial
		};
		for (int i = 1; i < legacyShaderTagIds.Length; i++)
		{
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}
		var filteringSettings = FilteringSettings.defaultValue;
		m_Context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
	}

	partial void DrawGizmos()
	{
		if (Handles.ShouldRenderGizmos())
		{
			m_Context.DrawGizmos(m_Camera, GizmoSubset.PreImageEffects);
			m_Context.DrawGizmos(m_Camera, GizmoSubset.PostImageEffects);
		}
	}
#endif
}
