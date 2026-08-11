using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.GameFlow;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal sealed class SituationRegistrationSnapshot
    {
        public bool LoopBaseAvailable { get; set; }
        public int CandidateOccurrenceCount { get; set; }
        public bool HasNullCandidate { get; set; }
        public HomeLayoutDefinition HomeLayout { get; set; }
    }

    internal static class SituationRegistrationService
    {
        public static SituationRegistrationSnapshot GetSnapshot(
            SituationDefinition definition)
        {
            SituationRegistrationSnapshot snapshot = new();
            snapshot.LoopBaseAvailable = WithLoopBase(
                false,
                (_, selector, coordinator) =>
                {
                    SerializedObject serializedSelector = new(selector);
                    SerializedProperty candidates =
                        serializedSelector.FindProperty("_candidates");
                    for (int index = 0; index < candidates.arraySize; index++)
                    {
                        UnityEngine.Object candidate = candidates
                            .GetArrayElementAtIndex(index).objectReferenceValue;
                        if (candidate == null)
                        {
                            snapshot.HasNullCandidate = true;
                        }

                        if (candidate == definition)
                        {
                            snapshot.CandidateOccurrenceCount++;
                        }
                    }

                    SerializedObject serializedCoordinator = new(coordinator);
                    snapshot.HomeLayout = serializedCoordinator
                        .FindProperty("_homeLayout")?.objectReferenceValue as
                        HomeLayoutDefinition;
                    return true;
                });
            return snapshot;
        }

        public static bool IsCandidateRegistered(
            SituationDefinition definition,
            out int occurrenceCount)
        {
            occurrenceCount = 0;
            if (definition == null)
            {
                return false;
            }

            int count = 0;
            bool isRegistered = WithLoopBase(
                false,
                (_, selector, _) =>
                {
                    SerializedObject serializedSelector = new(selector);
                    SerializedProperty candidates =
                        serializedSelector.FindProperty("_candidates");

                    for (int index = 0; index < candidates.arraySize; index++)
                    {
                        if (candidates.GetArrayElementAtIndex(index)
                                .objectReferenceValue == definition)
                        {
                            count++;
                        }
                    }

                    return count > 0;
                });
            occurrenceCount = count;
            return isRegistered;
        }

        public static bool RegisterCandidate(SituationDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return WithLoopBase(
                true,
                (scene, selector, _) =>
                {
                    SerializedObject serializedSelector = new(selector);
                    SerializedProperty candidates =
                        serializedSelector.FindProperty("_candidates");

                    for (int index = 0; index < candidates.arraySize; index++)
                    {
                        if (candidates.GetArrayElementAtIndex(index)
                                .objectReferenceValue == definition)
                        {
                            return true;
                        }
                    }

                    Undo.RecordObject(selector, "Register Situation Candidate");
                    int newIndex = candidates.arraySize;
                    candidates.arraySize++;
                    candidates.GetArrayElementAtIndex(newIndex)
                        .objectReferenceValue = definition;
                    serializedSelector.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selector);
                    return EditorSceneManager.SaveScene(scene);
                });
        }

        public static bool UnregisterCandidate(SituationDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return WithLoopBase(
                true,
                (scene, selector, _) =>
                {
                    SerializedObject serializedSelector = new(selector);
                    SerializedProperty candidates =
                        serializedSelector.FindProperty("_candidates");
                    bool changed = false;

                    Undo.RecordObject(selector, "Unregister Situation Candidate");
                    for (int index = candidates.arraySize - 1;
                         index >= 0;
                         index--)
                    {
                        if (candidates.GetArrayElementAtIndex(index)
                                .objectReferenceValue != definition)
                        {
                            continue;
                        }

                        DeleteArrayElement(candidates, index);
                        changed = true;
                    }

                    if (!changed)
                    {
                        return true;
                    }

                    serializedSelector.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selector);
                    return EditorSceneManager.SaveScene(scene);
                });
        }

        public static bool RemoveDuplicateCandidateEntries(
            SituationDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return WithLoopBase(
                true,
                (scene, selector, _) =>
                {
                    SerializedObject serializedSelector = new(selector);
                    SerializedProperty candidates =
                        serializedSelector.FindProperty("_candidates");
                    bool found = false;
                    bool changed = false;

                    Undo.RecordObject(selector, "Remove Duplicate Situation Candidates");
                    for (int index = candidates.arraySize - 1;
                         index >= 0;
                         index--)
                    {
                        if (candidates.GetArrayElementAtIndex(index)
                                .objectReferenceValue != definition)
                        {
                            continue;
                        }

                        if (!found)
                        {
                            found = true;
                            continue;
                        }

                        DeleteArrayElement(candidates, index);
                        changed = true;
                    }

                    if (!changed)
                    {
                        return true;
                    }

                    serializedSelector.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selector);
                    return EditorSceneManager.SaveScene(scene);
                });
        }

        public static bool HasNullCandidate(out SituationSelector selector)
        {
            SituationSelector foundSelector = null;
            bool result = WithLoopBase(
                false,
                (_, currentSelector, _) =>
                {
                    foundSelector = currentSelector;
                    SerializedObject serializedSelector = new(currentSelector);
                    SerializedProperty candidates =
                        serializedSelector.FindProperty("_candidates");
                    for (int index = 0; index < candidates.arraySize; index++)
                    {
                        if (candidates.GetArrayElementAtIndex(index)
                                .objectReferenceValue == null)
                        {
                            return true;
                        }
                    }

                    return false;
                });
            selector = foundSelector;
            return result;
        }

        public static bool TryGetHomeLayout(out HomeLayoutDefinition layout)
        {
            HomeLayoutDefinition foundLayout = null;
            bool succeeded = WithLoopBase(
                false,
                (_, _, coordinator) =>
                {
                    SerializedObject serializedCoordinator = new(coordinator);
                    foundLayout = serializedCoordinator.FindProperty("_homeLayout")
                        ?.objectReferenceValue as HomeLayoutDefinition;
                    return foundLayout != null;
                });
            layout = foundLayout;
            return succeeded;
        }

        public static bool IsInBuildSettings(string scenePath)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (string.Equals(scene.path, scenePath, StringComparison.Ordinal) &&
                    scene.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        public static void AddToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes =
                new(EditorBuildSettings.scenes);
            for (int index = 0; index < scenes.Count; index++)
            {
                if (!string.Equals(
                        scenes[index].path,
                        scenePath,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                scenes[index].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static bool OpenSituationScene(
            SituationDefinition definition,
            OpenSceneMode mode = OpenSceneMode.Single)
        {
            string scenePath = SituationAuthoringUtility.FindScenePath(
                definition?.SceneName);
            if (string.IsNullOrEmpty(scenePath))
            {
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            EditorSceneManager.OpenScene(scenePath, mode);
            return true;
        }

        public static bool OpenWithHomeLayout(SituationDefinition definition)
        {
            if (definition == null || !TryGetHomeLayout(out HomeLayoutDefinition layout))
            {
                return false;
            }

            string situationPath = SituationAuthoringUtility.FindScenePath(
                definition.SceneName);
            if (string.IsNullOrEmpty(situationPath) ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            List<string> modulePaths = new();
            foreach (string moduleSceneName in layout.ModuleSceneNames)
            {
                string modulePath = SituationAuthoringUtility.FindScenePath(
                    moduleSceneName);
                if (!string.IsNullOrEmpty(modulePath))
                {
                    modulePaths.Add(modulePath);
                }
            }

            if (modulePaths.Count == 0)
            {
                EditorSceneManager.OpenScene(situationPath, OpenSceneMode.Single);
                return true;
            }

            EditorSceneManager.OpenScene(modulePaths[0], OpenSceneMode.Single);
            for (int index = 1; index < modulePaths.Count; index++)
            {
                EditorSceneManager.OpenScene(modulePaths[index], OpenSceneMode.Additive);
            }

            Scene situationScene = EditorSceneManager.OpenScene(
                situationPath,
                OpenSceneMode.Additive);
            SceneManager.SetActiveScene(situationScene);
            return true;
        }

        private static bool WithLoopBase(
            bool _,
            Func<Scene, SituationSelector, DaySceneCoordinator, bool> action)
        {
            Scene scene = SceneManager.GetSceneByPath(
                SituationAuthoringUtility.LoopBaseScenePath);
            bool openedForOperation = !scene.IsValid() || !scene.isLoaded;

            if (openedForOperation)
            {
                scene = EditorSceneManager.OpenScene(
                    SituationAuthoringUtility.LoopBaseScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                SituationSelector selector =
                    SituationAuthoringUtility.FindComponentInScene<SituationSelector>(
                        scene);
                DaySceneCoordinator coordinator =
                    SituationAuthoringUtility.FindComponentInScene<DaySceneCoordinator>(
                        scene);
                if (selector == null || coordinator == null)
                {
                    return false;
                }

                return action(scene, selector, coordinator);
            }
            finally
            {
                if (openedForOperation)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void DeleteArrayElement(
            SerializedProperty array,
            int index)
        {
            int previousSize = array.arraySize;
            array.DeleteArrayElementAtIndex(index);
            if (array.arraySize == previousSize)
            {
                array.DeleteArrayElementAtIndex(index);
            }
        }
    }
}
