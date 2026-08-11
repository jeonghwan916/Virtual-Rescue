using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.GameFlow;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal static class SituationBuildingBlockService
    {
        public static bool TryGetCurrentController(
            out SituationController controller)
        {
            Scene scene = SceneManager.GetActiveScene();
            controller = scene.IsValid() && scene.isLoaded
                ? SituationAuthoringUtility
                    .FindComponentInScene<SituationController>(scene)
                : null;
            return controller != null;
        }

        public static T AddComponent<T>(
            GameObject target,
            SituationController controller,
            string idPropertyName,
            IReadOnlyList<UnityEngine.Object> ids)
            where T : Component
        {
            if (target == null || controller == null ||
                target.scene != controller.gameObject.scene)
            {
                throw new InvalidOperationException(
                    "Target and Controller must belong to the active situation scene.");
            }

            T existing = target.GetComponent<T>();
            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"{target.name} already contains {typeof(T).Name}.");
            }

            T component = Undo.AddComponent<T>(target);
            SituationAuthoringUtility.SetObjectReference(
                component,
                "_situationController",
                controller);

            if (!string.IsNullOrEmpty(idPropertyName))
            {
                SerializedObject serializedObject = new(component);
                SerializedProperty property =
                    serializedObject.FindProperty(idPropertyName);
                property.arraySize = ids?.Count ?? 0;
                for (int index = 0; index < property.arraySize; index++)
                {
                    property.GetArrayElementAtIndex(index).objectReferenceValue =
                        ids[index];
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(target.scene);
            Selection.activeGameObject = target;
            return component;
        }

        public static GameObject AddPrefab(
            GameObject prefab,
            GameObject parent)
        {
            if (prefab == null || parent == null)
            {
                throw new InvalidOperationException(
                    "A prefab and a target parent are required.");
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                parent.scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Failed to instantiate prefab '{prefab.name}'.");
            }

            Undo.RegisterCreatedObjectUndo(instance, "Add Situation Prefab");
            instance.transform.SetParent(parent.transform, false);
            EditorSceneManager.MarkSceneDirty(parent.scene);
            Selection.activeGameObject = instance;
            return instance;
        }
    }
}
