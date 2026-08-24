using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRSimpleInteractable))]
public sealed class SimpleHingedDoorController : MonoBehaviour
{
    private enum DoorState
    {
        Closed,
        HandleActuated,
        Open,
        WaitingForHandleReset
    }

    private const float SmallDistanceThreshold = 0.0001f;

    [Header("References")]
    [SerializeField] private XRSimpleInteractable _doorInteractable;
    [SerializeField] private SimpleHingedDoorHandle[] _handles =
        Array.Empty<SimpleHingedDoorHandle>();

    [Header("Door Rotation")]
    [SerializeField] private float _minimumAngle = -100f;
    [SerializeField] private float _maximumAngle;
    [SerializeField] private float _rotationSensitivity = 1f;
    [SerializeField] private float _maximumDegreesPerFrame = 6f;
    [SerializeField] private float _minimumInputDegrees = 0.02f;

    [Header("Door State")]
    [SerializeField] private float _openConfirmationAngle = 4f;
    [SerializeField] private float _closeSnapAngle = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _openSFX;
    [SerializeField] private AudioClip _closeSFX;

    private DoorState _state = DoorState.Closed;
    private Quaternion _closedLocalRotation;
    private float _currentAngle;

    private IXRSelectInteractor _panelInteractor;
    private Transform _panelDriver;
    private SimpleHingedDoorHandle _activeHandle;
    private Transform _handleDriver;
    private Transform _currentDriver;
    private Vector3 _previousDriverPosition;
    private bool _hasPreviousDriverPosition;

    public event Action Opened;
    public event Action Closed;

    public bool IsOpen => _state == DoorState.Open;
    public bool IsClosed =>
        _state == DoorState.Closed || _state == DoorState.WaitingForHandleReset;
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
        _state = DoorState.Closed;
        ApplyRotation();
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

        if (driver == null ||
            (_state != DoorState.HandleActuated && _state != DoorState.Open))
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

    public void BeginHandleInteraction(SimpleHingedDoorHandle handle, Transform driver)
    {
        if (!IsRegisteredHandle(handle) || driver == null)
        {
            return;
        }

        _activeHandle = handle;
        _handleDriver = driver;
        ResetDriverTracking();
    }

    public void EndHandleInteraction(SimpleHingedDoorHandle handle)
    {
        if (_activeHandle != handle)
        {
            return;
        }

        _activeHandle = null;
        _handleDriver = null;
        ResetDriverTracking();
    }

    public void NotifyHandleActuated(SimpleHingedDoorHandle handle)
    {
        if (!IsRegisteredHandle(handle) || _state != DoorState.Closed)
        {
            return;
        }

        _state = DoorState.HandleActuated;
        ResetDriverTracking();
    }

    public void NotifyHandleReleased(SimpleHingedDoorHandle handle)
    {
        if (IsRegisteredHandle(handle) && _state == DoorState.HandleActuated)
        {
            EnterClosedState(false);
        }
    }

    public void NotifyHandleReturnedToNeutral(SimpleHingedDoorHandle handle)
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
            _state = DoorState.Closed;
        }
    }

    private void HandleDoorSelected(SelectEnterEventArgs args)
    {
        if (args.interactorObject == null)
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
        if (args.interactorObject == null ||
            !ReferenceEquals(_panelInteractor, args.interactorObject))
        {
            return;
        }

        _panelInteractor = null;
        _panelDriver = null;
        ResetDriverTracking();
    }

    private Transform GetPreferredDriver()
    {
        return _handleDriver != null ? _handleDriver : _panelDriver;
    }

    private void UpdateDriverTracking(Transform driver)
    {
        if (_currentDriver != driver)
        {
            _currentDriver = driver;
            _hasPreviousDriverPosition = false;
        }

        if (driver != null && !_hasPreviousDriverPosition)
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

    private float CalculateAngleDelta(Vector3 previousPosition, Vector3 currentPosition)
    {
        Vector3 worldAxis = transform.TransformDirection(Vector3.up);
        Vector3 previousOffset =
            Vector3.ProjectOnPlane(previousPosition - transform.position, worldAxis);
        Vector3 currentOffset =
            Vector3.ProjectOnPlane(currentPosition - transform.position, worldAxis);

        if (previousOffset.sqrMagnitude < SmallDistanceThreshold ||
            currentOffset.sqrMagnitude < SmallDistanceThreshold)
        {
            return 0f;
        }

        float signedAngle = Vector3.SignedAngle(previousOffset, currentOffset, worldAxis);
        float angleDelta = signedAngle * _rotationSensitivity;
        return Mathf.Clamp(angleDelta, -_maximumDegreesPerFrame, _maximumDegreesPerFrame);
    }

    private void SetAngle(float angle)
    {
        _currentAngle = Mathf.Clamp(angle, _minimumAngle, _maximumAngle);
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        Quaternion angleRotation = Quaternion.AngleAxis(_currentAngle, Vector3.up);
        transform.localRotation = _closedLocalRotation * angleRotation;
    }

    private void EvaluateDoorState()
    {
        float distanceFromClosed = Mathf.Abs(_currentAngle);

        if (_state == DoorState.HandleActuated &&
            distanceFromClosed >= _openConfirmationAngle)
        {
            _state = DoorState.Open;
            Opened?.Invoke();
            PlayOneShot(_openSFX);
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
            ? DoorState.Closed
            : DoorState.WaitingForHandleReset;
        ResetDriverTracking();

        if (invokeClosedEvent)
        {
            Closed?.Invoke();
            PlayOneShot(_closeSFX);
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    private bool AreAllHandlesNeutral()
    {
        if (_handles == null || _handles.Length == 0)
        {
            return false;
        }

        foreach (SimpleHingedDoorHandle handle in _handles)
        {
            if (handle == null || !handle.IsNeutral)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsRegisteredHandle(SimpleHingedDoorHandle handle)
    {
        if (handle == null || _handles == null)
        {
            return false;
        }

        foreach (SimpleHingedDoorHandle registeredHandle in _handles)
        {
            if (registeredHandle == handle)
            {
                return true;
            }
        }

        return false;
    }

    private void ValidateReferences()
    {
        if (_doorInteractable == null)
        {
            Debug.LogError(
                "SimpleHingedDoorController requires an XRSimpleInteractable.", this);
        }

        if (_handles == null || _handles.Length == 0)
        {
            Debug.LogError(
                "SimpleHingedDoorController requires at least one registered handle.", this);
        }
    }

    private void OnValidate()
    {
        if (_maximumAngle < _minimumAngle)
        {
            _maximumAngle = _minimumAngle;
        }

        _rotationSensitivity = Mathf.Max(0f, _rotationSensitivity);
        _maximumDegreesPerFrame = Mathf.Max(0.01f, _maximumDegreesPerFrame);
        _minimumInputDegrees = Mathf.Max(0f, _minimumInputDegrees);
        _openConfirmationAngle = Mathf.Max(0.1f, _openConfirmationAngle);
        _closeSnapAngle =
            Mathf.Clamp(_closeSnapAngle, 0.01f, _openConfirmationAngle);
    }
}
