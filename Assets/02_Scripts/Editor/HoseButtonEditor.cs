using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HoseButton))]
public class HoseButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Press Hose Button"))
        {
            foreach (Object selectedTarget in targets)
            {
                HoseButton hoseButton = (HoseButton)selectedTarget;
                hoseButton.LeverEnabled();
            }
        }
    }
}
