using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualRescue.Missions09
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class FireExitDoorHandle : MonoBehaviour
    {
        public event Action<FireExitDoorHandle> HoverStarted;

        private enum RotationAxis
        {
            X,
            Y,
            Z
        }

        private const float SmallDistanceThreshold = 0.0001f;

        [Header("References")]
        [SerializeField] private FireExitDoorController _doorController;
        [SerializeField] private XRSimpleInteractable _interactable;
        [SerializeField] private Transform _handlePivot;

        [Header("Handle Rotation")]
        [SerializeField] private RotationAxis _rotationAxis = RotationAxis.Z;
        [SerializeField] private float _minimumAngle = -35f;
        [SerializeField] private float _maximumAngle = 35f;
        [SerializeField] private float _actuationAngle = 20f;
        [SerializeField] private float _neutralTolerance = 2f;
        [SerializeField] private float _rotationSensitivity = 1f;
        [SerializeField] private float _maximumDegreesPerFrame = 8f;
        [SerializeField] private float _minimumInputDegrees = 0.02f;
        [SerializeField] private float _returnSpeed = 140f;

        private Quaternion _neutralLocalRotation;
        private IXRSelectInteractor _activeInteractor;
        private Transform _activeInteractorTransform;
        private Vector3 _previousInteractorPosition;
        private float _currentAngle;
        private bool _hasPreviousInteractorPosition;
        private bool _isActuated;
        private bool _isNeutral = true;

        public bool IsNeutral => _isNeutral;
        public float CurrentAngle => _currentAngle;
        public bool CanOperate =>
            enabled &&
            _doorController != null &&
            _doorController.enabled;

        private void Reset()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _handlePivot = transform;
        }

        private void Awake()
        {
            if (_interactable == null)
            {
                _interactable = GetComponent<XRSimpleInteractable>();
            }

            if (_handlePivot == null)
            {
                _handlePivot = transform;
            }

            _neutralLocalRotation = _handlePivot.localRotation;
            _currentAngle = 0f;
            _isActuated = false;
            _isNeutral = true;
            ApplyRotation();
            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_interactable == null)
            {
                return;
            }

            _interactable.selectEntered.AddListener(HandleSelected);
            _interactable.selectExited.AddListener(HandleDeselected);
            _interactable.hoverEntered.AddListener(HandleHoverEntered);
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(HandleSelected);
                _interactable.selectExited.RemoveListener(HandleDeselected);
                _interactable.hoverEntered.RemoveListener(HandleHoverEntered);
            }

            if (_activeInteractor != null && _doorController != null)
            {
                _doorController.EndHandleInteraction(this);
                _doorController.NotifyHandleReleased(this);
            }

            ClearInteractor();
        }

        private void Update()
        {
            if (_activeInteractorTransform == null)
            {
                ReturnToNeutral();
                return;
            }

            if (_doorController != null && _doorController.IsOpen)
            {
                _hasPreviousInteractorPosition = false;
                return;
            }

            UpdateHandleFromInteractor();
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            if (args.interactorObject == null)
            {
                return;
            }

            _activeInteractor = args.interactorObject;
            _activeInteractorTransform = args.interactorObject.GetAttachTransform(args.interactableObject);

            if (_activeInteractorTransform == null)
            {
                _activeInteractorTransform = args.interactorObject.transform;
            }

            _previousInteractorPosition = _activeInteractorTransform.position;
            _hasPreviousInteractorPosition = true;
            _doorController?.BeginHandleInteraction(this, _activeInteractorTransform);
        }

        private void HandleHoverEntered(HoverEnterEventArgs args)
        {
            HoverStarted?.Invoke(this);
        }

        private void HandleDeselected(SelectExitEventArgs args)
        {
            if (args.interactorObject == null || !ReferenceEquals(_activeInteractor, args.interactorObject))
            {
                return;
            }

            _doorController?.EndHandleInteraction(this);
            _doorController?.NotifyHandleReleased(this);
            ClearInteractor();
        }

        private void ClearInteractor()
        {
            _activeInteractor = null;
            _activeInteractorTransform = null;
            _hasPreviousInteractorPosition = false;
        }

        private void UpdateHandleFromInteractor()
        {
            Vector3 currentPosition = _activeInteractorTransform.position;

            if (!_hasPreviousInteractorPosition)
            {
                _previousInteractorPosition = currentPosition;
                _hasPreviousInteractorPosition = true;
                return;
            }

            float angleDelta = CalculateAngleDelta(_previousInteractorPosition, currentPosition);
            _previousInteractorPosition = currentPosition;

            if (Mathf.Abs(angleDelta) < _minimumInputDegrees)
            {
                return;
            }

            SetAngle(_currentAngle + angleDelta);
            EvaluateActuation();
        }

        private float CalculateAngleDelta(Vector3 previousPosition, Vector3 currentPosition)
        {
            Vector3 worldAxis = _handlePivot.TransformDirection(GetLocalRotationAxis());
            Vector3 previousOffset = Vector3.ProjectOnPlane(previousPosition - _handlePivot.position, worldAxis);
            Vector3 currentOffset = Vector3.ProjectOnPlane(currentPosition - _handlePivot.position, worldAxis);

            if (previousOffset.sqrMagnitude < SmallDistanceThreshold ||
                currentOffset.sqrMagnitude < SmallDistanceThreshold)
            {
                return 0f;
            }

            float signedAngle = Vector3.SignedAngle(previousOffset, currentOffset, worldAxis);
            float angleDelta = signedAngle * _rotationSensitivity;
            return Mathf.Clamp(angleDelta, -_maximumDegreesPerFrame, _maximumDegreesPerFrame);
        }

        private void ReturnToNeutral()
        {
            if (Mathf.Approximately(_currentAngle, 0f))
            {
                UpdateNeutralState();
                return;
            }

            SetAngle(Mathf.MoveTowards(_currentAngle, 0f, _returnSpeed * Time.deltaTime));
            UpdateNeutralState();
        }

        private void SetAngle(float angle)
        {
            _currentAngle = Mathf.Clamp(angle, _minimumAngle, _maximumAngle);
            ApplyRotation();
            UpdateNeutralState();
        }

        private void ApplyRotation()
        {
            Quaternion angleRotation = Quaternion.AngleAxis(_currentAngle, GetLocalRotationAxis());
            _handlePivot.localRotation = _neutralLocalRotation * angleRotation;
        }

        private void EvaluateActuation()
        {
            if (_isActuated || Mathf.Abs(_currentAngle) < _actuationAngle)
            {
                return;
            }

            _isActuated = true;
            _isNeutral = false;
            _doorController?.NotifyHandleActuated(this);
        }

        private void UpdateNeutralState()
        {
            bool isNeutralNow = Mathf.Abs(_currentAngle) <= _neutralTolerance;

            if (_isNeutral == isNeutralNow)
            {
                return;
            }

            _isNeutral = isNeutralNow;

            if (!_isNeutral)
            {
                return;
            }

            _isActuated = false;
            _doorController?.NotifyHandleReturnedToNeutral(this);
        }

        private Vector3 GetLocalRotationAxis()
        {
            switch (_rotationAxis)
            {
                case RotationAxis.X:
                    return Vector3.right;
                case RotationAxis.Y:
                    return Vector3.up;
                default:
                    return Vector3.forward;
            }
        }

        private void ValidateReferences()
        {
            if (_doorController == null)
            {
                Debug.LogError("FireExitDoorHandle requires a FireExitDoorController reference.", this);
            }

            if (_interactable == null)
            {
                Debug.LogError("FireExitDoorHandle requires an XRSimpleInteractable reference.", this);
            }

            if (_handlePivot == null)
            {
                Debug.LogError("FireExitDoorHandle requires a handle pivot reference.", this);
            }
        }

        private void OnValidate()
        {
            if (_maximumAngle < _minimumAngle)
            {
                _maximumAngle = _minimumAngle;
            }

            float maximumAbsoluteAngle = Mathf.Max(Mathf.Abs(_minimumAngle), Mathf.Abs(_maximumAngle));
            _actuationAngle = Mathf.Clamp(_actuationAngle, 0.1f, maximumAbsoluteAngle);
            _neutralTolerance = Mathf.Clamp(_neutralTolerance, 0.01f, _actuationAngle);
            _rotationSensitivity = Mathf.Max(0f, _rotationSensitivity);
            _maximumDegreesPerFrame = Mathf.Max(0.01f, _maximumDegreesPerFrame);
            _minimumInputDegrees = Mathf.Max(0f, _minimumInputDegrees);
            _returnSpeed = Mathf.Max(0f, _returnSpeed);
        }
    }
}
