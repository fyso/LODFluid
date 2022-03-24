using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Setting Manager Asset")]
public class SettingAsset : ScriptableObject
{
    public bool m_UseDynamicBatching = true;
    public bool m_UseGPUInstancing = true;
    public bool m_UseSRPBatcher = true;

    public InputSetting m_InputSetting = default;
    [Space]
    public bool m_DrawThickness = true;
    [Space]
    public bool m_DrawFoam = true;

    [Space]
    [Space]
    public SurfaceSetting m_SurfaceSetting = default;

    [Space]
    [Space]
    public ShadowSetting m_ShadowSetting = default;

    [Space]
    [Space]
    public bool m_ShowDiffuse = true;
    public bool m_ShowSpecular = true;
    public bool m_ShowReflecion = true;
    public bool m_ShowRefraction = true;

    public enum Output
    {
        Scene,
        FuildNormal,
        FuildThickness,
        Foam,
        Fluid,
        Result
    }
    [Space]
    [Space]
    public Output m_Output = Output.Result;


    public void UpdateShaderProperty()
	{
        m_InputSetting.UpdateShaderProperty();
        m_SurfaceSetting.UpdateShaderProperty();

        Shader.SetGlobalInt("_ShowDiffuse", m_ShowDiffuse ? 1 : 0);
        Shader.SetGlobalInt("_ShowSpecular", m_ShowSpecular ? 1 : 0);
        Shader.SetGlobalInt("_ShowReflecion", m_ShowReflecion ? 1 : 0);
        Shader.SetGlobalInt("_ShowRefraction", m_ShowRefraction ? 1 : 0);
        Shader.SetGlobalInt("_UseThicknessMap", m_DrawThickness ? 1 : 0);
        Shader.SetGlobalInt("_BlendFoam", m_DrawFoam ? 1 : 0);
    }

    public void UpdateCameraData(ScriptableRenderContext context, Camera camera)
    {
        CommandBuffer commandBuffer = new CommandBuffer();
        commandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

        var projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        var viewProjMatrix = projMatrix * camera.worldToCameraMatrix;
        var invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
        var invUnityCameraProjMatrix = Matrix4x4.Inverse(camera.projectionMatrix);

        commandBuffer.SetGlobalMatrix("unity_MatrixIV", camera.cameraToWorldMatrix);
        commandBuffer.SetGlobalMatrix("unity_MatrixIP", invUnityCameraProjMatrix);
        commandBuffer.SetGlobalMatrix("unity_MatrixIVP", camera.cameraToWorldMatrix * Matrix4x4.Inverse(camera.projectionMatrix));
        commandBuffer.SetGlobalMatrix("glstate_matrix_inv_projection", Matrix4x4.Inverse(projMatrix));
        commandBuffer.SetGlobalMatrix("glstate_matrix_view_projection", viewProjMatrix);
        commandBuffer.SetGlobalMatrix("glstate_matrix_inv_view_projection", invViewProjMatrix);
        commandBuffer.SetGlobalFloat("_CameraFarDistance", camera.farClipPlane);
        commandBuffer.SetGlobalVector("_WorldSpaceCameraPos", camera.transform.position);
        commandBuffer.SetGlobalVector("_ScreenParams", new Vector4(camera.pixelWidth, camera.pixelHeight, 1 + 1 / camera.pixelWidth, 1 + 1 / camera.pixelHeight));
        
        context.ExecuteCommandBuffer(commandBuffer);
    }
}
