using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.GameFlow;
using VirtualRescue.Situations.DryerTowelHazard;

namespace VirtualRescue.EditorTools
{
    public static class DryerTowelHazardLoopSetup
    {
        private const string LoopBaseScenePath =
            "Assets/01_Scenes/Situation/LoopBase.unity";
        private const string SituationSceneFolder =
            "Assets/01_Scenes/Situation/DryerTowelHazard";
        private const string DefinitionFolder =
            "Assets/02_Scripts/00_Loop/Situation/SituationDefinition_SO";
        private const string PowerCordPrefabPath =
            "Assets/03_Prefabs/Interaction/Power Line/Power Cord.prefab";
        private const string PowerSocketPrefabPath =
            "Assets/03_Prefabs/Interaction/Power Line/PowerSocket.prefab";
        private const string TowelPrefabPath =
            "Assets/00_AssetStore/Pandazole_Lowpoly_Asset_Bundle/" +
            "Pandazole Home Interior/Prefabs/Prop_Towel_02.prefab";
        private const string SceneName = "Scenario_Dryer_TowelHazard";
        private const string DefinitionAssetName =
            "SituationDefinition_Dryer_TowelHazard";
        private const string SituationId = "dryer.towel_hazard";

        private static readonly Vector3 SocketPosition =
            new(0f, 1.15f, 0f);
        private static readonly Vector3 DryerPosition =
            new(0f, 0.6f, 0.75f);
        private static readonly Vector3 TowelPosition =
            new(0f, 0.78f, 0.75f);

        [MenuItem("Tools/Virtual Rescue/Setup Dryer Towel Hazard Loop")]
        public static void Execute()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                EnsureFolder(SituationSceneFolder);
                CreateSituationScene();

