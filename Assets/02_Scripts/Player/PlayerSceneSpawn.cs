using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualRescue.Player
{
    public sealed class PlayerSceneSpawn : MonoBehaviour
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private bool _applyAsSpawnPoint = true;
        [SerializeField] private bool _onlyApplyWhenSceneIsActive = true;

        public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

        public bool ShouldApplySpawn(string[] ignoredSceneNames)
        {
            if (!_applyAsSpawnPoint || !gameObject.activeInHierarchy)
            {
                return false;
            }

            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            if (_onlyApplyWhenSceneIsActive && scene != SceneManager.GetActiveScene())
            {
                return false;
            }

            return !IsIgnoredScene(scene.name, ignoredSceneNames);
        }

        private static bool IsIgnoredScene(string sceneName, string[] ignoredSceneNames)
        {
            if (ignoredSceneNames == null)
            {
                return false;
            }

            foreach (string ignoredSceneName in ignoredSceneNames)
            {
                if (sceneName == ignoredSceneName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
