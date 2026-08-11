using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class ModuleObjectRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, List<ModuleObjectRegistryItem>> _items = new();

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
                !_items.TryGetValue(normalizedId, out List<ModuleObjectRegistryItem> items))
            {
                return false;
            }

            bool handled = false;

            for (int i = items.Count - 1; i >= 0; i--)
            {
                ModuleObjectRegistryItem item = items[i];
                if (item == null)
                {
                    items.RemoveAt(i);
                    continue;
                }

                item.SetTargetActive(isActive);
                handled = true;
            }

            if (items.Count == 0)
            {
                _items.Remove(normalizedId);
            }

            return handled;
        }

        internal bool Register(ModuleObjectRegistryItem item)
        {
            if (item == null || item.ObjectId == null || string.IsNullOrEmpty(item.ObjectIdValue))
            {
                return false;
            }

            if (!_items.TryGetValue(item.ObjectIdValue, out List<ModuleObjectRegistryItem> items))
            {
                items = new List<ModuleObjectRegistryItem>();
                _items.Add(item.ObjectIdValue, items);
            }

            if (!items.Contains(item))
            {
                items.Add(item);
            }

            return true;
        }

        internal void Unregister(ModuleObjectRegistryItem item)
        {
            if (item == null || item.ObjectId == null || string.IsNullOrEmpty(item.ObjectIdValue))
            {
                return;
            }

            if (!_items.TryGetValue(item.ObjectIdValue, out List<ModuleObjectRegistryItem> items))
            {
                return;
            }

            items.Remove(item);

            if (items.Count == 0)
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
