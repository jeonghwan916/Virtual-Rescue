using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualRescue.Interaction
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class InductionLeverRotator : MonoBehaviour
    {
        [SerializeField] private XRSimpleInteractable _interactable;

        [Header("Rotation")]
        [SerializeField] private float _minimumAngle = 0f;
        [SerializeField] private float _maximumAngle = 30f;
        [SerializeField] private float _rotationSensitivity = 1f;
        [SerializeField] private float _maximumDegreesPerFrame = 8f;

        [Tooltip("손목 회전 방향이 반대로 움직이면 활성화")]
        [SerializeField] private bool _invertDirection;

        private IXRSelectInteractor _activeInteractor;
        private Transform _activeInteractorTransform;
        private Quaternion _previousInteractorRotation;

        private Quaternion _startOnRotation;
        private float _currentAngle;

        public float CurrentAngle => _currentAngle;

        private void Reset()
        {
            _interactable =
                GetComponent<XRSimpleInteractable>();
        }

        private void Awake()
        {
            if (_interactable == null)
            {
                _interactable =
                    GetComponent<XRSimpleInteractable>();
            }
        }

        private void Start()
        {
            // InductionLeverHeat가 설정한 30도 On 위치를 저장합니다.
            _startOnRotation = transform.localRotation;
            _currentAngle = _maximumAngle;
        }

        private void OnEnable()
        {
            _interactable.selectEntered.AddListener(
                HandleSelected);

            _interactable.selectExited.AddListener(
                HandleDeselected);
        }

        private void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(
                HandleSelected);

            _interactable.selectExited.RemoveListener(
                HandleDeselected);

            ClearInteractor();
        }

        private void Update()
        {
            if (_activeInteractorTransform == null)
                return;

            Quaternion currentRotation =
                _activeInteractorTransform.rotation;

            Quaternion rotationDelta =
                currentRotation *
                Quaternion.Inverse(
                    _previousInteractorRotation);

            _previousInteractorRotation = currentRotation;

            rotationDelta.ToAngleAxis(
                out float angle,
                out Vector3 axis);

            if (angle > 180f)
                angle -= 360f;

            Vector3 leverWorldAxis =
                transform.TransformDirection(Vector3.up);

            float direction =
                Vector3.Dot(axis, leverWorldAxis) >= 0f
                    ? 1f
                    : -1f;

            float angleDelta =
                angle * direction * _rotationSensitivity;

            if (_invertDirection)
                angleDelta = -angleDelta;

            angleDelta = Mathf.Clamp(
                angleDelta,
                -_maximumDegreesPerFrame,
                _maximumDegreesPerFrame);

            SetAngle(_currentAngle + angleDelta);
        }

        private void HandleSelected(
            SelectEnterEventArgs args)
        {
            _activeInteractor = args.interactorObject;

            _activeInteractorTransform =
                args.interactorObject.GetAttachTransform(
                    args.interactableObject);

            if (_activeInteractorTransform == null)
            {
                _activeInteractorTransform =
                    args.interactorObject.transform;
            }

            _previousInteractorRotation =
                _activeInteractorTransform.rotation;
        }

        private void HandleDeselected(
            SelectExitEventArgs args)
        {
            if (!ReferenceEquals(
                    _activeInteractor,
                    args.interactorObject))
            {
                return;
            }

            ClearInteractor();
        }

        private void SetAngle(float angle)
        {
            _currentAngle = Mathf.Clamp(
                angle,
                _minimumAngle,
                _maximumAngle);

            // 시작 위치가 30도이므로 0도까지 상대적으로 회전시킵니다.
            float rotationFromStart =
                _currentAngle - _maximumAngle;

            transform.localRotation =
                _startOnRotation *
                Quaternion.AngleAxis(
                    rotationFromStart,
                    Vector3.up);
        }

        private void ClearInteractor()
        {
            _activeInteractor = null;
            _activeInteractorTransform = null;
        }
    }
}
