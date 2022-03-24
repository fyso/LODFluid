using UnityEngine;

public class UpdateMesh : MonoBehaviour
{
    public GameObject manager;
    public Material material;
    public bool isOdd;
    private void FixedUpdate()
    {
        if ((Time.frameCount % 2 == 0) == isOdd)
            GetComponent<MeshRenderer>().enabled = false;
        else
        {
            GetComponent<MeshRenderer>().enabled = true;
            if (manager.GetComponent<FluidOfflineLoaderManager>()) 
                GetComponent<MeshFilter>().mesh = manager.GetComponent<FluidOfflineLoaderManager>().m_RenderStructSetting.m_RenderStruct.m_Mesh;
            if(manager.GetComponent<FluidRealTimeLoaderManager>()) 
                GetComponent<MeshFilter>().mesh = manager.GetComponent<FluidRealTimeLoaderManager>().m_RenderStructSetting.m_RenderStruct.m_Mesh;
            GetComponent<MeshRenderer>().material = material;
        }
    }
}