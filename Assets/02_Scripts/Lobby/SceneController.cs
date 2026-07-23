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
                SceneManager.LoadScene(_selectedSceneKey, LoadSceneMode.Single);
                
                // 2026.07.23 / HyungJun / 빌드테스트용 코드
                Debug.Log($"[{name}] 빌드테스트용 전체 씬 로드");
                SceneManager.LoadScene("BedRoom", LoadSceneMode.Additive);
                SceneManager.LoadScene("BedRoom2", LoadSceneMode.Additive);
                SceneManager.LoadScene("Hallway&Stair", LoadSceneMode.Additive);
                SceneManager.LoadScene("Kitchen&LivingRoom", LoadSceneMode.Additive);
                SceneManager.LoadScene("VestibuleRoom", LoadSceneMode.Additive);
                SceneManager.LoadScene("S_Env", LoadSceneMode.Additive);
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
