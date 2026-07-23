using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualRescue.Lobby
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private string _selectedSceneKey;
        [SerializeField] private int _selectedSceneBuildIndex = -1;

        public void SetSelectedScene(string sceneKey, int sceneBuildIndex)
        {
            _selectedSceneKey = sceneKey;
            _selectedSceneBuildIndex = sceneBuildIndex;
        }

        public void LoadSelectedScene()
        {
            if (!string.IsNullOrWhiteSpace(_selectedSceneKey))
            {
                Debug.Log("Loading... " + _selectedSceneKey);
                SceneManager.LoadScene(_selectedSceneKey);
                return;
            }

            if (_selectedSceneBuildIndex < 0)
            {
                Debug.LogWarning("Selected scene key is empty and selected scene build index is invalid.");
                return;
            }

            Debug.Log("Loading... BuildIndex " + _selectedSceneBuildIndex);
            SceneManager.LoadScene(_selectedSceneBuildIndex);
        }
    }
}
