using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.GameFlow;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal static class SituationFixService
    {
        public static void AddMissingRoot(string scenePath)
        {
            WithSituationScene(
                scenePath,
                scene =>
                {
                    var controllers = SituationAuthoringUtility
                        .FindComponentsInScene<SituationController>(scene);
                    var roots = SituationAuthoringUtility
                        .FindComponentsInScene<SituationSceneRoot>(scene);
                    if (controllers.Count != 1 || roots.Count != 0)
                    {
                        throw new InvalidOperationException(
                            "A missing root can only be fixed when the scene has " +
                            "exactly one controller and no SituationSceneRoot.");
                    }

                    GameObject rootObject = controllers[0].transform.root.gameObject;
                    Undo.RegisterCompleteObjectUndo(
                        rootObject,
                        "Add Situation Scene Root");
                    SituationSceneRoot root =
                        Undo.AddComponent<SituationSceneRoot>(rootObject);
                    SituationAuthoringUtility.SetObjectReference(
                        root,
                        "_controller",
                        controllers[0]);
                });
        }

        public static void ConnectSingleController(string scenePath)
        {
            WithSituationScene(
                scenePath,
                scene =>
                {
                    var controllers = SituationAuthoringUtility
                        .FindComponentsInScene<SituationController>(scene);
                    var roots = SituationAuthoringUtility
                        .FindComponentsInScene<SituationSceneRoot>(scene);
                    if (controllers.Count != 1 || roots.Count != 1)
                    {
                        throw new InvalidOperationException(
                            "Controller reference can only be fixed when exactly " +
                            "one root and one controller exist.");
                    }

                    Undo.RecordObject(roots[0], "Connect Situation Controller");
                    SituationAuthoringUtility.SetObjectReference(
                        roots[0],
                        "_controller",
                        controllers[0]);
                });
        }

        public static void SynchronizeSceneName(
            SituationDefinition definition,
            string scenePath)
        {
            if (definition == null || string.IsNullOrWhiteSpace(scenePath))
            {
                return;
            }

            Undo.RecordObject(definition, "Synchronize Situation Scene Name");
            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("_sceneName").stringValue =
                System.IO.Path.GetFileNameWithoutExtension(scenePath);
            serializedDefinition.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
        }

        private static void WithSituationScene(
            string scenePath,
            Action<Scene> action)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForOperation = !scene.IsValid() || !scene.isLoaded;
            if (openedForOperation)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                action(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedForOperation)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
