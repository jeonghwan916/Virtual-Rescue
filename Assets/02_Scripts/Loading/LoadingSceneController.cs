using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VirtualRescue.Loading;

namespace VirtualRescue.Loading
{
    public class LoadingSceneController : MonoBehaviour
    {
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private float _minimumLoadingSeconds = 0.5f;
        [SerializeField] private string _fallbackLobbySceneName = "BuildTest_Lobby";

        public float LoadingProgress { get; private set; }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadRequestedScenesRoutine());
        }

        private IEnumerator LoadRequestedScenesRoutine()
        {
            if (!LoadingRequest.HasValidMainScene)
            {
                Debug.LogWarning("Loading request is missing a valid main scene.");
                yield return LoadFallbackLobbyRoutine();
                yield break;
            }

            float startedAt = Time.unscaledTime;
            string mainSceneKey = LoadingRequest.MainSceneKey;

            AsyncOperation mainOperation = StartMainSceneLoad();
            if (mainOperation == null)
            {
                Debug.LogWarning("Failed to start main scene load.");
                yield return LoadFallbackLobbyRoutine();
                yield break;
            }

            while (!mainOperation.isDone)
            {
                SetProgress(Mathf.Clamp01(mainOperation.progress * 0.7f));
                yield return null;
            }

            Scene mainScene = GetLoadedMainScene(mainSceneKey);
            if (mainScene.IsValid() && mainScene.isLoaded)
            {
                SceneManager.SetActiveScene(mainScene);
            }
            else
            {
                Debug.LogWarning("Loaded main scene could not be found for SetActiveScene.");
            }

            List<AsyncOperation> additiveOperations = StartAdditiveSceneLoads();

            while (!AreAllOperationsDone(additiveOperations))
            {
                SetProgress(0.7f + GetAverageProgress(additiveOperations) * 0.3f);
                yield return null;
            }

            while (Time.unscaledTime - startedAt < _minimumLoadingSeconds)
            {
                yield return null;
            }

            SetProgress(1f);

            LoadingRequest.Clear();
            Destroy(gameObject);
        }

        private AsyncOperation StartMainSceneLoad()
        {
            if (!string.IsNullOrWhiteSpace(LoadingRequest.MainSceneKey))
            {
                return SceneManager.LoadSceneAsync(LoadingRequest.MainSceneKey, LoadSceneMode.Single);
            }

            return SceneManager.LoadSceneAsync(LoadingRequest.MainSceneBuildIndex, LoadSceneMode.Single);
        }

        private static List<AsyncOperation> StartAdditiveSceneLoads()
        {
            List<AsyncOperation> operations = new List<AsyncOperation>();

            foreach (string additiveSceneKey in LoadingRequest.AdditiveSceneKeys)
            {
                if (string.IsNullOrWhiteSpace(additiveSceneKey))
                {
                    continue;
                }

                AsyncOperation operation = SceneManager.LoadSceneAsync(additiveSceneKey, LoadSceneMode.Additive);
                if (operation != null)
                {
                    operations.Add(operation);
                }
                else
                {
                    Debug.LogWarning($"Failed to start additive scene load: {additiveSceneKey}");
                }
            }

            return operations;
        }

        private Scene GetLoadedMainScene(string mainSceneKey)
        {
            if (!string.IsNullOrWhiteSpace(mainSceneKey))
            {
                return SceneManager.GetSceneByName(mainSceneKey);
            }

            return SceneManager.GetSceneByBuildIndex(LoadingRequest.MainSceneBuildIndex);
        }

        private static bool AreAllOperationsDone(List<AsyncOperation> operations)
        {
            foreach (AsyncOperation operation in operations)
            {
                if (!operation.isDone)
                {
                    return false;
                }
            }

            return true;
        }

        private static float GetAverageProgress(List<AsyncOperation> operations)
        {
            if (operations.Count == 0)
            {
                return 0f;
            }

            float total = 0f;

            foreach (AsyncOperation operation in operations)
            {
                total += Mathf.Clamp01(operation.progress);
            }

            return total / operations.Count;
        }

        private void SetProgress(float progress)
        {
            LoadingProgress = Mathf.Clamp01(progress);

            if (_progressSlider != null)
            {
                _progressSlider.value = LoadingProgress;
            }
        }

        private IEnumerator LoadFallbackLobbyRoutine()
        {
            LoadingRequest.Clear();

            if (string.IsNullOrWhiteSpace(_fallbackLobbySceneName))
            {
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(_fallbackLobbySceneName, LoadSceneMode.Single);
            if (operation != null)
            {
                yield return operation;
            }

            Destroy(gameObject);
        }
    }
}
