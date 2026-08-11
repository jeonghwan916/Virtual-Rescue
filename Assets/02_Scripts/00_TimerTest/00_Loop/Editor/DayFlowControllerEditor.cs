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

            DayFlowController controller = (DayFlowController)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Current Day", controller.CurrentDay);
            }

            bool canAdvance = Application.isPlaying &&
                              controller.CurrentState == DayFlowState.Playing &&
                              !controller.IsEndingDay;

            using (new EditorGUI.DisabledScope(!canAdvance))
            {
                if (GUILayout.Button("Complete Day (Advance to Next Day)"))
                {
                    controller.CompleteDay();
                }
            }

            bool canJumpToDaySeven = Application.isPlaying &&
                                     controller.CurrentState == DayFlowState.Playing &&
                                     controller.CurrentDay != 7;

            using (new EditorGUI.DisabledScope(!canJumpToDaySeven))
            {
                if (GUILayout.Button("Go to Day 7"))
                {
                    controller.TransitionToDayForDebug(7);
                }
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
