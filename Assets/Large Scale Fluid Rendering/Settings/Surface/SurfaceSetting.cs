using UnityEngine;

[System.Serializable]
public class SurfaceSetting
{
    [Space]
    public FlexFilterSetting m_SmoothSetting = new FlexFilterSetting();

    public void UpdateShaderProperty()
    {
        m_SmoothSetting.UpdateShaderProperty();
    }
}