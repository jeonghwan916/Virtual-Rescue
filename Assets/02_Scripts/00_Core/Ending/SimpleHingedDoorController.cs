using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRSimpleInteractable))]
public sealed class SimpleHingedDoorController : MonoBehaviour
{
    private const float SmallDistanceThreshold = 0.0001f;

    [Header("References")]
    [SerializeField] private XRSimpleInteractable _interactable;

    [Header("Rotation")]
    [SerializeField] private float _minimumAngle = -100f;
    [SerializeField] private float _maximumAngle;
    [SerializeField] private float _rotationSensitivity = 1f;
    [SerializeField] private float _maximumDegreesPerFrame = 6f;
    [SerializeField] private float _minimumInputDegrees = 0.02f;
    [SerializeField] private float _closeSnapAngle = 2f;

    private Quaternion _closedLocalRotation;
    private IXRSelectInteractor _activeInteractor;
    private Transform _activeInteractorTransform;
    private Vector3 _previousInteractorPosition;
    private float _currentAngle;
    private bool _hasPreviousInteractorPosition;

    public float CurrentAngle => _currentAngle;

    private void Reset()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
    }

    private void Awake()
    {
        if (_interactable == null)
        {
            _interactable = GetComponent<XRSimpleInteractable>();
        }

        if (_interactable == null)
        {
            Debug.LogError("SimpleHingedDoorController requires an XRSimpleInteractable.", this);
            enabled = false;
            return;
        }

        _closedLocalRotation = transform.localRotation;
        _currentAngle = 0f;
    }

    private void OnEnable()
    {
        if (_interactable == null)
        {
            return;
        }

        _interactable.selectEntered.AddListener(HandleSelected);
        _interactable.selectExited.AddListener(HandleDeselected);
    }

    private void OnDisable()
    {
        if (_interactable != null)
        {
            _interactable.selectEntered.RemoveListener(HandleSelected);
            _interactable.selectExited.RemoveListener(HandleDeselected);
        }

        ClearInteractor();
    }

    private void Update()
    {
        if (_activeInteractorTransform == null)
        {
            return;
        }

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
    }

    private void HandleSelected(SelectEnterEventArgs args)
    {
        if (args.interactorObject == null)
        {
            return;
        }

        _activeInteractor = args.interactorObject;
        _activeInteractorTransform =
            args.interactorObject.GetAttachTransform(args.interactableObject);

        if (_activeInteractorTransform == null)
        {
            _activeInteractorTransform = args.interactorObject.transform;
        }

        _previousInteractorPosition = _activeInteractorTransform.position;
        _hasPreviousInteractorPosition = true;
    }

    private void HandleDeselected(SelectExitEventArgs args)
    {
        if (args.interactorObject == null ||
            !ReferenceEquals(_activeInteractor, args.interactorObject))
        {
            return;
        }

        ClearInteractor();

        if (Mathf.Abs(_currentAngle) <= _closeSnapAngle)
        {
            SetAngle(0f);
        }
    }

    private void ClearInteractor()
    {
        _activeInteractor = null;
        _activeInteractorTransform = null;
        _hasPreviousInteractorPosition = false;
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
        Quaternion angleRotation = Quaternion.AngleAxis(_currentAngle, Vector3.up);
        transform.localRotation = _closedLocalRotation * angleRotation;
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
        _closeSnapAngle = Mathf.Max(0f, _closeSnapAngle);
    }
}
