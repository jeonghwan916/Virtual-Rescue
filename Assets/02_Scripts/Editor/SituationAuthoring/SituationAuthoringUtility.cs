using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal static class SituationAuthoringUtility
    {
        public const string LoopBaseScenePath =
            "Assets/01_Scenes/Situation/LoopBase.unity";

        private static readonly Regex IdentifierPattern =
            new("^[_A-Za-z][_A-Za-z0-9]*$", RegexOptions.Compiled);

        public static bool IsValidIdentifier(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   IdentifierPattern.IsMatch(value);
        }

        public static bool IsValidNamespace(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Split('.').All(IsValidIdentifier);
        }

        public static bool IsProjectAssetFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalized = path.Replace('\\', '/').TrimEnd('/');
            return normalized == "Assets" || normalized.StartsWith("Assets/");
        }

        public static void EnsureFolder(string folderPath)
        {
            string normalized = folderPath.Replace('\\', '/').TrimEnd('/');
            string[] segments = normalized.Split('/');
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

        public static string FindScenePath(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return string.Empty;
            }

            string[] guids = AssetDatabase.FindAssets(
                $"{sceneName} t:Scene",
                new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        sceneName,
                        StringComparison.Ordinal))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        public static T FindComponentInScene<T>(Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        public static List<T> FindComponentsInScene<T>(Scene scene)
            where T : Component
        {
            List<T> results = new();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                results.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return results;
        }

        public static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Path.GetFullPath(Path.Combine(
                projectRoot ?? string.Empty,
                assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        public static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' was not found on " +
                    $"{target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        public static void SetObjectArray<T>(
            UnityEngine.Object target,
            string propertyName,
            IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                throw new InvalidOperationException(
                    $"Serialized array '{propertyName}' was not found on " +
                    $"{target.GetType().Name}.");
            }

            property.arraySize = values?.Count ?? 0;
            for (int index = 0; index < property.arraySize; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
