using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal static class SituationLocationCatalogService
    {
        public const string CatalogPath =
            "Assets/02_Scripts/Editor/SituationAuthoring/" +
            "SituationLocationCatalog.asset";

        private static readonly Regex IdPattern = new(
            "^[a-z0-9][a-z0-9._-]*$",
            RegexOptions.Compiled);

        public static SituationLocationCatalog GetOrCreate()
        {
            SituationLocationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<SituationLocationCatalog>(
                    CatalogPath);
            if (IsPersistentCatalog(catalog))
            {
                return catalog;
            }

            SituationAuthoringUtility.EnsureFolder(
                "Assets/02_Scripts/Editor/SituationAuthoring");
            if (AssetDatabase.LoadMainAssetAtPath(CatalogPath) != null ||
                File.Exists(SituationAuthoringUtility.ToAbsolutePath(
                    CatalogPath)))
            {
                string backupPath = AssetDatabase.GenerateUniqueAssetPath(
                    CatalogPath.Replace(".asset", ".broken.asset"));
                string moveError = AssetDatabase.MoveAsset(
                    CatalogPath,
                    backupPath);
                if (!string.IsNullOrEmpty(moveError))
                {
                    throw new InvalidOperationException(
                        "The invalid Location Catalog could not be backed up: " +
                        moveError);
                }
            }

            catalog = ScriptableObject.CreateInstance<SituationLocationCatalog>();
            AddDefaults(catalog);
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        public static bool TryAdd(
            SituationLocationCatalog catalog,
            string id,
            string displayName,
            string sceneFolderName,
            string controllerFolderName,
            out SituationLocationEntry entry,
            out string error)
        {
            entry = null;
            if (!IsPersistentCatalog(catalog))
            {
                error = "Situation Location Catalog is not a valid persistent " +
                        "asset. Close and reopen the Wizard to reload it.";
                return false;
            }

            id = id?.Trim() ?? string.Empty;
            displayName = displayName?.Trim() ?? string.Empty;
            sceneFolderName = sceneFolderName?.Trim() ?? string.Empty;
            controllerFolderName = controllerFolderName?.Trim() ?? string.Empty;

            if (!IdPattern.IsMatch(id))
            {
                error = "Location ID must use lowercase letters, numbers, '.', " +
                        "'_' or '-', and must begin with a letter or number.";
                return false;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                error = "Display Name is required.";
                return false;
            }

            if (!IsValidFolderName(sceneFolderName) ||
                !IsValidFolderName(controllerFolderName))
            {
                error = "Scene and Controller folder names must be single valid " +
                        "folder names without path separators.";
                return false;
            }

            if (catalog.Locations.Any(location => string.Equals(
                    location.Id,
                    id,
                    StringComparison.OrdinalIgnoreCase)))
            {
                error = $"Location ID already exists: {id}";
                return false;
            }

            if (catalog.Locations.Any(location => string.Equals(
                    location.DisplayName,
                    displayName,
                    StringComparison.OrdinalIgnoreCase)))
            {
                error = $"Display Name already exists: {displayName}";
                return false;
            }

            if (catalog.Locations.Any(location => string.Equals(
                    location.SceneFolderName,
                    sceneFolderName,
                    StringComparison.OrdinalIgnoreCase)))
            {
                error = $"Scene folder is already used by another Location: " +
                        sceneFolderName;
                return false;
            }

            entry = new SituationLocationEntry(
                id,
                displayName,
                sceneFolderName,
                controllerFolderName);
            Undo.RecordObject(catalog, "Add Situation Location");
            catalog.Add(entry);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            AssetDatabase.ImportAsset(
                CatalogPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            SituationLocationCatalog savedCatalog =
                AssetDatabase.LoadAssetAtPath<SituationLocationCatalog>(
                    CatalogPath);
            if (FindById(savedCatalog, id) == null)
            {
                error = "Location was changed in memory, but the catalog asset " +
                        "did not persist the new entry.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static SituationLocationEntry FindById(
            SituationLocationCatalog catalog,
            string id)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return catalog.Locations.FirstOrDefault(location => string.Equals(
                location.Id,
                id,
                StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsValidFolderName(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                   !value.Contains('/') &&
                   !value.Contains('\\') &&
                   value != "." &&
                   value != "..";
        }

        private static bool IsPersistentCatalog(
            SituationLocationCatalog catalog)
        {
            return catalog != null &&
                   AssetDatabase.Contains(catalog) &&
                   string.Equals(
                       AssetDatabase.GetAssetPath(catalog),
                       CatalogPath,
                       StringComparison.OrdinalIgnoreCase) &&
                   MonoScript.FromScriptableObject(catalog) != null;
        }

        private static void AddDefaults(SituationLocationCatalog catalog)
        {
            foreach (RoomLocation roomLocation in
                     Enum.GetValues(typeof(RoomLocation)).Cast<RoomLocation>())
            {
                if (roomLocation == RoomLocation.None)
                {
                    continue;
                }

                string name = roomLocation.ToString();
                catalog.Add(new SituationLocationEntry(
                    ToId(name),
                    name,
                    name,
                    name));
            }
        }

        private static string ToId(string value)
        {
            return value.ToLowerInvariant();
        }
    }
}
