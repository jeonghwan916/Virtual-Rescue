using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TimerManager))]
public class TimerManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Activate Timer"))
            {
                foreach (Object selectedTarget in targets)
                {
                    TimerManager timerManager = (TimerManager)selectedTarget;
                    timerManager.ActivateTimer();
                }
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Activate Timer can be used in Play Mode.", MessageType.Info);
        }
    }
}
