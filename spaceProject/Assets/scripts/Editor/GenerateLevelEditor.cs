using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelBuilder))]
public class GenerateLevelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LevelBuilder builder = (LevelBuilder)target;

        if(GUILayout.Button("Generate Level"))
        {
            builder.GenerateNewLevel();
        }
    }
}
