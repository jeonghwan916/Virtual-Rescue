using UnityEngine;

namespace VirtualRescue.Lobby
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BedGameStartTrigger : MonoBehaviour
    {
        [SerializeField]
        private SceneController _sceneController;

        private bool _isStarting;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isStarting || !other.CompareTag("Player"))
            {
                return;
            }

            if (_sceneController == null)
            {
                Debug.LogWarning("SceneController가 연결되지 않았습니다.", this);
                return;
            }

            _isStarting = true;

            _sceneController.SetSelectedScene(
                "LoopBase",
                -1,
                false);

            _sceneController.LoadSelectedScene();
        }
    }
}
