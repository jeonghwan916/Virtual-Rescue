using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.Destruction;

namespace VirtualRescue.Editor
{
    [InitializeOnLoad]
    public static class BreakablePartitionSceneSetup
    {
        private const string ScenePath = "Assets/01_Scenes/BreakablePartition.unity";
        private const string BatGrabAttachName = "BatGrabAttach";
        private const string ToolTag = "DestructionTool";
        private const string SessionKey = "VirtualRescue.BreakablePartitionSceneSetup.Completed";
        private const string GripSessionKey = "VirtualRescue.BreakablePartitionBatGrip.Completed";

        static BreakablePartitionSceneSetup()
        {
            EditorApplication.delayCall += TryRunOnce;
            EditorApplication.delayCall += TryConfigureBatGripOnce;
        }

        [MenuItem("Tools/Virtual Rescue/Setup Breakable Partition Scene")]
        public static void SetupScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Exit Play Mode before setting up the breakable partition scene.");
                return;
            }

            Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError($"Open '{ScenePath}' before running the setup.");
                return;
            }

            GameObject partition = FindSceneObject(scene, "Partition");
            GameObject bat = FindSceneObject(scene, "bat");
            if (partition == null || bat == null)
            {
                Debug.LogError("The scene must contain objects named 'Partition' and 'bat'.");
                return;
            }

            EnsureTag(ToolTag);
            ConfigureBat(bat);
            ConfigurePartition(partition);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            SessionState.SetBool(SessionKey, true);

            Debug.Log("BREAKABLE_PARTITION_SCENE_SETUP_OK", partition);
        }

        [MenuItem("Tools/Virtual Rescue/Setup Breakable Partition Bat Grip")]
        public static void SetupBatGrip()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Exit Play Mode before setting up the bat grip.");
                return;
            }

            Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError($"Open '{ScenePath}' before setting up the bat grip.");
                return;
            }

            GameObject bat = FindSceneObject(scene, "bat");
            if (bat == null)
            {
                Debug.LogError("The scene must contain an object named 'bat'.");
                return;
            }

            ConfigureBat(bat);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            SessionState.SetBool(GripSessionKey, true);

            Debug.Log("BREAKABLE_PARTITION_BAT_GRIP_SETUP_OK", bat);
        }

        private static void TryRunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                Application.isPlaying)
            {
                return;
            }

            Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath || FindSceneObject(scene, "PartitionFragments") != null)
            {
                return;
            }

            SetupScene();
        }

        private static void TryConfigureBatGripOnce()
        {
            if (SessionState.GetBool(GripSessionKey, false) ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                Application.isPlaying)
            {
                return;
            }

            Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject bat = FindSceneObject(scene, "bat");
            if (scene.path != ScenePath || bat == null ||
                bat.transform.Find(BatGrabAttachName) != null)
            {
                return;
            }

            SetupBatGrip();
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

            Transform grabAttach = bat.transform.Find(BatGrabAttachName);
            if (grabAttach == null)
            {
                var attachObject = new GameObject(BatGrabAttachName);
                Undo.RegisterCreatedObjectUndo(attachObject, "Create bat grab attach");
                Undo.SetTransformParent(attachObject.transform, bat.transform, "Parent bat grab attach");
                grabAttach = attachObject.transform;
            }

            Undo.RecordObject(grabAttach, "Position bat grab attach");
            grabAttach.localPosition = new Vector3(0f, 0f, -0.42f);
            grabAttach.localRotation = Quaternion.identity;
            grabAttach.localScale = Vector3.one;

            foreach (MonoBehaviour behaviour in bat.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour.GetType().Name != "XRGrabInteractable")
                {
                    continue;
                }

                var serializedGrab = new SerializedObject(behaviour);
                SerializedProperty movementType = serializedGrab.FindProperty("m_MovementType");
                if (movementType != null)
                {
                    movementType.enumValueIndex = 0;
                }

                serializedGrab.FindProperty("m_AttachTransform").objectReferenceValue = grabAttach;
                serializedGrab.FindProperty("m_UseDynamicAttach").boolValue = false;
                serializedGrab.FindProperty("m_MatchAttachPosition").boolValue = true;
                serializedGrab.FindProperty("m_MatchAttachRotation").boolValue = true;
                serializedGrab.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(behaviour);
                break;
            }

            EditorUtility.SetDirty(bat);
            EditorUtility.SetDirty(rigidbody);
        }

        private static void ConfigurePartition(GameObject partition)
        {
            BreakablePartitionAuthoring authoring =
                partition.GetComponent<BreakablePartitionAuthoring>();
            if (authoring == null)
            {
                authoring = Undo.AddComponent<BreakablePartitionAuthoring>(partition);
            }

            Rigidbody rigidbody = partition.GetComponent<Rigidbody>();
            rigidbody.mass = 40f;
            rigidbody.useGravity = true;

            var serializedAuthoring = new SerializedObject(authoring);
            SetInt(serializedAuthoring, "_fragmentCount", 36);
            SetBool(serializedAuthoring, "_fractureXAxis", true);
            SetBool(serializedAuthoring, "_fractureYAxis", true);
            SetBool(serializedAuthoring, "_fractureZAxis", false);
            SetBool(serializedAuthoring, "_detectFloatingFragments", false);
            SetBool(serializedAuthoring, "_saveFragmentMeshes", true);
            SetString(
                serializedAuthoring,
                "_meshOutputFolder",
                "Assets/Generated/BreakablePartitions/BreakablePartition");
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
            serializedAuthoring.FindProperty("_insideMaterial").objectReferenceValue = insideMaterial;
            serializedAuthoring.ApplyModifiedPropertiesWithoutUndo();

            Type editorType = Type.GetType(
                "VirtualRescue.Editor.BreakablePartitionAuthoringEditor, " +
                "BreakablePartitionToolkit.Editor");
            MethodInfo generateMethod = editorType?.GetMethod(
                "Generate",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (generateMethod == null)
            {
                throw new InvalidOperationException("Breakable partition generator was not found.");
            }

            generateMethod.Invoke(null, new object[] { authoring });
            EditorUtility.SetDirty(authoring);
            EditorUtility.SetDirty(rigidbody);
        }

        private static void EnsureTag(string tagName)
        {
            if (Array.IndexOf(InternalEditorUtility.tags, tagName) < 0)
            {
                InternalEditorUtility.AddTag(tagName);
            }
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

        private static void SetBool(SerializedObject target, string propertyName, bool value)
        {
            target.FindProperty(propertyName).boolValue = value;
        }

        private static void SetFloat(SerializedObject target, string propertyName, float value)
        {
            target.FindProperty(propertyName).floatValue = value;
        }

        private static void SetInt(SerializedObject target, string propertyName, int value)
        {
            target.FindProperty(propertyName).intValue = value;
        }

        private static void SetString(SerializedObject target, string propertyName, string value)
        {
            target.FindProperty(propertyName).stringValue = value;
        }
    }
}
