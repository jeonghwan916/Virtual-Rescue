using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.Missions09;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class SituationDoorLockOverride : MonoBehaviour
    {
        private readonly struct DoorLockSnapshot
        {
            public DoorLockSnapshot(
                FireExitDoorController doorController,
                bool wasLocked)
            {
                DoorController = doorController;
                WasLocked = wasLocked;
            }

            public FireExitDoorController DoorController { get; }
            public bool WasLocked { get; }
        }

        [SerializeField] private SituationController _situationController;
        [SerializeField] private DoorId[] _doorIds;

        private readonly List<DoorLockSnapshot> _snapshots = new();
        private bool _isApplied;

        private void Awake()
        {
            FindControllerIfMissing();
        }

        private void OnEnable()
        {
            if (_situationController == null)
            {
                Debug.LogError(
                    $"{name}: SituationController is not assigned.",
                    this);
                return;
            }

            _situationController.Activated += ApplyLocks;
            _situationController.ResetPerformed += RestoreLocks;

            if (_situationController.IsActive)
            {
                ApplyLocks();
            }
        }

        private void OnDisable()
        {
            if (_situationController != null)
            {
                _situationController.Activated -= ApplyLocks;
                _situationController.ResetPerformed -= RestoreLocks;
            }

            RestoreLocks();
        }

        private void OnValidate()
        {
            FindControllerIfMissing();
        }

        private void ApplyLocks()
        {
            if (_isApplied)
            {
                return;
            }

            DoorRegistry registry = DoorRegistry.Instance;
            if (registry == null)
            {
                Debug.LogError("DoorRegistry was not found.", this);
                return;
            }

            _isApplied = true;

            if (_doorIds == null)
            {
                return;
            }

            HashSet<string> appliedIds = new();

            foreach (DoorId configuredId in _doorIds)
            {
                string doorId = DoorRegistry.GetIdValue(configuredId);
                if (string.IsNullOrEmpty(doorId) || !appliedIds.Add(doorId))
                {
                    continue;
                }

                if (!registry.TryGetDoor(configuredId, out FireExitDoorController door))
                {
                    Debug.LogWarning(
                        $"Door ID '{doorId}' is not registered.",
                        this);
                    continue;
                }

                _snapshots.Add(new DoorLockSnapshot(door, door.IsLocked));
                door.SetLocked(true);
            }
        }

        private void RestoreLocks()
        {
            if (!_isApplied)
            {
                return;
            }

            for (int index = _snapshots.Count - 1; index >= 0; index--)
            {
                DoorLockSnapshot snapshot = _snapshots[index];
                if (snapshot.DoorController != null)
                {
                    snapshot.DoorController.SetLocked(snapshot.WasLocked);
                }
            }

            _snapshots.Clear();
            _isApplied = false;
        }

        private void FindControllerIfMissing()
        {
            if (_situationController == null)
            {
                _situationController = GetComponentInParent<SituationController>(true);
            }
        }
    }
}
