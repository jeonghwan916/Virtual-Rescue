using UnityEngine;

namespace VirtualRescue.Situations.AnomalyObservation
{
    [DisallowMultipleComponent]
    public sealed class FireExtinguisherGaugeObservationTarget
        : AnomalyTextureTarget
    {
        [Header("Extinguisher")]
        [SerializeField] private FireExtinguisher _fireExtinguisher;

        [Header("Pressure Indicator")]
        [SerializeField] private Transform _indicatorArrow;
        [SerializeField] private Vector3 _normalLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 _rotationAxis = Vector3.right;
        [SerializeField] private float _anomalyAngleDegrees = 60f;

        public override bool TryApplyAnomalyTexture()
        {
            return TryApplyState(false, _anomalyAngleDegrees);
        }

        public override bool TryApplyNormalTexture()
        {
            return TryApplyState(true, 0f);
        }

        private void OnValidate()
        {
            if (_fireExtinguisher == null)
            {
                _fireExtinguisher =
                    GetComponentInChildren<FireExtinguisher>(true);
            }

            if (_indicatorArrow == null)
            {
                _indicatorArrow = FindChild(transform, "indicator_arrow");
            }

            if (_rotationAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                _rotationAxis = Vector3.right;
            }
        }

        private bool TryApplyState(bool isOperational, float angleDegrees)
        {
            if (_fireExtinguisher == null || _indicatorArrow == null)
            {
                Debug.LogError(
                    "The observation extinguisher requires its controller and indicator arrow.",
                    this);
                return false;
            }

            Vector3 normalizedAxis = _rotationAxis.sqrMagnitude > Mathf.Epsilon
                ? _rotationAxis.normalized
                : Vector3.right;
            Quaternion normalRotation =
                Quaternion.Euler(_normalLocalEulerAngles);
            _indicatorArrow.localRotation =
                normalRotation * Quaternion.AngleAxis(angleDegrees, normalizedAxis);
            _fireExtinguisher.SetOperational(isOperational);
            return true;
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            foreach (Transform child in
                     parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
