using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

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
            Quaternion rootRotation = spawnTransform.rotation;
            Vector3 rootPosition = spawnTransform.position;

            if (xrOrigin != null)
            {
                rootRotation =
                    spawnTransform.rotation * Quaternion.Inverse(xrOrigin.localRotation);
                rootPosition =
                    spawnTransform.position - rootRotation * xrOrigin.localPosition;
            }

            GravityProvider gravityProvider =
                GetComponentInChildren<GravityProvider>(true);
            CharacterController characterController =
                GetComponentInChildren<CharacterController>(true);
            bool wasControllerEnabled =
                characterController != null && characterController.enabled;

            gravityProvider?.ResetFallForce();

            if (wasControllerEnabled)
            {
                characterController.enabled = false;
            }

            try
            {
                transform.SetPositionAndRotation(rootPosition, rootRotation);
                Physics.SyncTransforms();
            }
            finally
            {
                if (wasControllerEnabled)
                {
                    characterController.enabled = true;
                    Physics.SyncTransforms();
                }

                gravityProvider?.ResetFallForce();
            }
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
