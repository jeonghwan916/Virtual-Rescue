using UnityEngine;

namespace VirtualRescue.Situations.AnomalyObservation
{
    [DisallowMultipleComponent]
    public sealed class FireExtinguisherGaugeObservationTarget
        : AnomalyTextureTarget
    {
        private const string IndicatorObservationColliderName =
            "GaugeObservationCollider";

        [Header("Extinguisher")]
        [SerializeField] private FireExtinguisher _fireExtinguisher;

        [Header("Pressure Indicator")]
        [SerializeField] private Transform _indicatorArrow;
        [SerializeField] private Vector3 _normalLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 _rotationAxis = Vector3.right;
        [SerializeField] private float _anomalyAngleDegrees = 60f;
        [Min(0.01f)]
        [SerializeField] private float _indicatorObservationRadius = 0.12f;

        private SphereCollider _indicatorObservationCollider;

        public override bool TryApplyAnomalyTexture()
        {
            return TryApplyState(false, _anomalyAngleDegrees);
        }

        public override bool TryApplyNormalTexture()
        {
            // 관측은 이상을 확인하는 행위이므로, 부족한 압력 상태를 바꾸지 않는다.
            return true;
        }

        public override bool TryGetObservationHit(
            Ray ray,
            float maximumDistance,
            LayerMask raycastMask,
            out RaycastHit hit)
        {
            if (TryGetIndicatorObservationHit(ray, maximumDistance, out hit))
            {
                if (Physics.Raycast(
                        ray,
                        out RaycastHit obstructionHit,
                        hit.distance,
                        raycastMask,
                        QueryTriggerInteraction.Ignore) &&
                    !IsTargetCollider(obstructionHit.collider))
                {
                    return false;
                }

                return true;
            }

            return base.TryGetObservationHit(
                ray,
                maximumDistance,
                raycastMask,
                out hit);
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

        private bool TryGetIndicatorObservationHit(
            Ray ray,
            float maximumDistance,
            out RaycastHit hit)
        {
            EnsureIndicatorObservationCollider();

            if (_indicatorObservationCollider == null)
            {
                hit = default;
                return false;
            }

            return _indicatorObservationCollider.Raycast(
                ray,
                out hit,
                maximumDistance);
        }

        private void EnsureIndicatorObservationCollider()
        {
            if (_indicatorObservationCollider != null)
            {
                return;
            }

            if (_indicatorArrow == null)
            {
                _indicatorArrow = FindChild(transform, "indicator_arrow");
            }

            if (_indicatorArrow == null)
            {
                return;
            }

            GameObject colliderObject = new(IndicatorObservationColliderName);
            colliderObject.transform.SetParent(_indicatorArrow, false);

            _indicatorObservationCollider =
                colliderObject.AddComponent<SphereCollider>();
            _indicatorObservationCollider.isTrigger = true;
            _indicatorObservationCollider.radius = _indicatorObservationRadius;
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
