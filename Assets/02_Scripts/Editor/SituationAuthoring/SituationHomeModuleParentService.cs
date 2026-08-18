using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal static class SituationHomeModuleParentService
    {
        public sealed class Result
        {
            public int CreatedCount { get; set; }
            public int ExistingCount { get; set; }
            public List<string> MissingSceneNames { get; } = new();
            public List<string> UpdatedSceneNames { get; } = new();
            public List<string> ExistingSceneNames { get; } = new();
        }

        public static Result CreateParents(
            string parentName,
            IReadOnlyList<string> moduleSceneNames)
        {
            if (string.IsNullOrWhiteSpace(parentName))
            {
                throw new InvalidOperationException("Parent object name is required.");
            }

            if (moduleSceneNames == null || moduleSceneNames.Count == 0)
            {
                throw new InvalidOperationException(
                    "Select at least one Home Layout module scene.");
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException(
                    "Home module parent creation was cancelled.");
            }

            Result result = new();
            foreach (string sceneName in moduleSceneNames.Distinct())
            {
                string scenePath = SituationAuthoringUtility.FindScenePath(sceneName);
                if (string.IsNullOrEmpty(scenePath))
                {
                    result.MissingSceneNames.Add(sceneName);
                    continue;
                }

                Scene scene = SceneManager.GetSceneByPath(scenePath);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(
                        scenePath,
                        OpenSceneMode.Additive);
                }

                if (FindRoot(scene, parentName) != null)
                {
                    result.ExistingCount++;
                    result.ExistingSceneNames.Add(sceneName);
                    continue;
                }

                GameObject parent = new(parentName.Trim());
                SceneManager.MoveGameObjectToScene(parent, scene);
                parent.transform.SetPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                parent.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(
                    parent,
                    "Create Situation Home Module Parent");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                result.CreatedCount++;
                result.UpdatedSceneNames.Add(sceneName);
            }

            return result;
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(
                        root.name,
                        objectName.Trim(),
                        StringComparison.Ordinal))
                {
                    return root;
                }
            }

            return null;
        }
    }
}
