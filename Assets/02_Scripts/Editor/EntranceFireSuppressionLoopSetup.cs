using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.GameFlow;
using VirtualRescue.Situations.EntranceFireSuppression;

namespace VirtualRescue.EditorTools
{
    public static class EntranceFireSuppressionLoopSetup
    {
        private const string LoopBaseScenePath =
            "Assets/01_Scenes/Situation/LoopBase.unity";
        private const string SituationSceneFolder =
            "Assets/01_Scenes/Situation/EntranceFireSuppression";
        private const string DefinitionFolder =
            "Assets/02_Scripts/00_Loop/Situation/SituationDefinition_SO";
        private const string FirePrefabPath =
            "Assets/03_Prefabs/Particles/Fire/Fire_Small_Effect.prefab";
        private const string FireHydrantPrefabPath =
            "Assets/03_Prefabs/Interaction/Fire Hydrant Cabinet/" +
            "FireHydrantCabinet_withHose.prefab";
        private const string SceneName = "Scenario_Entrance_FireSuppression";
        private const string DefinitionAssetName =
            "SituationDefinition_Entrance_FireSuppression";
        private const string SituationId = "entrance.fire_suppression";

        private static readonly string[] ExistingSituationDefinitionPaths =
        {
            DefinitionFolder + "/SituationDefinition_PowerStrip_Unplug.asset",
            DefinitionFolder + "/SituationDefinition_LightweightPartition_Escape.asset",
            DefinitionFolder + "/SituationDefinition_PowerStrip_Fire.asset"
        };

        private static readonly Vector3[] FirePositions =
        {
            new(11.8f, 0f, 14.6f),
            new(12.5f, 0f, 15.2f),
            new(11.4f, 0f, 16f)
        };

        [MenuItem("Tools/Virtual Rescue/Setup Entrance Fire Suppression Loop")]
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
                AddLoopBaseCandidates(definition);
                AddSceneToBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Entrance fire suppression loop setup completed successfully.");

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
            GameObject firePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(FirePrefabPath);
            GameObject fireHydrantPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(FireHydrantPrefabPath);

            if (firePrefab == null || fireHydrantPrefab == null)
            {
                throw new InvalidOperationException(
                    "The fire or fire hydrant prefab could not be loaded.");
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            GameObject rootObject = new("SituationSceneRoot");
            GameObject scenarioObject = new("EntranceFireSuppression");
            scenarioObject.transform.SetParent(rootObject.transform, false);

            GameObject fireCandidatesObject = new("FireCandidates");
            fireCandidatesObject.transform.SetParent(scenarioObject.transform, false);

            List<FireObject> fireCandidates = new();

            for (int index = 0; index < FirePositions.Length; index++)
            {
                GameObject fireObject = (GameObject)PrefabUtility.InstantiatePrefab(
                    firePrefab,
                    scene);
                fireObject.name = $"EntranceFire_{index + 1:00}";
                fireObject.transform.SetParent(fireCandidatesObject.transform, true);
                fireObject.transform.position = FirePositions[index];

                FireObject fire = fireObject.GetComponent<FireObject>();

                if (fire == null)
                {
                    throw new InvalidOperationException(
                        "The fire prefab does not contain a FireObject component.");
                }

                fireCandidates.Add(fire);
                fireObject.SetActive(false);
            }

            GameObject fireHydrant = (GameObject)PrefabUtility.InstantiatePrefab(
                fireHydrantPrefab,
                scene);
            fireHydrant.name = "FireHydrantCabinet_withHose";
            fireHydrant.transform.SetParent(scenarioObject.transform, true);
            fireHydrant.transform.SetPositionAndRotation(
                new Vector3(14f, 0.6f, 16.410517f),
                Quaternion.Euler(0f, -90f, 0f));

            EntranceFireSuppressionSituationController controller =
                scenarioObject.AddComponent<EntranceFireSuppressionSituationController>();
            SerializedObject serializedController = new(controller);
            SerializedProperty fireCandidatesProperty =
                serializedController.FindProperty("_fireCandidates");
            fireCandidatesProperty.arraySize = fireCandidates.Count;

            for (int index = 0; index < fireCandidates.Count; index++)
            {
                fireCandidatesProperty.GetArrayElementAtIndex(index).objectReferenceValue =
                    fireCandidates[index];
            }

            serializedController.FindProperty("_activeFireCount").intValue =
                fireCandidates.Count;
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

        private static void AddLoopBaseCandidates(SituationDefinition definition)
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

            List<SituationDefinition> definitions = new() { definition };

            foreach (string assetPath in ExistingSituationDefinitionPaths)
            {
                SituationDefinition existingDefinition =
                    AssetDatabase.LoadAssetAtPath<SituationDefinition>(assetPath);

                if (existingDefinition != null)
                {
                    definitions.Add(existingDefinition);
                }
            }

            SerializedObject serializedSelector = new(selector);
            SerializedProperty candidates = serializedSelector.FindProperty("_candidates");

            foreach (SituationDefinition candidate in definitions)
            {
                AddCandidateIfMissing(candidates, candidate);
            }

            serializedSelector.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(selector);

            if (!EditorSceneManager.SaveScene(loopBaseScene))
            {
                throw new InvalidOperationException("Failed to save LoopBase scene.");
            }
        }

        private static void AddCandidateIfMissing(
            SerializedProperty candidates,
            SituationDefinition definition)
        {
            for (int index = 0; index < candidates.arraySize; index++)
            {
                if (candidates.GetArrayElementAtIndex(index).objectReferenceValue ==
                    definition)
                {
                    return;
                }
            }

            int newIndex = candidates.arraySize;
            candidates.arraySize++;
            candidates.GetArrayElementAtIndex(newIndex).objectReferenceValue = definition;
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
