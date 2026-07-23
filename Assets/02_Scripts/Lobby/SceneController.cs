using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.Effects;

namespace VirtualRescue.Lobby
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private string _selectedSceneKey;
        [SerializeField] private int _selectedSceneBuildIndex = -1;
        [SerializeField] private ScreenFader _screenFader;
        [SerializeField] private float _fadeDuration = 1f;

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
            if (!string.IsNullOrWhiteSpace(_selectedSceneKey))
            {
                yield return FadeOut();

                Debug.Log("Loading... " + _selectedSceneKey);
                SceneManager.LoadScene(_selectedSceneKey, LoadSceneMode.Single);

                Debug.Log($"[{name}] Loading additive build test scenes.");
                SceneManager.LoadScene("BedRoom", LoadSceneMode.Additive);
                SceneManager.LoadScene("BedRoom2", LoadSceneMode.Additive);
                SceneManager.LoadScene("Hallway&Stair", LoadSceneMode.Additive);
                SceneManager.LoadScene("Kitchen&LivingRoom", LoadSceneMode.Additive);
                SceneManager.LoadScene("VestibuleRoom", LoadSceneMode.Additive);
                SceneManager.LoadScene("S_Env", LoadSceneMode.Additive);
                yield break;
            }

            if (_selectedSceneBuildIndex < 0)
            {
                Debug.LogWarning("Selected scene key is empty and selected scene build index is invalid.");
                _loadRoutine = null;
                yield break;
            }

            yield return FadeOut();

            Debug.Log("Loading... BuildIndex " + _selectedSceneBuildIndex);
            SceneManager.LoadScene(_selectedSceneBuildIndex, LoadSceneMode.Single);
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
