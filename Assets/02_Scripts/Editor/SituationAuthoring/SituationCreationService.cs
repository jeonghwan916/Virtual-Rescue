using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.GameFlow;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal static class SituationCreationService
    {
        public static SituationDefinition Create(
            SituationCreationRequest request,
            Type controllerType)
        {
            if (request == null || controllerType == null ||
                !typeof(SituationController).IsAssignableFrom(controllerType) ||
                controllerType.IsAbstract)
            {
                throw new InvalidOperationException(
                    "A concrete SituationController type is required.");
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException(
                    "Situation creation was cancelled before scene changes.");
            }

            EnsureDestinationsAreAvailable(request);
            SituationAuthoringUtility.EnsureFolder(request.SceneFolder);
            SituationAuthoringUtility.EnsureFolder(
                SituationLocationPathMap.DefinitionRoot);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            GameObject rootObject = new("SituationSceneRoot");
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            SituationSceneRoot sceneRoot =
                rootObject.AddComponent<SituationSceneRoot>();

            GameObject situationObject = new(request.displayName.Trim());
            SceneManager.MoveGameObjectToScene(situationObject, scene);
            situationObject.transform.SetParent(rootObject.transform, false);
            SituationController controller =
                situationObject.AddComponent(controllerType) as SituationController;
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"Failed to add controller {request.ControllerFullName}.");
            }

            SituationAuthoringUtility.SetObjectReference(
                sceneRoot,
                "_controller",
                controller);

            AddInitialPrefabs(request.initialPrefabPaths, scene, situationObject);
            AddBuildingBlocks(request, situationObject, controller);

            if (!EditorSceneManager.SaveScene(scene, request.ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save situation scene: {request.ScenePath}");
            }

            SituationDefinition definition = CreateDefinition(request);
            SituationRegistrationService.AddToBuildSettings(request.ScenePath);
            if (request.registerAsCandidate &&
                !SituationRegistrationService.RegisterCandidate(definition))
            {
                Debug.LogError(
                    "The situation was created, but its Definition could not be " +
                    "registered in LoopBase. Use Register Candidate in the Wizard.",
                    definition);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return definition;
        }

        private static void EnsureDestinationsAreAvailable(
            SituationCreationRequest request)
        {
            if (System.IO.File.Exists(
                    SituationAuthoringUtility.ToAbsolutePath(request.ScenePath)))
            {
                throw new InvalidOperationException(
                    $"Situation scene already exists: {request.ScenePath}");
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    request.DefinitionPath) != null)
            {
                throw new InvalidOperationException(
                    $"Situation definition already exists: " +
                    request.DefinitionPath);
            }
        }

        private static SituationDefinition CreateDefinition(
            SituationCreationRequest request)
        {
            SituationDefinition definition =
                ScriptableObject.CreateInstance<SituationDefinition>();
            AssetDatabase.CreateAsset(definition, request.DefinitionPath);

            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("_id").stringValue =
                request.situationId.Trim();
            serializedDefinition.FindProperty("_level").enumValueIndex =
                request.level;
            serializedDefinition.FindProperty("_weight").intValue =
                request.weight;
            serializedDefinition.FindProperty("_minimumDay").intValue =
                request.minimumDay;
            serializedDefinition.FindProperty("_sceneName").stringValue =
                request.sceneName.Trim();
            serializedDefinition.FindProperty("_roomLocation").enumValueIndex =
                request.roomLocation;
            serializedDefinition.FindProperty("_usesTimeLimit").boolValue =
                request.Level == SituationLevel.Level2 && request.usesTimeLimit;
            serializedDefinition.FindProperty("_timeLimitSeconds").floatValue =
                request.timeLimitSeconds;

            SerializedProperty exits = serializedDefinition.FindProperty(
                "_level2AllowedExits");
            int[] allowedExits = request.Level == SituationLevel.Level2
                ? request.allowedExits ?? Array.Empty<int>()
                : Array.Empty<int>();
            exits.arraySize = allowedExits.Length;
            for (int index = 0; index < allowedExits.Length; index++)
            {
                exits.GetArrayElementAtIndex(index).enumValueIndex =
                    allowedExits[index];
            }

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void AddInitialPrefabs(
            IEnumerable<string> prefabPaths,
            Scene scene,
            GameObject parent)
        {
            if (prefabPaths == null)
            {
                return;
            }

            foreach (string prefabPath in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabPath);
                if (prefab == null)
                {
                    continue;
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(
                    prefab,
                    scene) as GameObject;
                if (instance != null)
                {
                    instance.transform.SetParent(parent.transform, false);
                }
            }
        }

        private static void AddBuildingBlocks(
            SituationCreationRequest request,
            GameObject target,
            SituationController controller)
        {
            List<ModuleObjectId> moduleIds = LoadAssets<ModuleObjectId>(
                request.moduleObjectIdPaths);
            if (moduleIds.Count > 0)
            {
                SituationObjectOverride component =
                    target.AddComponent<SituationObjectOverride>();
                SituationAuthoringUtility.SetObjectReference(
                    component,
                    "_situationController",
                    controller);
                SituationAuthoringUtility.SetObjectArray(
                    component,
                    "_moduleObjectIds",
                    moduleIds);
            }

            List<DoorId> lockedDoorIds = LoadAssets<DoorId>(
                request.lockedDoorIdPaths);
            if (lockedDoorIds.Count > 0)
            {
                SituationDoorLockOverride component =
                    target.AddComponent<SituationDoorLockOverride>();
                SituationAuthoringUtility.SetObjectReference(
                    component,
                    "_situationController",
                    controller);
                SituationAuthoringUtility.SetObjectArray(
                    component,
                    "_doorIds",
                    lockedDoorIds);
            }

            List<DoorId> trapDoorIds = LoadAssets<DoorId>(
                request.trapDoorIdPaths);
            if (trapDoorIds.Count > 0)
            {
                SituationTrapDoorTrigger component =
                    target.AddComponent<SituationTrapDoorTrigger>();
                SituationAuthoringUtility.SetObjectReference(
                    component,
                    "_situationController",
                    controller);
                SituationAuthoringUtility.SetObjectArray(
                    component,
                    "_doorIds",
                    trapDoorIds);
            }
        }

        private static List<T> LoadAssets<T>(IEnumerable<string> paths)
            where T : UnityEngine.Object
        {
            List<T> assets = new();
            if (paths == null)
            {
                return assets;
            }

            foreach (string path in paths)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && !assets.Contains(asset))
                {
                    assets.Add(asset);
                }
            }

            return assets;
        }
    }
}
