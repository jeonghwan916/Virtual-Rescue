using System;
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

        public bool TrySetActive(string objectId, bool isActive)
        {
            string normalizedId = NormalizeId(objectId);
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
            if (item == null || string.IsNullOrEmpty(item.ObjectId))
            {
                return false;
            }

            if (_items.TryGetValue(item.ObjectId, out ModuleObjectRegistryItem existingItem) &&
                existingItem != null &&
                existingItem != item)
            {
                Debug.LogWarning(
                    $"Module object ID '{item.ObjectId}' is already registered by " +
                    $"'{existingItem.name}'. '{item.name}' was not registered.",
                    item);
                return false;
            }

            _items[item.ObjectId] = item;
            return true;
        }

        internal void Unregister(ModuleObjectRegistryItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.ObjectId))
            {
                return;
            }

            if (_items.TryGetValue(item.ObjectId, out ModuleObjectRegistryItem registeredItem) &&
                registeredItem == item)
            {
                _items.Remove(item.ObjectId);
            }
        }

        internal static string NormalizeId(string objectId)
        {
            return objectId?.Trim() ?? string.Empty;
        }
    }
}
