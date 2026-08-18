using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class SituationObjectOverride : MonoBehaviour
    {
        private readonly struct ModuleObjectSnapshot
        {
            public ModuleObjectSnapshot(GameObject target, bool wasActive)
            {
                Target = target;
                WasActive = wasActive;
            }

            public GameObject Target { get; }
            public bool WasActive { get; }
        }

        [SerializeField] private SituationController _situationController;
        [SerializeField] private ModuleObjectId[] _moduleObjectIds;

        private readonly List<ModuleObjectSnapshot> _snapshots = new();
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

            _situationController.Activated += ApplyOverrides;
            _situationController.ResetPerformed += RestoreOverrides;

            if (_situationController.IsActive)
            {
                ApplyOverrides();
            }
        }

        private void OnDisable()
        {
            if (_situationController != null)
            {
                _situationController.Activated -= ApplyOverrides;
                _situationController.ResetPerformed -= RestoreOverrides;
            }

            RestoreOverrides();
        }

        private void OnValidate()
        {
            FindControllerIfMissing();
        }

        private void ApplyOverrides()
        {
            if (_isApplied)
            {
                return;
            }

            ModuleObjectRegistry registry = ModuleObjectRegistry.Instance;
            if (registry == null)
            {
                Debug.LogError("ModuleObjectRegistry was not found.", this);
                return;
            }

            _isApplied = true;

            if (_moduleObjectIds == null)
            {
                return;
            }

            List<GameObject> targets = new();
            HashSet<GameObject> appliedTargets = new();

            foreach (ModuleObjectId objectId in _moduleObjectIds)
            {
                string normalizedId = ModuleObjectRegistry.GetIdValue(objectId);
                if (string.IsNullOrEmpty(normalizedId))
                {
                    continue;
                }

                targets.Clear();
                if (!registry.TryGetTargets(objectId, targets))
                {
                    Debug.LogWarning(
                        $"Module object ID '{normalizedId}' is not registered.",
                        this);
                    continue;
                }

                foreach (GameObject target in targets)
                {
                    if (target == null || !appliedTargets.Add(target))
                    {
                        continue;
                    }

                    _snapshots.Add(
                        new ModuleObjectSnapshot(target, target.activeSelf));

                    if (target.activeSelf)
                    {
                        target.SetActive(false);
                    }
                }
            }
        }

        private void RestoreOverrides()
        {
            if (!_isApplied)
            {
                return;
            }

            for (int index = _snapshots.Count - 1; index >= 0; index--)
            {
                ModuleObjectSnapshot snapshot = _snapshots[index];
                GameObject target = snapshot.Target;

                if (target == null)
                {
                    continue;
                }

                if (target.activeSelf != snapshot.WasActive)
                {
                    target.SetActive(snapshot.WasActive);
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
