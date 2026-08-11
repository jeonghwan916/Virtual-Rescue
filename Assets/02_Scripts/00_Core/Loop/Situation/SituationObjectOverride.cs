using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class SituationObjectOverride : MonoBehaviour
    {
        [SerializeField] private SituationController _situationController;
        [SerializeField] private ModuleObjectId[] _moduleObjectIds;

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

            if (!SetModuleObjectsActive(false))
            {
                return;
            }

            _isApplied = true;
        }

        private void RestoreOverrides()
        {
            if (!_isApplied)
            {
                return;
            }

            SetModuleObjectsActive(true);
            _isApplied = false;
        }

        private bool SetModuleObjectsActive(bool isActive)
        {
            ModuleObjectRegistry registry = ModuleObjectRegistry.Instance;
            if (registry == null)
            {
                Debug.LogError("ModuleObjectRegistry was not found.", this);
                return false;
            }

            if (_moduleObjectIds == null)
            {
                return true;
            }

            foreach (ModuleObjectId objectId in _moduleObjectIds)
            {
                string normalizedId = ModuleObjectRegistry.GetIdValue(objectId);
                if (string.IsNullOrEmpty(normalizedId))
                {
                    continue;
                }

                if (registry.TrySetActive(objectId, isActive))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"Module object ID '{normalizedId}' is not registered.",
                    this);
            }

            return true;
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
