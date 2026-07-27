using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VirtualRescue.Effects;
using VirtualRescue.Loading;
using VirtualRescue.Player;

namespace VirtualRescue.Loading
{
    public class LoadingSceneController : MonoBehaviour
    {
        [Tooltip("로딩 진행도를 표시할 UI Slider")]
        [SerializeField] private Slider _progressSlider;

        [Tooltip("로딩 UI가 너무 빨리 사라지지 않도록 보장할 최소 표시 시간")]
        [SerializeField] private float _minimumLoadingSeconds = 0.5f;

        [Tooltip("모든 씬 로딩 완료 후 쉐이더 컴파일과 초기 렌더링 안정화를 위해 기다릴 시간")]
        [SerializeField] private float _postLoadWarmupSeconds = 1.75f;

        [Tooltip("Prewarm 완료 후 검은 Overlay가 서서히 사라지는 시간")]
        [SerializeField] private float _overlayFadeOutSeconds = 1f;

        [Tooltip("로딩 요청이 유효하지 않을 때 되돌아갈 로비 씬 이름")]
        [SerializeField] private string _fallbackLobbySceneName = "BuildTest_Lobby";

        public float LoadingProgress { get; private set; }

        private Canvas _loadingCanvas;
        private Image _blockingOverlayImage;

        private void Start()
        {
            PrepareLoadingCanvas();
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

            ClearPersistentPlayerFade();

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

            HideProgressSlider();
            AttachLoadingCanvasToMainCamera();
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

            if (_postLoadWarmupSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(_postLoadWarmupSeconds);
            }

            yield return FadeBlockingOverlayOut();

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

        private void PrepareLoadingCanvas()
        {
            _loadingCanvas = GetComponentInChildren<Canvas>(true);
            if (_loadingCanvas == null)
            {
                return;
            }

            _loadingCanvas.overrideSorting = true;
            _loadingCanvas.sortingOrder = short.MaxValue;
            _blockingOverlayImage = EnsureBlockingOverlay(_loadingCanvas.transform);
        }

        private void AttachLoadingCanvasToMainCamera()
        {
            if (_loadingCanvas == null)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindAnyObjectByType<Camera>();
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("Main camera could not be found for loading overlay.");
                return;
            }

            Transform loadingTransform = _loadingCanvas.transform;
            loadingTransform.SetParent(mainCamera.transform, false);
            loadingTransform.localPosition = new Vector3(0f, 0f, 0.5f);
            loadingTransform.localRotation = Quaternion.identity;
            loadingTransform.localScale = Vector3.one * 0.01f;

            _loadingCanvas.renderMode = RenderMode.WorldSpace;
            _loadingCanvas.worldCamera = mainCamera;
            _loadingCanvas.overrideSorting = true;
            _loadingCanvas.sortingOrder = short.MaxValue;

            RectTransform rectTransform = _loadingCanvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(1000f, 1000f);
            }
        }

        private static Image EnsureBlockingOverlay(Transform canvasTransform)
        {
            Transform existingOverlay = canvasTransform.Find("Loading Blocking Overlay");
            if (existingOverlay != null)
            {
                existingOverlay.SetAsFirstSibling();
                Image existingImage = existingOverlay.GetComponent<Image>();
                if (existingImage != null)
                {
                    existingImage.color = Color.black;
                }

                return existingImage;
            }

            GameObject overlayObject = new GameObject("Loading Blocking Overlay", typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(canvasTransform, false);
            overlayObject.transform.SetAsFirstSibling();

            RectTransform overlayTransform = overlayObject.GetComponent<RectTransform>();
            overlayTransform.anchorMin = Vector2.zero;
            overlayTransform.anchorMax = Vector2.one;
            overlayTransform.offsetMin = Vector2.zero;
            overlayTransform.offsetMax = Vector2.zero;

            Image overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = Color.black;
            return overlayImage;
        }

        private void HideProgressSlider()
        {
            if (_progressSlider == null)
            {
                return;
            }

            _progressSlider.gameObject.SetActive(false);
        }

        private IEnumerator FadeBlockingOverlayOut()
        {
            if (_blockingOverlayImage == null)
            {
                yield break;
            }

            if (_overlayFadeOutSeconds <= 0f)
            {
                SetBlockingOverlayAlpha(0f);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < _overlayFadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _overlayFadeOutSeconds);
                SetBlockingOverlayAlpha(1f - t);
                yield return null;
            }

            SetBlockingOverlayAlpha(0f);
        }

        private void SetBlockingOverlayAlpha(float alpha)
        {
            Color color = _blockingOverlayImage.color;
            color.a = Mathf.Clamp01(alpha);
            _blockingOverlayImage.color = color;
        }

        private void ClearPersistentPlayerFade()
        {
            if (PersistentPlayerRoot.Instance == null)
            {
                return;
            }

            ScreenFader screenFader = PersistentPlayerRoot.Instance.GetComponentInChildren<ScreenFader>(true);
            if (screenFader == null)
            {
                return;
            }

            screenFader.Clear();
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
