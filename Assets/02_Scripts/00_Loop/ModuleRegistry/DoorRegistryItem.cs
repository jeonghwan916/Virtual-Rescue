using UnityEngine;
using VirtualRescue.Missions09;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class DoorRegistryItem : MonoBehaviour
    {
        [SerializeField] private string _doorId;
        [SerializeField] private FireExitDoorController _doorController;

        private DoorRegistry _registry;

        public string DoorId => DoorRegistry.NormalizeId(_doorId);
        public FireExitDoorController DoorController => _doorController;

        private void Reset()
        {
            _doorController = GetComponent<FireExitDoorController>();
        }

        private void Awake()
        {
            if (_doorController == null)
            {
                _doorController = GetComponent<FireExitDoorController>();
            }

            if (string.IsNullOrEmpty(DoorId))
            {
                Debug.LogError($"{name}: Door ID is empty.", this);
                return;
            }

            if (_doorController == null)
            {
                Debug.LogError(
                    $"{name}: FireExitDoorController is not assigned.",
                    this);
                return;
            }

            _registry = DoorRegistry.Instance;
            if (_registry == null)
            {
                Debug.LogError(
                    $"{name}: DoorRegistry was not found. " +
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

        private void OnValidate()
        {
            _doorId = DoorRegistry.NormalizeId(_doorId);

            if (_doorController == null)
            {
                _doorController = GetComponent<FireExitDoorController>();
            }
        }
    }
}
