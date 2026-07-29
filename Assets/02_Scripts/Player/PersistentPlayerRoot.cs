using UnityEngine;

namespace VirtualRescue.Player
{
    public sealed class PersistentPlayerRoot : MonoBehaviour
    {
        [SerializeField] private string[] _sceneNamesIgnoredForSpawn =
        {
            "LoadingScene"
        };
        [SerializeField] private string _xrOriginName = "XR Origin (XR Rig)";

        public static PersistentPlayerRoot Instance { get; private set; }

        private void Awake()
        {
            PlayerSceneSpawn spawn = GetComponent<PlayerSceneSpawn>();

            if (Instance == null)
            {
                Instance = this;
                if (spawn != null && spawn.ShouldApplySpawn(_sceneNamesIgnoredForSpawn))
                {
                    ApplySpawn(spawn.SpawnPoint);
                }

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

        public void ApplySpawn(Transform spawnTransform)
        {
            if (spawnTransform == null)
            {
                return;
            }

            Transform xrOrigin = FindChildTransform(transform, _xrOriginName);
            if (xrOrigin == null)
            {
                transform.SetPositionAndRotation(spawnTransform.position, spawnTransform.rotation);
                return;
            }

            Quaternion rootRotation = spawnTransform.rotation * Quaternion.Inverse(xrOrigin.localRotation);
            Vector3 rootPosition = spawnTransform.position - rootRotation * xrOrigin.localPosition;
            transform.SetPositionAndRotation(rootPosition, rootRotation);
        }

        private static Transform FindChildTransform(Transform root, string transformName)
        {
            if (root.name == transformName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindChildTransform(child, transformName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
