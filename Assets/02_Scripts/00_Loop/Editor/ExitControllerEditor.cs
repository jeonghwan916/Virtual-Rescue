using UnityEditor;
using UnityEngine;

namespace VirtualRescue.GameFlow.Editor
{
    [CustomEditor(typeof(ExitController))]
    public sealed class ExitControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Request Exit"))
                {
                    ((ExitController)target).RequestExit();
                }
            }
        }
    }
}
