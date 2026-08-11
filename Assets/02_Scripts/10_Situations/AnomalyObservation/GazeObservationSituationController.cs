using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations.AnomalyObservation
{
    public abstract class GazeObservationSituationController : SituationController
    {
        [Header("References")]
        [SerializeField] private Transform _rayOrigin;
        [SerializeField] private AnomalyDetectionZone _detectionZone;
        [SerializeField] private AnomalyTextureTarget _textureTarget;

        [Header("Observation")]
        [Min(0.1f)]
        [SerializeField] private float _requiredObservationTime = 3f;
        [Min(0.1f)]
        [SerializeField] private float _maximumObservationDistance = 5f;
        [SerializeField] private LayerMask _raycastMask = ~0;

        private float _elapsedObservationTime;
        private bool _canObserve;

        public float ElapsedObservationTime => _elapsedObservationTime;
        public float ObservationProgress => _requiredObservationTime > 0f
            ? Mathf.Clamp01(_elapsedObservationTime / _requiredObservationTime)
            : 0f;

        protected override void OnActivated()
        {
            ResetObservationProgress();

            if (!TryPrepareObservation())
            {
                _canObserve = false;
                return;
            }

            _canObserve = _textureTarget.TryApplyAnomalyTexture();
        }

        protected override void OnResolved()
        {
            StopObservation();
        }

        protected override void OnFailed()
        {
            StopObservation();
        }

        protected override void OnReset()
        {
            StopObservation();
            _detectionZone?.ResetZone();

            if (_textureTarget != null)
            {
                _textureTarget.TryApplyAnomalyTexture();
            }
        }

        private void Update()
        {
            if (!_canObserve || !IsActive)
            {
                return;
            }

            if (_detectionZone == null || !_detectionZone.IsPlayerInside)
            {
                ResetObservationProgress();
                return;
            }

            UpdateObservation();
        }

        private void OnValidate()
        {
            _requiredObservationTime = Mathf.Max(0.1f, _requiredObservationTime);
            _maximumObservationDistance = Mathf.Max(0.1f, _maximumObservationDistance);
        }

        private bool TryPrepareObservation()
        {
            if (_rayOrigin == null)
            {
                Camera mainCamera = Camera.main;

                if (mainCamera != null)
                {
                    _rayOrigin = mainCamera.transform;
                }
            }

            if (_rayOrigin == null)
            {
                Debug.LogError("A Main Camera or ray origin is required.", this);
                return false;
            }

            if (_detectionZone == null)
            {
                Debug.LogError("Anomaly detection zone is not assigned.", this);
                return false;
            }

            if (_textureTarget == null)
            {
                Debug.LogError("Anomaly texture target is not assigned.", this);
                return false;
            }

            return true;
        }

        private void UpdateObservation()
        {
            Ray ray = new(_rayOrigin.position, _rayOrigin.forward);
            bool hasHit = Physics.Raycast(
                ray,
                out RaycastHit hit,
                _maximumObservationDistance,
                _raycastMask,
                QueryTriggerInteraction.Ignore);

            if (!hasHit || !_textureTarget.IsTargetCollider(hit.collider))
            {
                ResetObservationProgress();
                return;
            }

            _elapsedObservationTime += Time.deltaTime;

            if (_elapsedObservationTime < _requiredObservationTime)
            {
                return;
            }

            CompleteObservation();
        }

        private void CompleteObservation()
        {
            _elapsedObservationTime = _requiredObservationTime;
            _canObserve = false;

            if (!_textureTarget.TryApplyNormalTexture())
            {
                Debug.LogError(
                    "Observation completed, but the normal state could not be applied.",
                    this);
                return;
            }

            if (!ResolveSituation())
            {
                Debug.LogError("The observation situation could not be resolved.", this);
            }
        }

        private void StopObservation()
        {
            _canObserve = false;
            ResetObservationProgress();
        }

        private void ResetObservationProgress()
        {
            _elapsedObservationTime = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = _rayOrigin;

            if (origin == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                origin.position,
                origin.position + origin.forward * _maximumObservationDistance);
        }
    }
}
