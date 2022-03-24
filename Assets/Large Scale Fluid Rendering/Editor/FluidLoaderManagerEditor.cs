using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FluidOfflineLoaderManager), true)]
[CanEditMultipleObjects]
public class FluidSimulationOfflineManagerEditor : Editor
{
    private SerializedProperty m_Path, m_SettingAssetPath, m_IsImportDiffuse;
    FluidOfflineLoaderManager m_SPHFluidSimulationOffline;

    private void OnEnable()
    {
        m_Path = serializedObject.FindProperty("m_SPHDataPath");
        m_SettingAssetPath = serializedObject.FindProperty("m_SettingAsset");
        m_IsImportDiffuse = serializedObject.FindProperty("m_IsImportDiffuse");
        m_SPHFluidSimulationOffline = (FluidOfflineLoaderManager)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.PropertyField(m_Path);
        EditorGUILayout.PropertyField(m_SettingAssetPath, new GUIContent("Setting Asset"));
        EditorGUILayout.PropertyField(m_IsImportDiffuse);
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(500));
        if ((Event.current.type == EventType.DragUpdated
            || Event.current.type == EventType.DragExited 
            || Event.current.type == EventType.ContextClick)
            && rect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                string retPath = DragAndDrop.paths[0] + "/";
                m_Path.stringValue = retPath;
                m_SPHFluidSimulationOffline.setPath(retPath);    
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}