                SituationDefinition definition = CreateOrUpdateDefinition();
                AddLoopBaseCandidate(definition);
                AddSceneToBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    "Dryer towel hazard loop setup completed successfully.");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void CreateSituationScene()
        {
            GameObject powerCordPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PowerCordPrefabPath);
            GameObject powerSocketPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PowerSocketPrefabPath);
            GameObject towelPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TowelPrefabPath);

            if (powerCordPrefab == null ||
                powerSocketPrefab == null ||
                towelPrefab == null)
            {
                throw new InvalidOperationException(
                    "The power cord, socket, or towel prefab could not be loaded.");
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            GameObject rootObject = new("SituationSceneRoot");
            GameObject scenarioObject = new("DryerTowelHazard");
            scenarioObject.transform.SetParent(rootObject.transform, false);

            GameObject powerSetupObject = new("PowerSetup");
            powerSetupObject.transform.SetParent(scenarioObject.transform, false);

            GameObject powerSocketObject = InstantiatePrefab(
                powerSocketPrefab,
                scene,
                powerSetupObject.transform,
                "WallPowerSocket");
            powerSocketObject.transform.SetPositionAndRotation(
                SocketPosition,
                Quaternion.identity);

            XRSocketInteractor powerSocket =
                powerSocketObject.GetComponent<XRSocketInteractor>();

            if (powerSocket == null)
            {
                throw new InvalidOperationException(
                    "The power socket prefab does not contain XRSocketInteractor.");
            }

            GameObject powerCordObject = InstantiatePrefab(
                powerCordPrefab,
                scene,
                powerSetupObject.transform,
                "DryerPowerCord");
            powerCordObject.transform.SetPositionAndRotation(
                SocketPosition,
                Quaternion.identity);

            Transform plugTransform = powerCordObject.transform.Find("Plug");
            Transform cordEndTransform =
                powerCordObject.transform.Find("PowerCordEnd");

            if (plugTransform == null || cordEndTransform == null)
            {
                throw new InvalidOperationException(
                    "The power cord prefab does not contain Plug or PowerCordEnd.");
            }

            XRGrabInteractable plug =
                plugTransform.GetComponent<XRGrabInteractable>();

            if (plug == null)
            {
                throw new InvalidOperationException(
                    "The dryer plug does not contain XRGrabInteractable.");
            }

            cordEndTransform.position = DryerPosition;
            ConfigureStartingSelection(powerSocket, plug);

            AudioSource operatingAudioSource =
                CreateDryerPlaceholder(cordEndTransform);

            GameObject towelObject = InstantiatePrefab(
                towelPrefab,
                scene,
                scenarioObject.transform,
                "Towel_HazardCover");
            towelObject.transform.SetPositionAndRotation(
                TowelPosition,
                Quaternion.Euler(90f, 0f, 0f));
            ConfigureTowelInteraction(towelObject);

            DryerTowelHazardSituationController controller =
                scenarioObject.AddComponent<DryerTowelHazardSituationController>();
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("_powerSocket").objectReferenceValue =
                powerSocket;
            serializedController.FindProperty(
                "_operatingAudioSource").objectReferenceValue =
                operatingAudioSource;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SituationSceneRoot sceneRoot =
                rootObject.AddComponent<SituationSceneRoot>();
            SerializedObject serializedRoot = new(sceneRoot);
            serializedRoot.FindProperty("_controller").objectReferenceValue =
                controller;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();

            string scenePath = $"{SituationSceneFolder}/{SceneName}.unity";

            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save situation scene: {scenePath}");
            }
        }

        private static GameObject InstantiatePrefab(
            GameObject prefab,
            Scene scene,
            Transform parent,
            string objectName)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab,
                scene);
            instance.name = objectName;
            instance.transform.SetParent(parent, true);
            return instance;
        }

        private static void ConfigureStartingSelection(
            XRSocketInteractor powerSocket,
            XRGrabInteractable plug)
        {
            SerializedObject serializedSocket = new(powerSocket);
            SerializedProperty startingSelection =
                serializedSocket.FindProperty("m_StartingSelectedInteractable");

            if (startingSelection == null)
            {
                throw new InvalidOperationException(
                    "XRSocketInteractor starting selection could not be configured.");
            }

            startingSelection.objectReferenceValue = plug;
            serializedSocket.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioSource CreateDryerPlaceholder(Transform cordEnd)
        {
            GameObject placeholderObject = new("DryerPlaceholder");
            placeholderObject.transform.SetPositionAndRotation(
                cordEnd.position,
                Quaternion.identity);
            placeholderObject.transform.SetParent(cordEnd, true);

            GameObject bodyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyObject.name = "TemporaryDryerBody";
            bodyObject.transform.SetParent(placeholderObject.transform, false);
            bodyObject.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            bodyObject.transform.localScale = new Vector3(0.13f, 0.24f, 0.13f);

            GameObject handleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handleObject.name = "TemporaryDryerHandle";
            handleObject.transform.SetParent(placeholderObject.transform, false);
            handleObject.transform.localPosition = new Vector3(-0.06f, -0.2f, 0f);
            handleObject.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
            handleObject.transform.localScale = new Vector3(0.11f, 0.3f, 0.12f);

            AudioSource audioSource = placeholderObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 0.5f;
            audioSource.maxDistance = 8f;
            return audioSource;
        }

        private static void ConfigureTowelInteraction(GameObject towelObject)
        {
            if (towelObject.GetComponentInChildren<Collider>(true) == null)
            {
                throw new InvalidOperationException(
                    "The towel prefab does not contain a collider.");
            }

            Rigidbody rigidbody = towelObject.GetComponent<Rigidbody>();

            if (rigidbody == null)
            {
                rigidbody = towelObject.AddComponent<Rigidbody>();
            }

            rigidbody.mass = 0.25f;
            rigidbody.useGravity = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode =
                CollisionDetectionMode.ContinuousSpeculative;

            if (towelObject.GetComponent<XRGrabInteractable>() == null)
            {
                towelObject.AddComponent<XRGrabInteractable>();
            }
        }

        private static SituationDefinition CreateOrUpdateDefinition()
        {
            string assetPath =
                $"{DefinitionFolder}/{DefinitionAssetName}.asset";
            SituationDefinition definition =
                AssetDatabase.LoadAssetAtPath<SituationDefinition>(assetPath);

            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<SituationDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("_id").stringValue = SituationId;
            serializedDefinition.FindProperty("_level").enumValueIndex =
                (int)SituationLevel.Level1;
            serializedDefinition.FindProperty("_weight").intValue = 1;
            serializedDefinition.FindProperty("_minimumDay").intValue = 1;
            serializedDefinition.FindProperty("_sceneName").stringValue = SceneName;
            serializedDefinition.FindProperty("_usesTimeLimit").boolValue = false;
            serializedDefinition.FindProperty("_timeLimitSeconds").floatValue = 60f;
            serializedDefinition.FindProperty("_level2AllowedExits").arraySize = 0;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void AddLoopBaseCandidate(SituationDefinition definition)
        {
            Scene loopBaseScene = EditorSceneManager.OpenScene(
                LoopBaseScenePath,
                OpenSceneMode.Single);
            SituationSelector selector =
                UnityEngine.Object.FindFirstObjectByType<SituationSelector>();

            if (selector == null)
            {
                throw new InvalidOperationException(
                    "LoopBase does not contain a SituationSelector.");
            }

            SerializedObject serializedSelector = new(selector);
            SerializedProperty candidates =
                serializedSelector.FindProperty("_candidates");

            for (int index = 0; index < candidates.arraySize; index++)
            {
                if (candidates.GetArrayElementAtIndex(index).objectReferenceValue ==
                    definition)
                {
                    SaveLoopBaseScene(loopBaseScene);
                    return;
                }
            }

            int newIndex = candidates.arraySize;
            candidates.arraySize++;
            candidates.GetArrayElementAtIndex(newIndex).objectReferenceValue =
                definition;
            serializedSelector.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(selector);
            SaveLoopBaseScene(loopBaseScene);
        }

        private static void SaveLoopBaseScene(Scene loopBaseScene)
        {
            if (!EditorSceneManager.SaveScene(loopBaseScene))
            {
                throw new InvalidOperationException("Failed to save LoopBase scene.");
            }
        }

        private static void AddSceneToBuildSettings()
        {
            string scenePath = $"{SituationSceneFolder}/{SceneName}.unity";
            List<EditorBuildSettingsScene> scenes =
                new(EditorBuildSettings.scenes);

            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path == scenePath)
                {
                    scene.enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string currentPath = segments[0];

            for (int index = 1; index < segments.Length; index++)
            {
                string nextPath = $"{currentPath}/{segments[index]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
