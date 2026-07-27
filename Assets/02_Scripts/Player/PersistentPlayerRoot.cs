using UnityEngine;

namespace VirtualRescue.Player
{
    public sealed class PersistentPlayerRoot : MonoBehaviour
    {
        [SerializeField] private string[] _sceneNamesIgnoredForSpawn =
        {
            "LoadingScene"
        };

        public static PersistentPlayerRoot Instance { get; private set; }

        private void Awake()
        {
            PlayerSceneSpawn spawn = GetComponent<PlayerSceneSpawn>();

            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                return;
            }

            if (Instance == this)
            {
                return;
            }

            if (spawn != null && spawn.ShouldApplySpawn(_sceneNamesIgnoredForSpawn))
            {
                Instance.ApplySpawn(spawn.SpawnPoint);
            }

            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void ApplySpawn(Transform spawnTransform)
        {
            transform.SetPositionAndRotation(spawnTransform.position, spawnTransform.rotation);
        }
    }
}
