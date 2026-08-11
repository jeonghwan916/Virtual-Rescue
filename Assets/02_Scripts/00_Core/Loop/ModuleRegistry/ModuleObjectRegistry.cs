using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class ModuleObjectRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, ModuleObjectRegistryItem> _items = new();

        public static ModuleObjectRegistry Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Only one ModuleObjectRegistry may exist at a time.", this);
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

        public bool TrySetActive(ModuleObjectId objectId, bool isActive)
        {
            string normalizedId = GetIdValue(objectId);
            if (string.IsNullOrEmpty(normalizedId) ||
                !_items.TryGetValue(normalizedId, out ModuleObjectRegistryItem item) ||
                item == null)
            {
                return false;
            }

            item.SetTargetActive(isActive);
            return true;
        }

        internal bool Register(ModuleObjectRegistryItem item)
        {
            if (item == null || item.ObjectId == null || string.IsNullOrEmpty(item.ObjectIdValue))
            {
                return false;
            }

            if (_items.TryGetValue(item.ObjectIdValue, out ModuleObjectRegistryItem existingItem) &&
                existingItem != null &&
                existingItem != item)
            {
                Debug.LogWarning(
                    $"Module object ID '{item.ObjectIdValue}' is already registered by " +
                    $"'{existingItem.name}'. '{item.name}' was not registered.",
                    item);
                return false;
            }

            _items[item.ObjectIdValue] = item;
            return true;
        }

        internal void Unregister(ModuleObjectRegistryItem item)
        {
            if (item == null || item.ObjectId == null || string.IsNullOrEmpty(item.ObjectIdValue))
            {
                return;
            }

            if (_items.TryGetValue(item.ObjectIdValue, out ModuleObjectRegistryItem registeredItem) &&
                registeredItem == item)
            {
                _items.Remove(item.ObjectIdValue);
            }
        }

        internal static string GetIdValue(ModuleObjectId objectId)
        {
            return objectId != null ? objectId.Id : string.Empty;
        }

        internal static string NormalizeId(string objectId)
        {
            return objectId?.Trim() ?? string.Empty;
        }
    }
}
