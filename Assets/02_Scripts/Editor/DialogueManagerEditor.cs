using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueManager))]
public class DialogueManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Play Testing Group"))
        {
            foreach (Object selectedTarget in targets)
            {
                DialogueManager dialogueManager = (DialogueManager)selectedTarget;
                dialogueManager.PlayGroup("testing");
            }
        }

        if (GUILayout.Button("Play test_004"))
        {
            foreach (Object selectedTarget in targets)
            {
                DialogueManager dialogueManager = (DialogueManager)selectedTarget;
                dialogueManager.Play("test_004");
            }
        }
        
        if (GUILayout.Button("Play Testing2 Group"))
        {
            foreach (Object selectedTarget in targets)
            {
                DialogueManager dialogueManager = (DialogueManager)selectedTarget;
                dialogueManager.PlayGroup("testing2");
            }
        }
    }
}
