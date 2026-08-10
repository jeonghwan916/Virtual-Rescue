using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.GameFlow;
using VirtualRescue.Situations.KitchenOilFire;

namespace VirtualRescue.EditorTools
{
    public static class KitchenOilFireLoopSetup
    {
        private const string KitchenScenePath =
            "Assets/01_Scenes/S_MainHome/Kitchen&LivingRoom.unity";
        private const string LoopBaseScenePath =
            "Assets/01_Scenes/Situation/LoopBase.unity";
        private const string SituationSceneFolder =
            "Assets/01_Scenes/Situation/KitchenOilFire";
        private const string DefinitionFolder =
            "Assets/02_Scripts/00_Loop/Situation/SituationDefinition_SO";
        private const string FirePrefabPath =
            "Assets/03_Prefabs/Particles/Fire/Fire_Small_Effect.prefab";
        private const string ExtinguisherPrefabPath =
            "Assets/03_Prefabs/Interaction/Fire_Extinguisher.prefab";
        private const string StoveObjectName = "Prop_SurfaceStove_01";
        private const string SceneName = "Scenario_Kitchen_OilFire";
        private const string DefinitionAssetName =
            "SituationDefinition_Kitchen_OilFire";
        private const string SituationId = "kitchen.oil_fire";

        private static readonly Vector3 NormalExtinguisherPosition =
            new(-2.5f, 0.1f, 7f);
        private static readonly Vector3 IncompatibleExtinguisherPosition =
            new(-1.75f, 0.1f, 7f);

        [MenuItem("Tools/Virtual Rescue/Setup Kitchen Oil Fire Loop")]
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
                Vector3 firePosition = FindStoveFirePosition();
                CreateSituationScene(firePosition);

                SituationDefinition definition = CreateOrUpdateDefinition();
                AddLoopBaseCandidate(definition);
                AddSceneToBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Kitchen oil fire loop setup completed successfully.");

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

        private static Vector3 FindStoveFirePosition()
        {
            Scene kitchenScene = EditorSceneManager.OpenScene(
                KitchenScenePath,
                OpenSceneMode.Single);
            GameObject stoveObject = FindGameObject(
                kitchenScene,
                StoveObjectName);

            if (stoveObject == null)
            {
                throw new InvalidOperationException(
                    $"The kitchen stove '{StoveObjectName}' was not found.");
            }

            Renderer[] renderers = stoveObject.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "The kitchen stove does not contain a renderer.");
            }

            Bounds bounds = renderers[0].bounds;

            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return new Vector3(
                bounds.center.x,
                bounds.max.y + 0.05f,
                bounds.center.z);
        }

        private static void CreateSituationScene(Vector3 firePosition)
        {
            GameObject firePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(FirePrefabPath);
            GameObject extinguisherPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ExtinguisherPrefabPath);

            if (firePrefab == null || extinguisherPrefab == null)
            {
                throw new InvalidOperationException(
                    "The fire or extinguisher prefab could not be loaded.");
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            GameObject rootObject = new("SituationSceneRoot");
            GameObject scenarioObject = new("KitchenOilFire");
            scenarioObject.transform.SetParent(rootObject.transform, false);

            GameObject fireObject = (GameObject)PrefabUtility.InstantiatePrefab(
                firePrefab,
                scene);
            fireObject.name = "KitchenOilFireEffect";
            fireObject.transform.SetParent(scenarioObject.transform, true);
            fireObject.transform.position = firePosition;

            FireObject oilFire = fireObject.GetComponent<FireObject>();

            if (oilFire == null)
            {
                throw new InvalidOperationException(
                    "The fire prefab does not contain a FireObject component.");
            }

            ConfigureOilFire(oilFire);

            GameObject normalExtinguisher = CreateExtinguisher(
                extinguisherPrefab,
                scene,
                scenarioObject.transform,
                "Fire_Extinguisher_GeneralPurpose",
                NormalExtinguisherPosition,
                FireSuppressantType.GeneralPurpose,
                false);
            normalExtinguisher.transform.rotation = Quaternion.identity;

            GameObject incompatibleExtinguisher = CreateExtinguisher(
                extinguisherPrefab,
                scene,
                scenarioObject.transform,
                "Fire_Extinguisher_OilIncompatible",
                IncompatibleExtinguisherPosition,
                FireSuppressantType.OilFireIncompatible,
                true);
            incompatibleExtinguisher.transform.rotation = Quaternion.identity;

            KitchenOilFireSituationController controller =
                scenarioObject.AddComponent<KitchenOilFireSituationController>();
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("_oilFire").objectReferenceValue = oilFire;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SituationSceneRoot sceneRoot = rootObject.AddComponent<SituationSceneRoot>();
            SerializedObject serializedRoot = new(sceneRoot);
            serializedRoot.FindProperty("_controller").objectReferenceValue = controller;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();

            string scenePath = $"{SituationSceneFolder}/{SceneName}.unity";

            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save situation scene: {scenePath}");
            }
        }

        private static GameObject CreateExtinguisher(
            GameObject extinguisherPrefab,
            Scene scene,
            Transform parent,
            string objectName,
            Vector3 position,
            FireSuppressantType suppressantType,
            bool unpackCompletely)
        {
            GameObject extinguisher = (GameObject)PrefabUtility.InstantiatePrefab(
                extinguisherPrefab,
                scene);
            extinguisher.name = objectName;
            extinguisher.transform.SetParent(parent, true);
            extinguisher.transform.position = position;

            if (unpackCompletely)
            {
                PrefabUtility.UnpackPrefabInstance(
                    extinguisher,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            FireExtinguisher fireExtinguisher =
                extinguisher.GetComponentInChildren<FireExtinguisher>(true);

            if (fireExtinguisher == null)
            {
                throw new InvalidOperationException(
                    "The extinguisher prefab does not contain FireExtinguisher.");
            }

            SerializedObject serializedExtinguisher = new(fireExtinguisher);
            serializedExtinguisher.FindProperty("_suppressantType").enumValueIndex =
                (int)suppressantType;
            serializedExtinguisher.ApplyModifiedPropertiesWithoutUndo();
            return extinguisher;
        }

        private static void ConfigureOilFire(FireObject oilFire)
        {
            SerializedObject serializedFire = new(oilFire);
            SerializedProperty temporarySuppressants =
                serializedFire.FindProperty("_temporaryOnlySuppressants");
            temporarySuppressants.arraySize = 1;
            temporarySuppressants.GetArrayElementAtIndex(0).enumValueIndex =
                (int)FireSuppressantType.OilFireIncompatible;
            serializedFire.FindProperty("_maximumTemporarySuppression").floatValue =
                0.75f;
            serializedFire.FindProperty("_temporaryRecoveryDelay").floatValue = 0.15f;
            serializedFire.FindProperty("_temporaryRecoveryDuration").floatValue = 3f;
            serializedFire.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SituationDefinition CreateOrUpdateDefinition()
        {
            string assetPath = $"{DefinitionFolder}/{DefinitionAssetName}.asset";
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
            serializedDefinition.FindProperty("_timeLimitSeconds").floatValue = 0f;
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
            SerializedProperty candidates = serializedSelector.FindProperty("_candidates");

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
            candidates.GetArrayElementAtIndex(newIndex).objectReferenceValue = definition;
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
            List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);

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

        private static GameObject FindGameObject(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                Transform[] transforms =
                    rootObject.GetComponentsInChildren<Transform>(true);

                foreach (Transform transform in transforms)
                {
                    if (transform.name == objectName)
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
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
