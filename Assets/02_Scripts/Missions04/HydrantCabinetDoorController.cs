using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualRescue.Missions04
{
    [DisallowMultipleComponent]
    public sealed class HydrantCabinetDoorController : MonoBehaviour
    {
        private const float SmallDistanceThreshold = 0.0001f;
        private const string DefaultDoorObjectName = "door";

        [Header("References")]
        [SerializeField] private Transform _doorTransform;
        [SerializeField] private XRSimpleInteractable _doorInteractable;
        [SerializeField] private BoxCollider _doorCollider;

        [Header("Door Rotation")]
        [SerializeField] private float _minimumAngle = -110f;
        [SerializeField] private float _maximumAngle;
        [SerializeField] private float _rotationSensitivity = 1f;
        [SerializeField] private float _maximumDegreesPerFrame = 6f;
        [SerializeField] private float _minimumInputDegrees = 0.02f;

        [Header("Door State")]
        [SerializeField] private float _openConfirmationAngle = 4f;
        [SerializeField] private float _closeSnapAngle = 2f;

        private Quaternion _closedLocalRotation;
        private IXRSelectInteractor _activeInteractor;
        private Transform _activeInteractorTransform;
        private Vector3 _previousInteractorPosition;
        private float _currentAngle;
        private bool _hasPreviousInteractorPosition;
        private bool _isOpen;

        public event Action Opened;
        public event Action Closed;

        public bool IsOpen => _isOpen;
        public bool IsClosed => !_isOpen && Mathf.Abs(_currentAngle) <= _closeSnapAngle;
        public float CurrentAngle => _currentAngle;

        private void Awake()
        {
            ResolveReferences();

            if (_doorTransform == null || _doorInteractable == null)
            {
                Debug.LogError("소화전 캐비닛의 door 오브젝트 또는 XRSimpleInteractable을 찾을 수 없습니다.", this);
                enabled = false;
                return;
            }

            _closedLocalRotation = _doorTransform.localRotation;
            _currentAngle = 0f;
            _isOpen = false;
            ApplyRotation();
        }

        private void OnEnable()
        {
            if (_doorInteractable == null)
            {
                return;
            }

            _doorInteractable.selectEntered.AddListener(HandleSelected);
            _doorInteractable.selectExited.AddListener(HandleDeselected);
        }

        private void OnDisable()
        {
            if (_doorInteractable != null)
            {
                _doorInteractable.selectEntered.RemoveListener(HandleSelected);
                _doorInteractable.selectExited.RemoveListener(HandleDeselected);
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
            EvaluateDoorState();
        }

        private void ResolveReferences()
        {
            if (_doorTransform == null)
            {
                Transform searchRoot = transform.parent != null ? transform.parent : transform;
                _doorTransform = FindDoorTransform(searchRoot);
            }

            if (_doorTransform == null)
            {
                return;
            }

            if (_doorCollider == null)
            {
                _doorCollider = _doorTransform.GetComponent<BoxCollider>();
            }

            if (_doorCollider == null)
            {
                _doorCollider = _doorTransform.gameObject.AddComponent<BoxCollider>();
                ConfigureColliderFromMesh();
            }

            if (_doorInteractable == null)
            {
                _doorInteractable = _doorTransform.GetComponent<XRSimpleInteractable>();
            }

            if (_doorInteractable == null)
            {
                _doorInteractable = _doorTransform.gameObject.AddComponent<XRSimpleInteractable>();
            }
        }

        private static Transform FindDoorTransform(Transform searchRoot)
        {
            Transform[] transforms = searchRoot.GetComponentsInChildren<Transform>(true);

            foreach (Transform candidate in transforms)
            {
                if (string.Equals(candidate.name, DefaultDoorObjectName, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ConfigureColliderFromMesh()
        {
            MeshFilter meshFilter = _doorTransform.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning("소화전 캐비닛 door 메시를 찾을 수 없어 BoxCollider 크기를 자동 설정하지 못했습니다.", this);
                return;
            }

            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            _doorCollider.center = meshBounds.center;
            _doorCollider.size = meshBounds.size;
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
        }

        private void HandleDeselected(SelectExitEventArgs args)
        {
            if (args.interactorObject == null || !ReferenceEquals(_activeInteractor, args.interactorObject))
            {
                return;
            }

            ClearInteractor();

            if (Mathf.Abs(_currentAngle) <= _closeSnapAngle)
            {
                CloseDoor();
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
            Vector3 worldAxis = _doorTransform.TransformDirection(Vector3.up);
            Vector3 previousOffset = Vector3.ProjectOnPlane(previousPosition - _doorTransform.position, worldAxis);
            Vector3 currentOffset = Vector3.ProjectOnPlane(currentPosition - _doorTransform.position, worldAxis);

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
            _doorTransform.localRotation = _closedLocalRotation * angleRotation;
        }

        private void EvaluateDoorState()
        {
            bool isOpenNow = Mathf.Abs(_currentAngle) >= _openConfirmationAngle;

            if (!_isOpen && isOpenNow)
            {
                _isOpen = true;
                Opened?.Invoke();
                return;
            }

            if (_isOpen && Mathf.Abs(_currentAngle) <= _closeSnapAngle)
            {
                CloseDoor();
            }
        }

        private void CloseDoor()
        {
            bool wasOpen = _isOpen;
            SetAngle(0f);
            _isOpen = false;

            if (wasOpen)
            {
                Closed?.Invoke();
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
            _closeSnapAngle = Mathf.Clamp(_closeSnapAngle, 0.01f, _openConfirmationAngle);
        }
    }
}
