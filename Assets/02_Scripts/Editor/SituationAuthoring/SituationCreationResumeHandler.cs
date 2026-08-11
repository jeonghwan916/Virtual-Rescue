using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    [InitializeOnLoad]
    internal static class SituationCreationResumeHandler
    {
        private const string PendingKey =
            "VirtualRescue.SituationAuthoring.PendingCreation";
        private const string StatusKey =
            "VirtualRescue.SituationAuthoring.PendingStatus";

        static SituationCreationResumeHandler()
        {
            EditorApplication.delayCall += TryResume;
        }

        public static bool HasPending =>
            !string.IsNullOrEmpty(SessionState.GetString(PendingKey, string.Empty));

        public static string Status => SessionState.GetString(
            StatusKey,
            string.Empty);

        public static void Store(SituationCreationRequest request)
        {
            SessionState.SetString(PendingKey, JsonUtility.ToJson(request));
            SessionState.SetString(
                StatusKey,
                "Controller script created. Waiting for Unity compilation.");
        }

        public static void Cancel()
        {
            SessionState.EraseString(PendingKey);
            SessionState.EraseString(StatusKey);
        }

        [DidReloadScripts]
        private static void HandleScriptsReloaded()
        {
            EditorApplication.delayCall += TryResume;
        }

        public static void TryResume()
        {
            if (!HasPending || EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            SituationCreationRequest request = JsonUtility.FromJson<
                SituationCreationRequest>(SessionState.GetString(
                PendingKey,
                string.Empty));
            if (request == null)
            {
                SessionState.SetString(StatusKey, "Pending request is invalid.");
                return;
            }

            Type controllerType = null;
            foreach (Type type in TypeCache.GetTypesDerivedFrom<SituationController>())
            {
                if (string.Equals(
                        type.FullName,
                        request.ControllerFullName,
                        StringComparison.Ordinal))
                {
                    controllerType = type;
                    break;
                }
            }

            if (controllerType == null)
            {
                SessionState.SetString(
                    StatusKey,
                    "Controller type was not found. Fix compilation errors, then " +
                    "press Resume in Situation Authoring.");
                return;
            }

            try
            {
                SessionState.SetString(
                    StatusKey,
                    "Controller compiled. Creating situation assets.");
                SituationDefinition definition =
                    SituationCreationService.Create(request, controllerType);
                Cancel();
                Selection.activeObject = definition;
                EditorGUIUtility.PingObject(definition);
                SituationAuthoringWindow.Open();
                Debug.Log(
                    $"Situation '{request.situationId}' was created successfully.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(StatusKey, exception.Message);
                Debug.LogException(exception);
            }
        }
    }
}
