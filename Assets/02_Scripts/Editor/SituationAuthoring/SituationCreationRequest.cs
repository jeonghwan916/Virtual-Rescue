using System;
using UnityEngine;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    [Serializable]
    public sealed class SituationCreationRequest
    {
        public string displayName;
        public string situationId;
        public int location;
        public int level;
        public string sceneName;
        public string controllerClassName;
        public string controllerNamespace;
        public string controllerScriptFolder;
        public int weight = 1;
        public int minimumDay = 1;
        public bool registerAsCandidate;
        public bool usesTimeLimit;
        public float timeLimitSeconds = 60f;
        public int[] allowedExits = Array.Empty<int>();
        public string[] initialPrefabPaths = Array.Empty<string>();
        public string[] moduleObjectIdPaths = Array.Empty<string>();
        public string[] lockedDoorIdPaths = Array.Empty<string>();
        public string[] trapDoorIdPaths = Array.Empty<string>();

        public SituationLocation Location => (SituationLocation)location;
        public VirtualRescue.GameFlow.SituationLevel Level =>
            (VirtualRescue.GameFlow.SituationLevel)level;
        public string ControllerFullName => string.IsNullOrWhiteSpace(
            controllerNamespace)
            ? controllerClassName
            : $"{controllerNamespace}.{controllerClassName}";
        public string SceneFolder =>
            SituationLocationPathMap.GetSceneFolder(Location, Level);
        public string ScenePath => $"{SceneFolder}/{sceneName}.unity";
        public string DefinitionPath =>
            $"{SituationLocationPathMap.DefinitionRoot}/" +
            $"SituationDefinition_{SanitizeFileName(displayName)}.asset";
        public string ControllerScriptPath =>
            $"{controllerScriptFolder.TrimEnd('/', '\\')}/" +
            $"{controllerClassName}.cs";

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "NewSituation";
            }

            foreach (char invalidCharacter in
                     System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidCharacter, '_');
            }

            return value.Trim().Replace(' ', '_');
        }
    }
}
