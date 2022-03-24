using UnityEngine;

public class FluidRealTimeLoaderManager : MonoBehaviour
{
    public SettingAsset m_SettingAsset;
    public RenderStructSetting m_RenderStructSetting = new RenderStructSetting();

    void FixedUpdate()
    {
        m_RenderStructSetting.shareValueToGlobal(m_SettingAsset);
    }
}
