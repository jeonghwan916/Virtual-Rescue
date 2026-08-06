using UnityEditor;
using UnityEngine;

namespace VirtualRescue.GameFlow.Editor
{
    [CustomEditor(typeof(DayFlowController))]
    public sealed class DayFlowControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(true))
            {
                DayFlowController controller = (DayFlowController)target;
                EditorGUILayout.IntField("Current Day", controller.CurrentDay);
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
