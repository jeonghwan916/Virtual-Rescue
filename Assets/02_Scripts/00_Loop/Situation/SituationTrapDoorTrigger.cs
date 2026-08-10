using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.Missions09;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class SituationTrapDoorTrigger : MonoBehaviour
    {
        private sealed class TrapDoorBinding
        {
            public TrapDoorBinding(
                FireExitDoorController doorController,
                bool wasTrapped,
                Action openedHandler)
            {
                DoorController = doorController;
                WasTrapped = wasTrapped;
                OpenedHandler = openedHandler;
            }

            public FireExitDoorController DoorController { get; }
            public bool WasTrapped { get; }
            public Action OpenedHandler { get; }
        }

        [SerializeField] private SituationController _situationController;
        [SerializeField] private string[] _doorIds;

        private readonly List<TrapDoorBinding> _bindings = new();
        private bool _isApplied;
        private bool _isTriggered;

        public event Action Triggered;

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

            _situationController.Activated += ApplyTrapDoors;
            _situationController.ResetPerformed += RestoreTrapDoors;

            if (_situationController.IsActive)
            {
                ApplyTrapDoors();
            }
        }

        private void OnDisable()
        {
            if (_situationController != null)
            {
                _situationController.Activated -= ApplyTrapDoors;
                _situationController.ResetPerformed -= RestoreTrapDoors;
            }

            RestoreTrapDoors();
        }

        private void OnValidate()
        {
            FindControllerIfMissing();

            if (_doorIds == null)
            {
                return;
            }

            for (int index = 0; index < _doorIds.Length; index++)
            {
                _doorIds[index] = DoorRegistry.NormalizeId(_doorIds[index]);
            }
        }

        private void ApplyTrapDoors()
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
            _isTriggered = false;

            if (_doorIds == null)
            {
                return;
            }

            HashSet<string> appliedIds = new();

            foreach (string configuredId in _doorIds)
            {
                string doorId = DoorRegistry.NormalizeId(configuredId);
                if (string.IsNullOrEmpty(doorId) || !appliedIds.Add(doorId))
                {
                    continue;
                }

                if (!registry.TryGetDoor(doorId, out FireExitDoorController door))
                {
                    Debug.LogWarning(
                        $"Door ID '{doorId}' is not registered.",
                        this);
                    continue;
                }

                Action openedHandler = () => HandleTrapDoorOpened(door);
                _bindings.Add(new TrapDoorBinding(
                    door,
                    door.IsTrapped,
                    openedHandler));

                door.SetTrapped(true);
                door.Opened += openedHandler;
            }
        }

        private void HandleTrapDoorOpened(FireExitDoorController openedDoor)
        {
            if (_isTriggered || openedDoor == null)
            {
                return;
            }

            _isTriggered = true;
            openedDoor.ShowFire();
            Triggered?.Invoke();
        }

        private void RestoreTrapDoors()
        {
            if (!_isApplied)
            {
                return;
            }

            for (int index = _bindings.Count - 1; index >= 0; index--)
            {
                TrapDoorBinding binding = _bindings[index];
                FireExitDoorController door = binding.DoorController;

                if (door == null)
                {
                    continue;
                }

                door.Opened -= binding.OpenedHandler;
                door.SetTrapped(binding.WasTrapped);
            }

            _bindings.Clear();
            _isApplied = false;
            _isTriggered = false;
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
