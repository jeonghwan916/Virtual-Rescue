using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    [Serializable]
    public sealed class SituationLocationEntry
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private string _sceneFolderName = string.Empty;
        [SerializeField] private string _controllerFolderName = string.Empty;

        public string Id => _id?.Trim() ?? string.Empty;
        public string DisplayName => _displayName?.Trim() ?? string.Empty;
        public string SceneFolderName => _sceneFolderName?.Trim() ?? string.Empty;
        public string ControllerFolderName =>
            _controllerFolderName?.Trim() ?? string.Empty;

        public SituationLocationEntry()
        {
        }

        internal SituationLocationEntry(
            string id,
            string displayName,
            string sceneFolderName,
            string controllerFolderName)
        {
            _id = id;
            _displayName = displayName;
            _sceneFolderName = sceneFolderName;
            _controllerFolderName = controllerFolderName;
        }
    }

    public sealed class SituationLocationCatalog : ScriptableObject
    {
        [SerializeField] private List<SituationLocationEntry> _locations = new();

        public IReadOnlyList<SituationLocationEntry> Locations => _locations;

        internal void Add(SituationLocationEntry entry)
        {
            _locations.Add(entry);
        }
    }

    public static class SituationLocationPathMap
    {
        public const string SceneRoot = "Assets/01_Scenes/Situation";
        public const string ControllerRoot = "Assets/02_Scripts/10_Situations";
        public const string DefinitionRoot =
            "Assets/02_Scripts/00_Core/Loop/Situation/" +
            "SituationDefinition_SO";

        public static string GetSceneFolder(
            string locationFolderName,
            VirtualRescue.GameFlow.SituationLevel level)
        {
            return $"{SceneRoot}/{locationFolderName}/{level}";
        }

        public static string GetControllerFolder(
            string controllerFolderName,
            VirtualRescue.GameFlow.SituationLevel level)
        {
            return $"{ControllerRoot}/{controllerFolderName}/{level}";
        }
    }
}
