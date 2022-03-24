using UnityEngine;

public class ShadingSettingManager : MonoBehaviour
{
    public Cubemap m_Skybox = null;

    [Space]
    public FluidShadingSetting m_FluidShadingSetting = default;

    [Space]
    public FoamShadingSetting m_FoamShadingSetting = default;

    void Start()
    {
        m_FluidShadingSetting.UpdateShaderProperty();
        m_FoamShadingSetting.UpdateShaderProperty();
    }

    void Update()
    {
#if UNITY_EDITOR
        m_FluidShadingSetting.UpdateShaderProperty();
        m_FoamShadingSetting.UpdateShaderProperty();
#endif
    }
}
