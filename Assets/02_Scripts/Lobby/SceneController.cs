using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.Effects;
using VirtualRescue.Loading;

namespace VirtualRescue.Lobby
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private string _selectedSceneKey;
        [SerializeField] private int _selectedSceneBuildIndex = -1;
        [SerializeField] private ScreenFader _screenFader;
        [SerializeField] private float _fadeDuration = 1f;
        [SerializeField] private string _loadingSceneName = "LoadingScene";

        private static readonly string[] AdditiveSceneKeys =
        {
            "BedRoom",
            "BedRoom2",
            "Hallway&Stair",
            "Kitchen&LivingRoom",
            "VestibuleRoom",
            "S_Env"
        };

        private Coroutine _loadRoutine;

        public void SetSelectedScene(string sceneKey, int sceneBuildIndex)
        {
            _selectedSceneKey = sceneKey;
            _selectedSceneBuildIndex = sceneBuildIndex;
        }

        public void LoadSelectedScene()
        {
            if (_loadRoutine != null)
            {
                return;
            }

            _loadRoutine = StartCoroutine(LoadSelectedSceneRoutine());
        }

        private IEnumerator LoadSelectedSceneRoutine()
        {
            if (string.IsNullOrWhiteSpace(_selectedSceneKey) && _selectedSceneBuildIndex < 0)
            {
                Debug.LogWarning("Selected scene key is empty and selected scene build index is invalid.");
                _loadRoutine = null;
                yield break;
            }

            if (string.IsNullOrWhiteSpace(_loadingSceneName))
            {
                Debug.LogWarning("Loading scene name is empty.");
                _loadRoutine = null;
                yield break;
            }

            string[] additiveSceneKeys = string.IsNullOrWhiteSpace(_selectedSceneKey) ? null : AdditiveSceneKeys;
            LoadingRequest.Set(_selectedSceneKey, _selectedSceneBuildIndex, additiveSceneKeys);

            yield return FadeOut();

            Debug.Log("Loading scene... " + _loadingSceneName);
            AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Single);
            if (loadingOperation == null)
            {
                Debug.LogWarning($"Failed to load loading scene: {_loadingSceneName}");
                LoadingRequest.Clear();
                _loadRoutine = null;
                yield break;
            }

            yield return loadingOperation;
        }

        private IEnumerator FadeOut()
        {
            if (_screenFader == null)
            {
                yield break;
            }

            yield return _screenFader.FadeOut(_fadeDuration);
        }
    }
}
