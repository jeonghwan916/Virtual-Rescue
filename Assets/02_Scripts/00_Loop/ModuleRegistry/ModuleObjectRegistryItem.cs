using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class ModuleObjectRegistryItem : MonoBehaviour
    {
        [SerializeField] private string _objectId;
        [SerializeField] private GameObject _target;

        private ModuleObjectRegistry _registry;

        public string ObjectId => ModuleObjectRegistry.NormalizeId(_objectId);
        public GameObject Target => _target != null ? _target : gameObject;

        private void Awake()
        {
            if (string.IsNullOrEmpty(ObjectId))
            {
                Debug.LogError($"{name}: Module object ID is empty.", this);
                return;
            }

            _registry = ModuleObjectRegistry.Instance;
            if (_registry == null)
            {
                Debug.LogError(
                    $"{name}: ModuleObjectRegistry was not found. " +
                    "Load the Core scene before home module scenes.",
                    this);
                return;
            }

            if (!_registry.Register(this))
            {
                _registry = null;
            }
        }

        private void OnDestroy()
        {
            _registry?.Unregister(this);
            _registry = null;
        }

        internal void SetTargetActive(bool isActive)
        {
            GameObject target = Target;
            if (target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }

        private void OnValidate()
        {
            _objectId = ModuleObjectRegistry.NormalizeId(_objectId);
        }
    }
}
