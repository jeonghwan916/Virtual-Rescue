using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.Effects;
using VirtualRescue.Loading;
using VirtualRescue.Player;

namespace VirtualRescue.Lobby
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private string _selectedSceneKey;
        [SerializeField] private int _selectedSceneBuildIndex = -1;
        [SerializeField] private bool _loadMainGameAdditiveScenes = true;
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private ScreenFader _screenFader;
        [SerializeField] private float _fadeDuration = 1f;
        [SerializeField] private string _loadingSceneName = "LoadingScene";

        private static readonly string[] MainGameAdditiveSceneKeys =
        {
            "BedRoom",
            "BedRoom2",
            "Hallway&Stair",
            "Kitchen&LivingRoom",
            "VestibuleRoom",
            "S_Env",
            "BedRoom_Sub",
            "BedRoom2_Sub",
            "Kitchen&LivingRoom_Sub",
            "VestibuleRoom_Sub"
        };

        private Coroutine _loadRoutine;

        private IEnumerator Start()
        {
            if (PersistentPlayerRoot.Instance != null && _playerSpawnPoint != null)
            {
                PersistentPlayerRoot.Instance.ApplySpawn(_playerSpawnPoint);
            }

            // Lobby 씬의 직렬화 참조는 중복 Player와 함께 파괴될 수 있으므로
            // Awake 이후 살아남은 PersistentPlayer의 Fader를 다시 가져온다.
            _screenFader = FindScreenFader();

            if (_screenFader == null)
            {
                yield break;
            }

            yield return _screenFader.FadeIn(_fadeDuration);
        }

        public void SetSelectedScene(string sceneKey, int sceneBuildIndex)
        {
            SetSelectedScene(sceneKey, sceneBuildIndex, true);
        }

        public void SetSelectedScene(string sceneKey, int sceneBuildIndex, bool loadMainGameAdditiveScenes)
        {
            _selectedSceneKey = sceneKey;
            _selectedSceneBuildIndex = sceneBuildIndex;
            _loadMainGameAdditiveScenes = loadMainGameAdditiveScenes;
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

            string[] additiveSceneKeys = !string.IsNullOrWhiteSpace(_selectedSceneKey) && _loadMainGameAdditiveScenes
                ? MainGameAdditiveSceneKeys
                : null;
            LoadingRequest.Set(
                _selectedSceneKey,
                _selectedSceneBuildIndex,
                additiveSceneKeys);

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
                _screenFader = FindScreenFader();
            }

            if (_screenFader == null)
            {
                yield break;
            }

            yield return _screenFader.FadeOut(_fadeDuration);
        }

        private static ScreenFader FindScreenFader()
        {
            if (PersistentPlayerRoot.Instance != null)
            {
                ScreenFader playerFader = PersistentPlayerRoot.Instance.GetComponentInChildren<ScreenFader>(true);
                if (playerFader != null)
                {
                    return playerFader;
                }
            }

            return FindFirstObjectByType<ScreenFader>(FindObjectsInactive.Include);
        }
    }
}
