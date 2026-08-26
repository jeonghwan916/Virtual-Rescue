using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualRescue.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private bool _onlyApplyWhenSceneIsActive = true;

        private void Start()
        {
            if (_onlyApplyWhenSceneIsActive &&
                gameObject.scene != SceneManager.GetActiveScene())
            {
                return;
            }

            PersistentPlayerRoot playerRoot = PersistentPlayerRoot.Instance;
            if (playerRoot == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerSpawnPoint)} could not find a persistent player.",
                    this);
                return;
            }

            playerRoot.ApplySpawn(transform);
        }
    }
}
