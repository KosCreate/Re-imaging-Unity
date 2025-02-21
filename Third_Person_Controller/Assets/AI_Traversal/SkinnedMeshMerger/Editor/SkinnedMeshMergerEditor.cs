using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SkinnedMeshMerger))]
public class SkinnedMeshMergerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SkinnedMeshMerger thisSkinMeshMerger = (SkinnedMeshMerger)target;

        if (GUILayout.Button("Merge Skins"))
        {
            thisSkinMeshMerger.MergeSkinnedMeshes();
        }
    }
}
