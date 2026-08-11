using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class HomeModuleLoader : MonoBehaviour
    {
        private readonly List<string> _loadedModuleSceneNames = new();

        public IReadOnlyList<string> LoadedModuleSceneNames => _loadedModuleSceneNames;
        public bool IsBusy { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        public async Task<bool> LoadAsync(HomeLayoutDefinition layout)
        {
            if (IsBusy)
            {
                return Fail("Home module loading is already in progress.");
            }

            if (_loadedModuleSceneNames.Count > 0)
            {
                return Fail("Home modules are already loaded. Unload them before loading another layout.");
            }

            if (!TryGetValidatedSceneNames(layout, out List<string> sceneNames))
            {
                return false;
            }

            IsBusy = true;
            LastError = string.Empty;

            List<string> startedSceneNames = new(sceneNames.Count);
            List<Task> loadTasks = new(sceneNames.Count);

            try
            {
                foreach (string sceneName in sceneNames)
                {
                    AsyncOperation operation = SceneManager.LoadSceneAsync(
                        sceneName,
                        LoadSceneMode.Additive);

                    if (operation == null)
                    {
                        throw new InvalidOperationException(
                            $"Failed to start loading home module scene: {sceneName}");
                    }

                    startedSceneNames.Add(sceneName);
                    loadTasks.Add(AwaitOperationAsync(operation));
                }

                await Task.WhenAll(loadTasks);
                _loadedModuleSceneNames.AddRange(startedSceneNames);
                return true;
            }
            catch (Exception exception)
            {
                await WaitForStartedLoadsAsync(loadTasks);
                await UnloadScenesAsync(startedSceneNames);
                return Fail($"Failed to load home modules: {exception.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task UnloadAllAsync()
        {
            if (IsBusy)
            {
                Fail("Home module loading or unloading is already in progress.");
                return;
            }

            if (_loadedModuleSceneNames.Count == 0)
            {
                LastError = string.Empty;
                return;
            }

            IsBusy = true;
            LastError = string.Empty;

            try
            {
                await UnloadScenesAsync(_loadedModuleSceneNames);
                _loadedModuleSceneNames.Clear();
            }
            catch (Exception exception)
            {
                Fail($"Failed to unload home modules: {exception.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool TryGetValidatedSceneNames(
            HomeLayoutDefinition layout,
            out List<string> sceneNames)
        {
            sceneNames = new List<string>();

            if (layout == null)
            {
                return Fail("Home layout definition is not assigned.");
            }

            IReadOnlyList<string> configuredSceneNames = layout.ModuleSceneNames;
            if (configuredSceneNames == null || configuredSceneNames.Count == 0)
            {
                return Fail($"Home layout '{layout.name}' has no module scenes.");
            }

            HashSet<string> uniqueSceneNames = new(StringComparer.Ordinal);

            foreach (string configuredSceneName in configuredSceneNames)
            {
                string sceneName = configuredSceneName?.Trim();

                if (string.IsNullOrEmpty(sceneName))
                {
                    return Fail($"Home layout '{layout.name}' contains an empty scene name.");
                }

                if (!uniqueSceneNames.Add(sceneName))
                {
                    return Fail($"Home layout '{layout.name}' contains duplicate scene '{sceneName}'.");
                }

                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    return Fail($"Home module scene cannot be loaded: {sceneName}");
                }

                if (SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    return Fail($"Home module scene is already loaded and is not owned by this loader: {sceneName}");
                }

                sceneNames.Add(sceneName);
            }

            return true;
        }

        private async Task UnloadScenesAsync(IReadOnlyList<string> sceneNames)
        {
            List<Task> unloadTasks = new(sceneNames.Count);

            for (int index = sceneNames.Count - 1; index >= 0; index--)
            {
                string sceneName = sceneNames[index];
                AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

                if (operation != null)
                {
                    unloadTasks.Add(AwaitOperationAsync(operation));
                }
            }

            await Task.WhenAll(unloadTasks);
        }

        private static async Task WaitForStartedLoadsAsync(IReadOnlyList<Task> loadTasks)
        {
            try
            {
                await Task.WhenAll(loadTasks);
            }
            catch
            {
                // The original loading exception is reported by LoadAsync.
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

        private bool Fail(string message)
        {
            LastError = message;
            Debug.LogError(message, this);
            return false;
        }
    }
}
