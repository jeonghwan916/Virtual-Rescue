using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using VirtualRescue.GameFlow;
using VirtualRescue.Player;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal static class SituationValidationService
    {
        public static List<SituationValidationResult> Validate(
            SituationDefinition definition)
        {
            List<SituationValidationResult> results = new();
            if (definition == null)
            {
                results.Add(new SituationValidationResult(
                    SituationValidationSeverity.Error,
                    "Select a SituationDefinition to validate."));
                return results;
            }

            ValidateDefinition(definition, results);
            string scenePath = SituationAuthoringUtility.FindScenePath(
                definition.SceneName);
            SituationRegistrationSnapshot registration =
                SituationRegistrationService.GetSnapshot(definition);
            ValidateRegistration(definition, scenePath, registration, results);
            ValidateHomeLayout(registration, results);

            if (!string.IsNullOrEmpty(scenePath))
            {
                ValidateScene(definition, scenePath, results);
            }

            return results;
        }

        private static void ValidateDefinition(
            SituationDefinition definition,
            ICollection<SituationValidationResult> results)
        {
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                results.Add(Error("Situation ID is required.", definition));
            }

            if (!string.Equals(
                    definition.Id,
                    definition.Id?.Trim(),
                    StringComparison.Ordinal))
            {
                results.Add(Error(
                    "Situation ID contains leading or trailing whitespace.",
                    definition));
            }

            int duplicateIdCount = 0;
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:SituationDefinition",
                         new[] { "Assets" }))
            {
                SituationDefinition other =
                    AssetDatabase.LoadAssetAtPath<SituationDefinition>(
                        AssetDatabase.GUIDToAssetPath(guid));
                if (other != null && string.Equals(
                        other.Id,
                        definition.Id,
                        StringComparison.Ordinal))
                {
                    duplicateIdCount++;
                }
            }

            if (duplicateIdCount > 1)
            {
                results.Add(Error(
                    $"Situation ID '{definition.Id}' is used by " +
                    $"{duplicateIdCount} definitions.",
                    definition));
            }

            if (definition.Weight < 1)
            {
                results.Add(Error("Selection weight must be at least 1.", definition));
            }

            if (definition.MinimumDay < 1 || definition.MinimumDay > 7)
            {
                results.Add(Error("Minimum Day must be between 1 and 7.", definition));
            }

            if (string.IsNullOrWhiteSpace(definition.SceneName))
            {
                results.Add(Error("Scene Name is required.", definition));
            }

            if (definition.AllowedExits == null ||
                definition.AllowedExits.Count == 0)
            {
                results.Add(Error(
                    "A situation requires at least one allowed exit.",
                    definition));
            }

            if (definition.Level == SituationLevel.Level1)
            {
                if (!ContainsAllowedExit(definition, ExitType.CellPhone))
                {
                    results.Add(Error(
                        "A Level 1 situation requires CellPhone as an allowed exit.",
                        definition));
                }

                if (ContainsAllowedExit(definition, ExitType.Elevator))
                {
                    results.Add(Error(
                        "Elevator cannot be an allowed Level 1 exit.",
                        definition));
                }

                if (string.IsNullOrWhiteSpace(
                        definition.BeforeResolveCallingDialogueGroupId))
                {
                    results.Add(Warning(
                        "Level 1 before-resolve calling dialogue group is empty.",
                        definition));
                }

                if (string.IsNullOrWhiteSpace(
                        definition.AfterResolveCallingDialogueGroupId))
                {
                    results.Add(Warning(
                        "Level 1 after-resolve calling dialogue group is empty.",
                        definition));
                }
            }

            if (definition.Level == SituationLevel.Level0 &&
                ContainsAllowedExit(definition, ExitType.CellPhone) &&
                string.IsNullOrWhiteSpace(
                    definition.AfterResolveCallingDialogueGroupId))
            {
                results.Add(Warning(
                    "Level 0 CellPhone exit uses the after-resolve calling dialogue group, but it is empty.",
                    definition));
            }

            if (definition.Level == SituationLevel.Level2)
            {
                if (definition.UsesTimeLimit && definition.TimeLimitSeconds <= 0f)
                {
                    results.Add(Error(
                        "A timed Level 2 situation requires a positive time limit.",
                        definition));
                }

                if (ContainsAllowedExit(definition, ExitType.Elevator))
                {
                    results.Add(Error(
                        "Elevator cannot be an allowed Level 2 exit.",
                        definition));
                }

                if (string.IsNullOrWhiteSpace(
                        definition.Level2CallingDialogueGroupId))
                {
                    results.Add(Warning(
                        "Level 2 calling dialogue group is empty.",
                        definition));
                }
            }
        }

        private static bool ContainsAllowedExit(
            SituationDefinition definition,
            ExitType exitType)
        {
            if (definition?.AllowedExits == null)
            {
                return false;
            }

            foreach (ExitType allowedExit in definition.AllowedExits)
            {
                if (allowedExit == exitType)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateRegistration(
            SituationDefinition definition,
            string scenePath,
            SituationRegistrationSnapshot registration,
            ICollection<SituationValidationResult> results)
        {
            if (!registration.LoopBaseAvailable)
            {
                results.Add(Error(
                    "LoopBase is missing a SituationSelector or DaySceneCoordinator.",
                    definition));
            }

            int occurrenceCount = registration.CandidateOccurrenceCount;
            if (occurrenceCount == 0)
            {
                results.Add(new SituationValidationResult(
                    SituationValidationSeverity.Info,
                    "This definition is not registered as a random candidate. " +
                    "This is valid for a test-only situation.",
                    definition,
                    () => SituationRegistrationService.RegisterCandidate(definition),
                    "Register Candidate"));
            }
            else if (occurrenceCount > 1)
            {
                results.Add(new SituationValidationResult(
                    SituationValidationSeverity.Error,
                    $"This definition appears {occurrenceCount} times in Candidates.",
                    definition,
                    () => SituationRegistrationService
                        .RemoveDuplicateCandidateEntries(definition),
                    "Remove Duplicates"));
            }

            if (registration.HasNullCandidate)
            {
                results.Add(Error(
                    "LoopBase Candidates contains a missing definition.",
                    definition));
            }

            if (string.IsNullOrEmpty(scenePath))
            {
                results.Add(Error(
                    $"Scene asset '{definition.SceneName}' was not found.",
                    definition));
                return;
            }

            string actualSceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (!string.Equals(
                    definition.SceneName,
                    actualSceneName,
                    StringComparison.Ordinal))
            {
                results.Add(new SituationValidationResult(
                    SituationValidationSeverity.Error,
                    $"Definition Scene Name does not match '{actualSceneName}'.",
                    definition,
                    () => SituationFixService.SynchronizeSceneName(
                        definition,
                        scenePath),
                    "Use Actual Scene Name"));
            }

            if (!SituationRegistrationService.IsInBuildSettings(scenePath))
            {
                results.Add(new SituationValidationResult(
                    SituationValidationSeverity.Error,
                    "Situation scene is missing or disabled in Build Settings.",
                    definition,
                    () => SituationRegistrationService.AddToBuildSettings(scenePath),
                    "Add to Build Settings"));
            }
        }

        private static void ValidateHomeLayout(
            SituationRegistrationSnapshot registration,
            ICollection<SituationValidationResult> results)
        {
            HomeLayoutDefinition layout = registration.HomeLayout;
            if (layout == null)
            {
                results.Add(new SituationValidationResult(
                    SituationValidationSeverity.Warning,
                    "LoopBase DaySceneCoordinator has no HomeLayoutDefinition."));
                return;
            }

            foreach (string moduleSceneName in layout.ModuleSceneNames)
            {
                if (string.IsNullOrWhiteSpace(moduleSceneName) ||
                    string.IsNullOrEmpty(
                        SituationAuthoringUtility.FindScenePath(moduleSceneName)))
                {
                    results.Add(Error(
                        $"Home Layout module scene was not found: " +
                        $"'{moduleSceneName}'.",
                        layout));
                }
            }
        }

        private static void ValidateScene(
            SituationDefinition definition,
            string scenePath,
            ICollection<SituationValidationResult> results)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                var roots = SituationAuthoringUtility
                    .FindComponentsInScene<SituationSceneRoot>(scene);
                var controllers = SituationAuthoringUtility
                    .FindComponentsInScene<SituationController>(scene);

                if (roots.Count != 1)
                {
                    Action fix = roots.Count == 0 && controllers.Count == 1
                        ? () => SituationFixService.AddMissingRoot(scenePath)
                        : null;
                    results.Add(new SituationValidationResult(
                        SituationValidationSeverity.Error,
                        $"Situation scene requires exactly one root; found " +
                        $"{roots.Count}.",
                        definition,
                        fix,
                        "Add Root"));
                }
                else if (!roots[0].IsValid)
                {
                    Action fix = controllers.Count == 1
                        ? () => SituationFixService.ConnectSingleController(scenePath)
                        : null;
                    results.Add(new SituationValidationResult(
                        SituationValidationSeverity.Error,
                        "SituationSceneRoot has no valid Controller reference.",
                        roots[0],
                        fix,
                        "Connect Controller"));
                }

                AddForbiddenComponentResult<Camera>(scene, "Camera", results);
                AddForbiddenComponentResult<AudioListener>(
                    scene,
                    "Audio Listener",
                    results);
                AddForbiddenComponentResult<EventSystem>(
                    scene,
                    "EventSystem",
                    results);
                AddForbiddenComponentResult<PersistentPlayerRoot>(
                    scene,
                    "XR/Persistent Player",
                    results);

                foreach (Light light in SituationAuthoringUtility
                             .FindComponentsInScene<Light>(scene))
                {
                    if (light.type == LightType.Directional)
                    {
                        results.Add(new SituationValidationResult(
                            SituationValidationSeverity.Warning,
                            "Situation scene contains a Directional Light.",
                            light));
                    }
                }

                foreach (Volume volume in SituationAuthoringUtility
                             .FindComponentsInScene<Volume>(scene))
                {
                    if (volume.isGlobal)
                    {
                        results.Add(new SituationValidationResult(
                            SituationValidationSeverity.Warning,
                            "Situation scene contains a global Volume.",
                            volume));
                    }
                }

                ValidateRegistryReferences(scene, results);

                if (scene.isDirty)
                {
                    results.Add(new SituationValidationResult(
                        SituationValidationSeverity.Info,
                        "Situation scene has unsaved changes."));
                }
            }
            finally
            {
                if (openedForValidation)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateRegistryReferences(
            Scene scene,
            ICollection<SituationValidationResult> results)
        {
            foreach (SituationObjectOverride component in
                     SituationAuthoringUtility.FindComponentsInScene<
                         SituationObjectOverride>(scene))
            {
                ValidateIdArray<ModuleObjectId>(
                    component,
                    "_moduleObjectIds",
                    id => id.IsValid,
                    results);
            }

            foreach (SituationDoorLockOverride component in
                     SituationAuthoringUtility.FindComponentsInScene<
                         SituationDoorLockOverride>(scene))
            {
                ValidateIdArray<DoorId>(
                    component,
                    "_doorIds",
                    id => id.IsValid,
                    results);
            }

            foreach (SituationTrapDoorTrigger component in
                     SituationAuthoringUtility.FindComponentsInScene<
                         SituationTrapDoorTrigger>(scene))
            {
                ValidateIdArray<DoorId>(
                    component,
                    "_doorIds",
                    id => id.IsValid,
                    results);
            }
        }

        private static void ValidateIdArray<T>(
            Component component,
            string propertyName,
            Func<T, bool> isValid,
            ICollection<SituationValidationResult> results)
            where T : UnityEngine.Object
        {
            SerializedObject serializedObject = new(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            HashSet<T> found = new();
            if (property == null || !property.isArray)
            {
                results.Add(Error(
                    $"{component.GetType().Name} ID array is unavailable.",
                    component));
                return;
            }

            for (int index = 0; index < property.arraySize; index++)
            {
                T id = property.GetArrayElementAtIndex(index)
                    .objectReferenceValue as T;
                if (id == null || !isValid(id))
                {
                    results.Add(Error(
                        $"{component.GetType().Name} contains a missing or " +
                        "invalid ID.",
                        component));
                    continue;
                }

                if (!found.Add(id))
                {
                    results.Add(Error(
                        $"{component.GetType().Name} contains duplicate ID " +
                        $"'{id.name}'.",
                        component));
                }
            }
        }

        private static void AddForbiddenComponentResult<T>(
            Scene scene,
            string label,
            ICollection<SituationValidationResult> results)
            where T : Component
        {
            foreach (T component in
                     SituationAuthoringUtility.FindComponentsInScene<T>(scene))
            {
                results.Add(Error(
                    $"Situation scene must not contain {label}.",
                    component));
            }
        }

        private static SituationValidationResult Error(
            string message,
            UnityEngine.Object context)
        {
            return new SituationValidationResult(
                SituationValidationSeverity.Error,
                message,
                context);
        }

        private static SituationValidationResult Warning(
            string message,
            UnityEngine.Object context)
        {
            return new SituationValidationResult(
                SituationValidationSeverity.Warning,
                message,
                context);
        }
    }
}
