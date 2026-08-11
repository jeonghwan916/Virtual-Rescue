using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.Missions09;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class DoorRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, DoorRegistryItem> _items = new();

        public static DoorRegistry Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Only one DoorRegistry may exist at a time.", this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            _items.Clear();
            Instance = null;
        }

        public bool TryGetDoor(
            DoorId doorId,
            out FireExitDoorController doorController)
        {
            doorController = null;
            string normalizedId = GetIdValue(doorId);

            if (string.IsNullOrEmpty(normalizedId) ||
                !_items.TryGetValue(normalizedId, out DoorRegistryItem item) ||
                item == null)
            {
                return false;
            }

            doorController = item.DoorController;
            return doorController != null;
        }

        internal bool Register(DoorRegistryItem item)
        {
            if (item == null ||
                item.DoorId == null ||
                string.IsNullOrEmpty(item.DoorIdValue) ||
                item.DoorController == null)
            {
                return false;
            }

            if (_items.TryGetValue(item.DoorIdValue, out DoorRegistryItem existingItem) &&
                existingItem != null &&
                existingItem != item)
            {
                Debug.LogWarning(
                    $"Door ID '{item.DoorIdValue}' is already registered by " +
                    $"'{existingItem.name}'. '{item.name}' was not registered.",
                    item);
                return false;
            }

            _items[item.DoorIdValue] = item;
            return true;
        }

        internal void Unregister(DoorRegistryItem item)
        {
            if (item == null || item.DoorId == null || string.IsNullOrEmpty(item.DoorIdValue))
            {
                return;
            }

            if (_items.TryGetValue(item.DoorIdValue, out DoorRegistryItem registeredItem) &&
                registeredItem == item)
            {
                _items.Remove(item.DoorIdValue);
            }
        }

        internal static string GetIdValue(DoorId doorId)
        {
            return doorId != null ? doorId.Id : string.Empty;
        }

        internal static string NormalizeId(string doorId)
        {
            return doorId?.Trim() ?? string.Empty;
        }
    }
}
