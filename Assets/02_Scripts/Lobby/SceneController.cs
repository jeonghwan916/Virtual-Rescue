using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualRescue.Lobby
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private string _selectedSceneKey;

        public void SetSelectedSceneKey(string sceneKey)
        {
            _selectedSceneKey = sceneKey;
        }

        public void LoadSelectedScene()
        {
            if (string.IsNullOrWhiteSpace(_selectedSceneKey))
            {
                Debug.LogWarning("Selected scene key is empty.");
                return;
            }

            SceneManager.LoadScene(_selectedSceneKey);
        }
    }
}
