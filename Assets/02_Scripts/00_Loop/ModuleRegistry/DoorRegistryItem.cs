using UnityEngine;
using VirtualRescue.Missions09;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class DoorRegistryItem : MonoBehaviour
    {
        [SerializeField] private DoorId _doorId;
        [SerializeField] private FireExitDoorController _doorController;

        private DoorRegistry _registry;

        public DoorId DoorId => _doorId;
        public string DoorIdValue => _doorId != null ? _doorId.Id : string.Empty;
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

            if (_doorId == null)
            {
                Debug.LogError($"{name}: Door ID asset is not assigned.", this);
                return;
            }

            if (string.IsNullOrEmpty(DoorIdValue))
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
            if (_doorController == null)
            {
                _doorController = GetComponent<FireExitDoorController>();
            }
        }
    }
}
