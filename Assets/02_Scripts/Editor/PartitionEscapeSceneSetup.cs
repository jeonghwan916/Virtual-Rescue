using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VirtualRescue.Destruction;
using VirtualRescue.Missions08;

namespace VirtualRescue.Editor
{
    public static class PartitionEscapeSceneSetup
    {
        private const string MissionScenePath =
            "Assets/01_Scenes/Missions/05_PartitionEscape.unity";
        private const string EnvironmentScenePath =
            "Assets/01_Scenes/S_Env/S_Env.unity";
        private const string PartitionName = "Lightweight_Partitions";
        private const string GeneratedRootName = "Lightweight_PartitionsFragments";
        private const string BatName = "Destruction_Bat";
        private const string BatGrabAttachName = "BatGrabAttach";
        private const string ToolTag = "DestructionTool";
        private const string MeshOutputFolder =
            "Assets/05_Models/Generated/BreakablePartitions/PartitionEscape";

        [MenuItem("Tools/Virtual Rescue/Setup Partition Escape Scene")]
        public static void SetupLoadedScenes()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Exit Play Mode before setting up Partition Escape.");
                return;
            }

            Scene environmentScene = FindLoadedScene(EnvironmentScenePath);
            Scene missionScene = FindLoadedScene(MissionScenePath);
            if (!environmentScene.IsValid() || !missionScene.IsValid())
            {
                Debug.LogError(
                    "Load both 05_PartitionEscape and S_Env before running the setup.");
                return;
            }

            GameObject partition = FindSceneObject(environmentScene, PartitionName);
            GameObject bat = FindSceneObject(missionScene, BatName);
            if (partition == null || bat == null)
            {
                Debug.LogError(
                    $"Could not find '{PartitionName}' or '{BatName}' in the loaded scenes.");
                return;
            }

            ConfigurePartition(partition);
            ConfigureBat(bat);

            EditorSceneManager.MarkSceneDirty(environmentScene);
            EditorSceneManager.MarkSceneDirty(missionScene);
            EditorSceneManager.SaveScene(environmentScene);
            EditorSceneManager.SaveScene(missionScene);
            AssetDatabase.SaveAssets();

