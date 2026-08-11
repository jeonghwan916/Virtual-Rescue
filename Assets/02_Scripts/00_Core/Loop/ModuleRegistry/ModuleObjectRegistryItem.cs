using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class ModuleObjectRegistryItem : MonoBehaviour
    {
        [SerializeField] private ModuleObjectId _objectId;
        [SerializeField] private GameObject _target;

        private ModuleObjectRegistry _registry;

        public ModuleObjectId ObjectId => _objectId;
        public string ObjectIdValue => _objectId != null ? _objectId.Id : string.Empty;
        public GameObject Target => _target != null ? _target : gameObject;

        private void Awake()
        {
            if (_objectId == null)
            {
                Debug.LogError($"{name}: Module object ID asset is not assigned.", this);
                return;
            }

            if (string.IsNullOrEmpty(ObjectIdValue))
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
    }
}
