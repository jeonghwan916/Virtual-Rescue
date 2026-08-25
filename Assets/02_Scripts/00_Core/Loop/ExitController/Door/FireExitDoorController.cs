using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualRescue.Missions09
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class FireExitDoorController : MonoBehaviour
    {
        private enum DoorState
        {
            Locked,
            HandleActuated,
            Open,
            WaitingForHandleReset
        }

        private enum RotationAxis
        {
            X,
            Y,
            Z
        }

        private const float SmallDistanceThreshold = 0.0001f;

        [Header("References")]
        [SerializeField] private XRSimpleInteractable _doorInteractable;
        [SerializeField] private FireExitDoorHandle[] _handles = Array.Empty<FireExitDoorHandle>();

        [Header("Door Rotation")]
        [SerializeField] private RotationAxis _rotationAxis = RotationAxis.Y;
        [SerializeField] private float _minimumAngle;
        [SerializeField] private float _maximumAngle = 105f;
        [SerializeField] private float _pushSensitivity = 1f;
        [SerializeField] private float _maximumDegreesPerFrame = 6f;
        [SerializeField] private float _minimumInputDegrees = 0.02f;

        [Header("Latch")]
        [SerializeField] private float _openConfirmationAngle = 4f;
        [SerializeField] private float _closeSnapAngle = 2f;

        [Header("Lock")]
        [SerializeField] private bool _isLocked;
        
        [Header("Trap")]
        [SerializeField] private bool _isTrapped;
        [SerializeField] private GameObject _hazeEffect;
        [SerializeField] private GameObject _fireEffect;
        [SerializeField] private GameObject _smokeEffect;
        [SerializeField] private GameObject _blastEffect;
        
        [Header("Audio Source")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _openSFX;
        [SerializeField] private AudioClip _closeSFX;
        [SerializeField] private AudioClip _fireSFX;
        

        private DoorState _state = DoorState.Locked;
        private Quaternion _closedLocalRotation;
        private float _currentAngle;

        private IXRSelectInteractor _panelInteractor;
        private Transform _panelDriver;
        private FireExitDoorHandle _activeHandle;
        private Transform _handleDriver;
        private Transform _currentDriver;
        private Vector3 _previousDriverPosition;
        private bool _hasPreviousDriverPosition;

        public event Action Opened;
        public event Action Closed;

        public bool IsOpen => _state == DoorState.Open;
        public bool IsClosed => _state == DoorState.Locked || _state == DoorState.WaitingForHandleReset;
        public bool IsLocked => _isLocked;
        public bool IsTrapped => _isTrapped;
        public float CurrentAngle => _currentAngle;
        
        

        private void Reset()
        {
            _doorInteractable = GetComponent<XRSimpleInteractable>();
        }

        private void Awake()
        {
            if (_doorInteractable == null)
            {
                _doorInteractable = GetComponent<XRSimpleInteractable>();
            }

            _closedLocalRotation = transform.localRotation;
            _currentAngle = 0f;
            _state = DoorState.Locked;
            ApplyRotation();
            ApplyInitialTrapState();
            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_doorInteractable == null)
            {
                return;
            }

            _doorInteractable.selectEntered.AddListener(HandleDoorSelected);
            _doorInteractable.selectExited.AddListener(HandleDoorDeselected);
        }

        private void OnDisable()
        {
            if (_doorInteractable != null)
            {
                _doorInteractable.selectEntered.RemoveListener(HandleDoorSelected);
                _doorInteractable.selectExited.RemoveListener(HandleDoorDeselected);
            }

            _panelInteractor = null;
            _panelDriver = null;
            _activeHandle = null;
            _handleDriver = null;
            ResetDriverTracking();
        }

        private void Update()
        {
            Transform driver = GetPreferredDriver();
            UpdateDriverTracking(driver);

            if (driver == null)
            {
                return;
            }

            if (_state != DoorState.HandleActuated && _state != DoorState.Open)
            {
                return;
            }

            Vector3 currentPosition = driver.position;
            float angleDelta = CalculateAngleDelta(_previousDriverPosition, currentPosition);
            _previousDriverPosition = currentPosition;

            if (Mathf.Abs(angleDelta) < _minimumInputDegrees)
            {
                return;
            }

            SetAngle(_currentAngle + angleDelta);
            EvaluateDoorState();
        }

        public void BeginHandleInteraction(FireExitDoorHandle handle, Transform driver)
        {
            if (_isLocked || !IsRegisteredHandle(handle) || driver == null)
            {
                return;
            }

            _activeHandle = handle;
            _handleDriver = driver;
            ResetDriverTracking();
        }

        public void EndHandleInteraction(FireExitDoorHandle handle)
        {
            if (_activeHandle != handle)
            {
                return;
            }

            _activeHandle = null;
            _handleDriver = null;
            ResetDriverTracking();
        }

        public void NotifyHandleActuated(FireExitDoorHandle handle)
        {
            if (_isLocked || !IsRegisteredHandle(handle))
            {
                return;
            }

            if (_state == DoorState.Locked)
            {
                _state = DoorState.HandleActuated;
                ResetDriverTracking();
            }
        }

        public void NotifyHandleReleased(FireExitDoorHandle handle)
        {
            if (!IsRegisteredHandle(handle))
            {
                return;
            }

            if (_state == DoorState.HandleActuated)
            {
                EnterClosedState(false);
            }
        }

        public void NotifyHandleReturnedToNeutral(FireExitDoorHandle handle)
        {
            if (!IsRegisteredHandle(handle))
            {
                return;
            }

            if (_state == DoorState.HandleActuated)
            {
                EnterClosedState(false);
                return;
            }

            if (_state == DoorState.WaitingForHandleReset && AreAllHandlesNeutral())
            {
                _state = DoorState.Locked;
            }
        }

        private void HandleDoorSelected(SelectEnterEventArgs args)
        {
            if (_isLocked || args.interactorObject == null)
            {
                return;
            }

            _panelInteractor = args.interactorObject;
            _panelDriver = args.interactorObject.GetAttachTransform(args.interactableObject);

            if (_panelDriver == null)
            {
                _panelDriver = args.interactorObject.transform;
            }

            ResetDriverTracking();
        }

        private void HandleDoorDeselected(SelectExitEventArgs args)
        {
            if (args.interactorObject == null || !ReferenceEquals(_panelInteractor, args.interactorObject))
            {
                return;
            }

            _panelInteractor = null;
            _panelDriver = null;
            ResetDriverTracking();
        }

        public void SetLocked(bool isLocked)
        {
            if (_isLocked == isLocked)
            {
                return;
            }

            _isLocked = isLocked;

            if (!_isLocked)
            {
                return;
            }

            _panelInteractor = null;
            _panelDriver = null;
            _activeHandle = null;
            _handleDriver = null;
            EnterClosedState(false);
        }

        public void SetTrapped(bool isTrapped)
        {
            _isTrapped = isTrapped;

            if (_hazeEffect != null) _hazeEffect.SetActive(_isTrapped);
            
            if (_smokeEffect != null) _smokeEffect.SetActive(_isTrapped);

            if (_fireEffect != null) _fireEffect.SetActive(false);
            
            if (_blastEffect != null)  _blastEffect.SetActive(false);
        }

        private void UpdateDriverTracking(Transform driver)
        {
            if (_currentDriver != driver)
            {
                _currentDriver = driver;
                _hasPreviousDriverPosition = false;
            }

            if (driver == null)
            {
                return;
            }

            if (!_hasPreviousDriverPosition)
            {
                _previousDriverPosition = driver.position;
                _hasPreviousDriverPosition = true;
            }
        }

        private void ResetDriverTracking()
        {
            _currentDriver = null;
            _hasPreviousDriverPosition = false;
        }

        private Transform GetPreferredDriver()
        {
            if (_handleDriver != null)
            {
                return _handleDriver;
            }

            return _panelDriver;
        }

        private float CalculateAngleDelta(Vector3 previousPosition, Vector3 currentPosition)
        {
            Vector3 worldAxis = transform.TransformDirection(GetLocalRotationAxis());
            Vector3 previousOffset = Vector3.ProjectOnPlane(previousPosition - transform.position, worldAxis);
            Vector3 currentOffset = Vector3.ProjectOnPlane(currentPosition - transform.position, worldAxis);

            if (previousOffset.sqrMagnitude < SmallDistanceThreshold ||
                currentOffset.sqrMagnitude < SmallDistanceThreshold)
            {
                return 0f;
            }

            float signedAngle = Vector3.SignedAngle(previousOffset, currentOffset, worldAxis);
            float angleDelta = signedAngle * _pushSensitivity;
            return Mathf.Clamp(angleDelta, -_maximumDegreesPerFrame, _maximumDegreesPerFrame);
        }

        private void SetAngle(float angle)
        {
            _currentAngle = Mathf.Clamp(angle, _minimumAngle, _maximumAngle);
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            Quaternion angleRotation = Quaternion.AngleAxis(_currentAngle, GetLocalRotationAxis());
            transform.localRotation = _closedLocalRotation * angleRotation;
        }

        private void EvaluateDoorState()
        {
            float distanceFromClosed = Mathf.Abs(_currentAngle);

            if (_state == DoorState.HandleActuated && distanceFromClosed >= _openConfirmationAngle)
            {
                _state = DoorState.Open;
                Opened?.Invoke();
                
                if (_audioSource != null || _openSFX != null)
                {
                    _audioSource.PlayOneShot(_openSFX);
                }
                return;
            }

            if (_state == DoorState.Open && distanceFromClosed <= _closeSnapAngle)
            {
                EnterClosedState(true);
            }
        }

        private void EnterClosedState(bool invokeClosedEvent)
        {
            SetAngle(0f);
            _state = AreAllHandlesNeutral()
                ? DoorState.Locked
                : DoorState.WaitingForHandleReset;
            ResetDriverTracking();

            if (invokeClosedEvent)
            {
                Closed?.Invoke();
                
                if (_audioSource != null || _closeSFX != null)
                {
                    _audioSource.PlayOneShot(_closeSFX);
                }
            }
        }

        private bool AreAllHandlesNeutral()
        {
            if (_handles == null || _handles.Length == 0)
            {
                return false;
            }

            foreach (FireExitDoorHandle handle in _handles)
            {
                if (handle == null || !handle.IsNeutral)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsRegisteredHandle(FireExitDoorHandle handle)
        {
            if (handle == null || _handles == null)
            {
                return false;
            }

            foreach (FireExitDoorHandle registeredHandle in _handles)
            {
                if (registeredHandle == handle)
                {
                    return true;
                }
            }

            return false;
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
            if (_doorInteractable == null)
            {
                Debug.LogError("FireExitDoorController requires an XRSimpleInteractable reference.", this);
            }

            if (_handles == null || _handles.Length == 0)
            {
                Debug.LogError("FireExitDoorController requires at least one registered handle.", this);
            }
        }

        private void OnValidate()
        {
            if (_maximumAngle < _minimumAngle)
            {
                _maximumAngle = _minimumAngle;
            }

            _pushSensitivity = Mathf.Max(0f, _pushSensitivity);
            _maximumDegreesPerFrame = Mathf.Max(0.01f, _maximumDegreesPerFrame);
            _minimumInputDegrees = Mathf.Max(0f, _minimumInputDegrees);
            _openConfirmationAngle = Mathf.Max(0.1f, _openConfirmationAngle);
            _closeSnapAngle = Mathf.Clamp(_closeSnapAngle, 0.01f, _openConfirmationAngle);
        }

        public void ShowHaze()
        {
            if (_hazeEffect != null)
            {
                _hazeEffect.SetActive(true);
            }
        }

        public void ShowFire()
        {
            if (_fireEffect != null) _fireEffect.SetActive(true);

            if (_blastEffect != null) _blastEffect.SetActive(true);
            
            if (_audioSource != null && _fireSFX != null) _audioSource.PlayOneShot(_fireSFX);
        }

        private void ApplyInitialTrapState()
        {
            if (_hazeEffect != null)
            {
                _hazeEffect.SetActive(_isTrapped);
            }

            if (_fireEffect != null)
            {
                _fireEffect.SetActive(false);
            }
        }
    }
}