            Debug.Log("PARTITION_ESCAPE_SCENE_SETUP_OK", partition);
        }

        private static void ConfigurePartition(GameObject partition)
        {
            Rigidbody rigidbody = partition.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = Undo.AddComponent<Rigidbody>(partition);
            }

            Undo.RecordObject(rigidbody, "Configure partition rigidbody");
            rigidbody.mass = 40f;
            rigidbody.useGravity = true;

            BreakablePartitionAuthoring authoring =
                partition.GetComponent<BreakablePartitionAuthoring>();
            if (authoring == null)
            {
                authoring = Undo.AddComponent<BreakablePartitionAuthoring>(partition);
            }

            var serializedAuthoring = new SerializedObject(authoring);
            SetInt(serializedAuthoring, "_fragmentCount", 36);
            SetBool(serializedAuthoring, "_fractureXAxis", true);
            SetBool(serializedAuthoring, "_fractureYAxis", true);
            SetBool(serializedAuthoring, "_fractureZAxis", false);
            SetBool(serializedAuthoring, "_detectFloatingFragments", false);
            SetBool(serializedAuthoring, "_saveFragmentMeshes", true);
            SetString(serializedAuthoring, "_meshOutputFolder", MeshOutputFolder);
            SetString(serializedAuthoring, "_allowedTag", ToolTag);
            SetFloat(serializedAuthoring, "_minimumCollisionForce", 25f);
            SetFloat(serializedAuthoring, "_impactCooldown", 0.08f);
            SetFloat(serializedAuthoring, "_destructionRadius", 1.25f);
            SetFloat(serializedAuthoring, "_directHitRadius", 0.35f);
            SetFloat(serializedAuthoring, "_impulseMultiplier", 0.08f);
            SetBool(serializedAuthoring, "_anchorLeft", true);
            SetBool(serializedAuthoring, "_anchorRight", true);
            SetBool(serializedAuthoring, "_anchorTop", true);
            SetBool(serializedAuthoring, "_anchorBottom", true);

            Material insideMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.virtualrescue.breakable-partition/Runtime/Materials/Inside.mat");
            serializedAuthoring.FindProperty("_insideMaterial").objectReferenceValue =
                insideMaterial;
            serializedAuthoring.ApplyModifiedPropertiesWithoutUndo();

            GameObject generatedRoot = authoring.GeneratedRoot;
            if (generatedRoot == null)
            {
                Transform existingRoot = partition.transform.parent != null
                    ? partition.transform.parent.Find(GeneratedRootName)
                    : null;
                generatedRoot = existingRoot != null ? existingRoot.gameObject : null;
            }

            if (generatedRoot == null)
            {
                GenerateFragments(authoring);
                generatedRoot = authoring.GeneratedRoot;
            }

            if (generatedRoot == null)
            {
                throw new InvalidOperationException(
                    "Failed to generate the lightweight partition fragments.");
            }

            generatedRoot.name = GeneratedRootName;
            if (generatedRoot.GetComponent<FractureDebrisCollisionController>() == null)
            {
                Undo.AddComponent<FractureDebrisCollisionController>(generatedRoot);
            }

            if (generatedRoot.GetComponent<UnfreezeFragmentSupportController>() == null)
            {
                Undo.AddComponent<UnfreezeFragmentSupportController>(generatedRoot);
            }

            EditorUtility.SetDirty(rigidbody);
            EditorUtility.SetDirty(authoring);
            EditorUtility.SetDirty(generatedRoot);
        }

        private static void ConfigureBat(GameObject bat)
        {
            Undo.RecordObject(bat, "Configure destruction bat");
            bat.tag = ToolTag;

            Rigidbody rigidbody = bat.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = Undo.AddComponent<Rigidbody>(bat);
            }

            Undo.RecordObject(rigidbody, "Configure destruction bat rigidbody");
            rigidbody.mass = 1f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            XRGrabInteractable grabInteractable = bat.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = Undo.AddComponent<XRGrabInteractable>(bat);
            }

            Transform grabAttach = bat.transform.Find(BatGrabAttachName);
            if (grabAttach == null)
            {
                var attachObject = new GameObject(BatGrabAttachName);
                Undo.RegisterCreatedObjectUndo(attachObject, "Create bat grab attach");
                Undo.SetTransformParent(
                    attachObject.transform,
                    bat.transform,
                    "Parent bat grab attach");
                grabAttach = attachObject.transform;
            }

            Undo.RecordObject(grabAttach, "Position bat grab attach");
            grabAttach.localPosition = new Vector3(0f, 0f, 0.08f);
            grabAttach.localRotation = Quaternion.Euler(0f, 180f, 0f);
            grabAttach.localScale = Vector3.one;

            var serializedGrab = new SerializedObject(grabInteractable);
            serializedGrab.FindProperty("m_MovementType").enumValueIndex = 0;
            serializedGrab.FindProperty("m_AttachTransform").objectReferenceValue = grabAttach;
            serializedGrab.FindProperty("m_UseDynamicAttach").boolValue = false;
            serializedGrab.FindProperty("m_MatchAttachPosition").boolValue = true;
            serializedGrab.FindProperty("m_MatchAttachRotation").boolValue = true;
            serializedGrab.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(bat);
            EditorUtility.SetDirty(rigidbody);
            EditorUtility.SetDirty(grabInteractable);
            EditorUtility.SetDirty(grabAttach);
        }

        private static void GenerateFragments(BreakablePartitionAuthoring authoring)
        {
            Type editorType = Type.GetType(
                "VirtualRescue.Editor.BreakablePartitionAuthoringEditor, " +
                "BreakablePartitionToolkit.Editor");
            MethodInfo generateMethod = editorType?.GetMethod(
                "Generate",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (generateMethod == null)
            {
                throw new InvalidOperationException(
                    "Breakable partition fragment generator was not found.");
            }

            generateMethod.Invoke(null, new object[] { authoring });
        }

        private static Scene FindLoadedScene(string scenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.path == scenePath)
                {
                    return scene;
                }
            }

            return default;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (Transform child in rootObject.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private static void SetBool(
            SerializedObject target,
            string propertyName,
            bool value)
        {
            target.FindProperty(propertyName).boolValue = value;
        }

        private static void SetFloat(
            SerializedObject target,
            string propertyName,
            float value)
        {
            target.FindProperty(propertyName).floatValue = value;
        }

        private static void SetInt(
            SerializedObject target,
            string propertyName,
            int value)
        {
            target.FindProperty(propertyName).intValue = value;
        }

        private static void SetString(
            SerializedObject target,
            string propertyName,
            string value)
        {
            target.FindProperty(propertyName).stringValue = value;
        }
    }
}
