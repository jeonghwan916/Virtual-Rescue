using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal static class SituationControllerScriptGenerator
    {
        public static bool TryBegin(
            SituationCreationRequest request,
            out string error)
        {
            if (EditorApplication.isCompiling)
            {
                error = "Wait for the current Unity compilation to finish.";
                return false;
            }

            if (!ValidateRequest(request, out error, true))
            {
                return false;
            }

            if (!EditorUtility.DisplayDialog(
                    "Create Situation",
                    "The Wizard will create a Controller C# script first. " +
                    "Unity will compile and resume scene creation after the " +
                    "Domain Reload. Existing files will not be overwritten.",
                    "Create",
                    "Cancel"))
            {
                error = "Situation creation was cancelled.";
                return false;
            }

            try
            {
                SituationAuthoringUtility.EnsureFolder(
                    request.controllerScriptFolder);
                SituationCreationResumeHandler.Store(request);

                string script = CreateScriptText(request);
                File.WriteAllText(
                    SituationAuthoringUtility.ToAbsolutePath(
                        request.ControllerScriptPath),
                    script,
                    new UTF8Encoding(false));
                AssetDatabase.Refresh();
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                SituationCreationResumeHandler.Cancel();
                error = exception.Message;
                return false;
            }
        }

        public static bool ValidateRequest(
            SituationCreationRequest request,
            out string error,
            bool checkProjectCollisions = true)
        {
            if (request == null)
            {
                error = "Creation request is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.displayName) ||
                string.IsNullOrWhiteSpace(request.situationId) ||
                string.IsNullOrWhiteSpace(request.sceneName))
            {
                error = "Display Name, Situation ID, and Scene Name are required.";
                return false;
            }

            if (!SituationAuthoringUtility.IsValidIdentifier(
                    request.controllerClassName))
            {
                error = "Controller Class Name is not a valid C# identifier.";
                return false;
            }

            if (!SituationAuthoringUtility.IsValidNamespace(
                    request.controllerNamespace))
            {
                error = "Controller Namespace is not valid.";
                return false;
            }

            if (!SituationAuthoringUtility.IsProjectAssetFolder(
                    request.controllerScriptFolder))
            {
                error = "Controller Script Path must be a folder below Assets.";
                return false;
            }

            if (request.weight < 1 || request.minimumDay < 1 ||
                request.minimumDay > 7)
            {
                error = "Weight must be at least 1 and Minimum Day must be 1-7.";
                return false;
            }

            if (request.Level == SituationLevel.Level2)
            {
                if (request.usesTimeLimit && request.timeLimitSeconds <= 0f)
                {
                    error = "A timed Level 2 situation requires a positive time limit.";
                    return false;
                }

                if (request.allowedExits == null ||
                    request.allowedExits.Length == 0)
                {
                    error = "A Level 2 situation requires at least one allowed exit.";
                    return false;
                }

                if (request.allowedExits.Contains((int)ExitType.Elevator))
                {
                    error = "Elevator cannot be allowed for a Level 2 situation.";
                    return false;
                }
            }

            if (request.sceneName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                request.sceneName.Contains('/') || request.sceneName.Contains('\\'))
            {
                error = "Scene Name contains invalid filename characters.";
                return false;
            }

            if (!checkProjectCollisions)
            {
                error = string.Empty;
                return true;
            }

            if (File.Exists(SituationAuthoringUtility.ToAbsolutePath(
                    request.ControllerScriptPath)))
            {
                error = $"Controller script already exists: " +
                        request.ControllerScriptPath;
                return false;
            }

            if (File.Exists(SituationAuthoringUtility.ToAbsolutePath(
                    request.ScenePath)))
            {
                error = $"Situation scene already exists: {request.ScenePath}";
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    request.DefinitionPath) != null)
            {
                error = $"Situation definition already exists: " +
                        request.DefinitionPath;
                return false;
            }

            foreach (Type controllerType in
                     TypeCache.GetTypesDerivedFrom<SituationController>())
            {
                if (string.Equals(
                        controllerType.FullName,
                        request.ControllerFullName,
                        StringComparison.Ordinal))
                {
                    error = $"Controller type already exists: " +
                            request.ControllerFullName;
                    return false;
                }
            }

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:SituationDefinition",
                         new[] { "Assets" }))
            {
                SituationDefinition existing =
                    AssetDatabase.LoadAssetAtPath<SituationDefinition>(
                        AssetDatabase.GUIDToAssetPath(guid));
                if (existing != null && string.Equals(
                        existing.Id,
                        request.situationId.Trim(),
                        StringComparison.Ordinal))
                {
                    error = $"Situation ID already exists: {request.situationId}";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        internal static string CreateScriptText(SituationCreationRequest request)
        {
            return
                "using VirtualRescue.GameFlow;\n\n" +
                $"namespace {request.controllerNamespace}\n" +
                "{\n" +
                $"    public sealed class {request.controllerClassName} : " +
                "SituationController\n" +
                "    {\n" +
                "        protected override void OnActivated()\n" +
                "        {\n" +
                "            base.OnActivated();\n" +
                "        }\n\n" +
                "        protected override void OnResolved()\n" +
                "        {\n" +
                "            base.OnResolved();\n" +
                "        }\n\n" +
                "        protected override void OnFailed()\n" +
                "        {\n" +
                "            base.OnFailed();\n" +
                "        }\n\n" +
                "        protected override void OnReset()\n" +
                "        {\n" +
                "            base.OnReset();\n" +
                "        }\n" +
                "    }\n" +
                "}\n";
        }
    }
}
