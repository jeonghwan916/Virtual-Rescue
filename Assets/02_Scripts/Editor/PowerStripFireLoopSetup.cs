using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.GameFlow;
using VirtualRescue.Situations.PowerStripFire;

namespace VirtualRescue.EditorTools
{
    public static class PowerStripFireLoopSetup
    {
        private const string PowerStripSourceScenePath =
            "Assets/01_Scenes/Situation/PowerStripUnplug/Scenario_PowerStrip_Unplug.unity";
        private const string LoopBaseScenePath =
            "Assets/01_Scenes/Situation/LoopBase.unity";
        private const string SituationSceneFolder =
            "Assets/01_Scenes/Situation/PowerStripFire";
        private const string DefinitionFolder =
            "Assets/02_Scripts/00_Loop/SituationDefinition_SO";
        private const string FirePrefabPath =
            "Assets/03_Prefabs/Particles/Fire/Fire_Small_Effect.prefab";
        private const string ExtinguisherPrefabPath =
            "Assets/03_Prefabs/Interaction/Fire_Extinguisher.prefab";
        private const string SceneName = "Scenario_PowerStrip_Fire";
        private const string DefinitionAssetName =
            "SituationDefinition_PowerStrip_Fire";
        private const string SituationId = "power_strip.fire";

        [MenuItem("Tools/Virtual Rescue/Setup Power Strip Fire Loop")]
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

                Scene sourceScene = EditorSceneManager.OpenScene(
                    PowerStripSourceScenePath,
                    OpenSceneMode.Single);
                GameObject sourcePowerStrip = FindGameObject(
                    sourceScene,
                    "Power Strip");

                if (sourcePowerStrip == null)
                {
                    throw new InvalidOperationException(
                        "The four-outlet power strip source was not found.");
                }

                CreateSituationScene(sourcePowerStrip);
                SituationDefinition definition = CreateOrUpdateDefinition();
                AddLoopBaseCandidate(definition);
                AddSceneToBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Power strip fire loop setup completed successfully.");

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

        private static void CreateSituationScene(GameObject sourcePowerStrip)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);

            GameObject rootObject = new("SituationSceneRoot");
            SceneManager.MoveGameObjectToScene(rootObject, scene);

            GameObject scenarioObject = new("PowerStripFire");
            SceneManager.MoveGameObjectToScene(scenarioObject, scene);
            scenarioObject.transform.SetParent(rootObject.transform, false);

            GameObject powerStrip = UnityEngine.Object.Instantiate(sourcePowerStrip);
            SceneManager.MoveGameObjectToScene(powerStrip, scene);
            powerStrip.name = "Power Strip";
            powerStrip.transform.SetParent(scenarioObject.transform, true);
            ClearStartingSelections(powerStrip);

            GameObject firePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(FirePrefabPath);
            GameObject extinguisherPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ExtinguisherPrefabPath);

            if (firePrefab == null || extinguisherPrefab == null)
            {
                throw new InvalidOperationException(
                    "The fire or extinguisher prefab could not be loaded.");
            }

            GameObject fireObject = (GameObject)PrefabUtility.InstantiatePrefab(
                firePrefab,
                scene);
            fireObject.name = "PowerStripFireEffect";
            fireObject.transform.SetParent(scenarioObject.transform, true);
            fireObject.transform.position = GetFirePosition(powerStrip);

            FireObject fire = fireObject.GetComponent<FireObject>();

            if (fire == null)
            {
                throw new InvalidOperationException(
                    "The fire prefab does not contain a FireObject component.");
            }

            GameObject extinguisher = (GameObject)PrefabUtility.InstantiatePrefab(
                extinguisherPrefab,
                scene);
            extinguisher.name = "Fire_Extinguisher";
            extinguisher.transform.SetParent(scenarioObject.transform, true);
            extinguisher.transform.position = new Vector3(
                powerStrip.transform.position.x - 0.8f,
                0f,
                powerStrip.transform.position.z - 0.3f);
            extinguisher.transform.rotation = Quaternion.identity;
            PrefabUtility.UnpackPrefabInstance(
                extinguisher,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);

            PowerStripFireSituationController controller =
                scenarioObject.AddComponent<PowerStripFireSituationController>();
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("_fireObject").objectReferenceValue = fire;
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

            EditorSceneManager.CloseScene(scene, true);
        }

        private static Vector3 GetFirePosition(GameObject powerStrip)
        {
            Renderer[] renderers = powerStrip.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                return powerStrip.transform.position + Vector3.up * 0.1f;
            }

            Bounds bounds = renderers[0].bounds;

            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return new Vector3(bounds.center.x, bounds.max.y + 0.05f, bounds.center.z);
        }

        private static void ClearStartingSelections(GameObject powerStrip)
        {
            XRSocketInteractor[] sockets =
                powerStrip.GetComponentsInChildren<XRSocketInteractor>(true);

            foreach (XRSocketInteractor socket in sockets)
            {
                SerializedObject serializedSocket = new(socket);
                SerializedProperty startingSelection =
                    serializedSocket.FindProperty("m_StartingSelectedInteractable");

                if (startingSelection == null)
                {
                    continue;
                }

                startingSelection.objectReferenceValue = null;
                serializedSocket.ApplyModifiedPropertiesWithoutUndo();
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
