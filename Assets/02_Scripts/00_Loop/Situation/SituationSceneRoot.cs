using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class SituationSceneRoot : MonoBehaviour
    {
        [SerializeField] private SituationController _controller;

        public SituationController Controller => _controller;
        public bool IsValid => IsControllerValid();

        private void Awake()
        {
            FindControllerIfMissing();

            if (!IsControllerValid())
            {
                Debug.LogError(
                    $"{name}: SituationSceneRoot requires a SituationController " +
                    "on this object or one of its children in the same scene.",
                    this);
            }
        }

        private void OnValidate()
        {
            FindControllerIfMissing();

            if (_controller != null && !IsControllerValid())
            {
                Debug.LogWarning(
                    $"{name}: Assigned SituationController must belong to this root " +
                    "and the same scene.",
                    this);
            }
        }

        public bool TryGetController(out SituationController controller)
        {
            if (IsControllerValid())
            {
                controller = _controller;
                return true;
            }

            controller = null;
            return false;
        }

        private void FindControllerIfMissing()
        {
            if (_controller == null)
            {
                _controller = GetComponentInChildren<SituationController>(true);
            }
        }

        private bool IsControllerValid()
        {
            if (_controller == null || _controller.gameObject.scene != gameObject.scene)
            {
                return false;
            }

            Transform controllerTransform = _controller.transform;
            return controllerTransform == transform || controllerTransform.IsChildOf(transform);
        }
    }
}
