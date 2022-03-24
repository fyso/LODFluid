using UnityEngine;

public class FluidOfflineLoaderManager : MonoBehaviour
{
    [SerializeField]
    public string m_SPHDataPath;

    public SPHLoader m_SPHLoader;
    public SettingAsset m_SettingAsset;

    uint PhysicalFrameIndex = 0;
    [SerializeField]
    public bool m_IsImportDiffuse = true;

    public RenderStructSetting m_RenderStructSetting = new RenderStructSetting();
    void Awake()
    {
        //Screen.SetResolution(1024, 1024, false);
    }
    public void setPath(string Path)
    {
        m_SPHDataPath = Path;
    }
    public string getPath()
    {
        return m_SPHDataPath;
    }
    void OnEnable()
    {
        Matrix4x4 Local2World = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        m_SPHLoader.init(getPath(), m_IsImportDiffuse);
    }

    void FixedUpdate()
    {
        m_SPHLoader.step(PhysicalFrameIndex, m_SettingAsset, ref (m_RenderStructSetting.m_RenderStruct));
        m_RenderStructSetting.shareValueToGlobal(m_SettingAsset);
        PhysicalFrameIndex++;
    }

    private void OnDisable()
    {
        m_SPHLoader.free();
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Matrix4x4 Local2World = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = new Color(1, 1, 0, 0.6f);
    }
#endif
   
}
