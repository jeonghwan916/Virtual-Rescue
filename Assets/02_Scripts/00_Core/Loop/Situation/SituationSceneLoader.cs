using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class SituationSceneLoader : MonoBehaviour
    {
        private Scene _loadedScene;

        public event Action<SituationController, SituationDefinition> SituationLoaded;
        public event Action SituationUnloaded;

        public SituationDefinition CurrentDefinition { get; private set; }
        public SituationSceneRoot CurrentRoot { get; private set; }
        public SituationController CurrentController { get; private set; }
        public bool IsBusy { get; private set; }
        public bool HasLoadedSituation => _loadedScene.IsValid() && _loadedScene.isLoaded;
        public string LoadedSceneName => HasLoadedSituation ? _loadedScene.name : string.Empty;
        public string LastError { get; private set; } = string.Empty;

        public async Task<bool> LoadAsync(SituationDefinition definition)
        {
            if (IsBusy)
            {
                return Fail("Situation scene loading or unloading is already in progress.");
            }

            if (HasLoadedSituation || CurrentController != null)
            {
                return Fail("A situation scene is already loaded. Unload it before loading another one.");
            }

            if (!TryValidateDefinition(definition, out string sceneName))
            {
                return false;
            }

            IsBusy = true;
            LastError = string.Empty;
            Scene loadedScene = default;
            SituationController activatedController = null;

            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Additive);

                if (operation == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to start loading situation scene: {sceneName}");
                }

                await AwaitOperationAsync(operation);

                loadedScene = FindLoadedScene(sceneName);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    throw new InvalidOperationException(
                        $"Loaded situation scene could not be found: {sceneName}");
                }

                SituationSceneRoot root = FindSingleSituationRoot(loadedScene);
                if (!root.TryGetController(out SituationController controller))
                {
                    throw new InvalidOperationException(
                        $"Situation scene '{loadedScene.name}' has an invalid SituationSceneRoot.");
                }

                if (!controller.Activate(definition))
                {
                    throw new InvalidOperationException(
                        $"Situation controller could not activate definition '{definition.Id}'.");
                }

                activatedController = controller;
                _loadedScene = loadedScene;
                CurrentDefinition = definition;
                CurrentRoot = root;
                CurrentController = controller;
                SituationLoaded?.Invoke(CurrentController, CurrentDefinition);
                return true;
            }
            catch (Exception exception)
            {
                if (activatedController != null)
                {
                    activatedController.ResetSituation();
                }

                string rollbackError = await TryRollbackAsync(loadedScene);
                string message = $"Failed to load situation scene: {exception.Message}";

                if (!string.IsNullOrEmpty(rollbackError))
                {
                    message += $" Rollback also failed: {rollbackError}";
                }

                ClearCurrentSituation();
                return Fail(message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<bool> UnloadAsync()
        {
            if (IsBusy)
            {
                return Fail("Situation scene loading or unloading is already in progress.");
            }

            if (!HasLoadedSituation)
            {
                ClearCurrentSituation();
                SituationUnloaded?.Invoke();
                LastError = string.Empty;
                return true;
            }

            IsBusy = true;
            LastError = string.Empty;

            try
            {
                CurrentController?.ResetSituation();

                AsyncOperation operation = SceneManager.UnloadSceneAsync(_loadedScene);
                if (operation == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to start unloading situation scene: {_loadedScene.name}");
                }

                await AwaitOperationAsync(operation);
                ClearCurrentSituation();
                SituationUnloaded?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                return Fail($"Failed to unload situation scene: {exception.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool TryValidateDefinition(
            SituationDefinition definition,
            out string sceneName)
        {
            sceneName = string.Empty;

            if (definition == null)
            {
                return Fail("Situation definition is not assigned.");
            }

            sceneName = definition.SceneName?.Trim();
            if (string.IsNullOrEmpty(sceneName))
            {
                return Fail($"Situation '{definition.Id}' has no scene name.");
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                return Fail($"Situation scene cannot be loaded: {sceneName}");
            }

            Scene existingScene = FindLoadedScene(sceneName);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                return Fail(
                    $"Situation scene is already loaded and is not owned by this loader: {sceneName}");
            }

            return true;
        }

        private static SituationSceneRoot FindSingleSituationRoot(Scene scene)
        {
            List<SituationSceneRoot> roots = new();

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.TryGetComponent(out SituationSceneRoot situationRoot))
                {
                    roots.Add(situationRoot);
                }
            }

            if (roots.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Situation scene '{scene.name}' must contain exactly one " +
                    $"SituationSceneRoot at the scene root. Found: {roots.Count}");
            }

            return roots[0];
        }

        private static Scene FindLoadedScene(string sceneNameOrPath)
        {
            Scene scene = SceneManager.GetSceneByPath(sceneNameOrPath);
            return scene.IsValid()
                ? scene
                : SceneManager.GetSceneByName(sceneNameOrPath);
        }

        private static async Task<string> TryRollbackAsync(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return string.Empty;
            }

            try
            {
                AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null)
                {
                    return $"Could not start unloading scene '{scene.name}'.";
                }

                await AwaitOperationAsync(operation);
                return string.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        private static Task AwaitOperationAsync(AsyncOperation operation)
        {
            if (operation.isDone)
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource<bool> completionSource = new();
            operation.completed += _ => completionSource.TrySetResult(true);
            return completionSource.Task;
        }

        private void ClearCurrentSituation()
        {
            _loadedScene = default;
            CurrentDefinition = null;
            CurrentRoot = null;
            CurrentController = null;
        }

        private bool Fail(string message)
        {
            LastError = message;
            Debug.LogError(message, this);
            return false;
        }
    }
}
