using UnityEngine;
using UnityEngine.Serialization;

namespace VirtualRescue.DialogueSystem
{
    [DisallowMultipleComponent]
    public class SubtitleFollower : MonoBehaviour
    {
        private enum PositionFollowMode
        {
            SmoothDamp,
            MoveTowards
        }

        [Header("Target")]
        [Tooltip("자막이 따라갈 기준 카메라. 비워둬도 Main Camera를 자동으로 찾음")]
        [FormerlySerializedAs("targetCamera")]
        [SerializeField] private Transform _targetCamera;

        [Header("Placement")]
        [Tooltip("카메라로부터 자막을 얼마나 앞에 배치할지 정함")]
        [FormerlySerializedAs("distance")]
        [SerializeField] private float _distance = 2.0f;
        [Tooltip("카메라 기준 자막의 위아래 위치를 조절\n음수면 아래로 내려감")]
        [FormerlySerializedAs("verticalOffset")]
        [SerializeField] private float _verticalOffset = -0.35f;

        [Header("Smoothing")]
        [Tooltip("자막 위치가 목표 방향을 따라가는 방식을 정함\nSmoothDamp는 부드럽게 감속\nMoveTowards는 일정 속도로 따라감")]
        [FormerlySerializedAs("positionFollowMode")]
        [SerializeField] private PositionFollowMode _positionFollowMode = PositionFollowMode.SmoothDamp;
        [Tooltip("SmoothDamp 모드에서 자막 위치가 목표 방향에 부드럽게 도달하는 데 걸리는 시간\n값이 작을수록 빠르게 따라감")]
        [FormerlySerializedAs("positionSmoothTime")]
        [SerializeField] private float _positionSmoothTime = 0.5f;
        [Tooltip("MoveTowards 모드에서 자막 위치가 목표 방향을 따라가는 이동 속도\n값이 클수록 빠르게 따라감")]
        [FormerlySerializedAs("moveSpeed")]
        [SerializeField] private float _moveSpeed = 6f;
        [Tooltip("자막이 카메라를 바라보도록 회전할 때의 부드러움\n값이 클수록 빠르게 회전")]
        [FormerlySerializedAs("rotationSmoothSpeed")]
        [SerializeField] private float _rotationSmoothSpeed = 12f;

        [Header("Rotation")]
        [Tooltip("카메라가 이 각도 이상 회전했을 때만 자막의 목표 방향을 갱신\n작은 시점 흔들림을 무시하기 위해 사용")]
        [FormerlySerializedAs("minCameraRotationAngle")]
        [SerializeField] private float _minCameraRotationAngle = 27.5f;
        [Tooltip("자막이 현재 카메라 방향에 이 각도 이내로 가까워지면 추적을 멈추고 다시 작은 흔들림을 무시")]
        [FormerlySerializedAs("stopTrackingAngle")]
        [SerializeField] private float _stopTrackingAngle = 1f;
    
        [Header("Camera Follow")]
        [Tooltip("켜져 있으면 카메라의 좌우 회전만 따라감\n위아래 회전은 자막 위치 계산에서 무시")]
        [FormerlySerializedAs("followYawOnly")]
        [SerializeField] private bool _followYawOnly = true;
        [Tooltip("켜져 있으면 자막이 항상 카메라를 바라보도록 회전")]
        [FormerlySerializedAs("faceCamera")]
        [SerializeField] private bool _faceCamera = true;

        private Vector3 _followDirection;
        private Vector3 _acceptedTargetDirection;
        private bool _hasFollowDirection;
        private bool _isTrackingCameraRotation;

        private void Awake()
        {
            ResolveTargetCamera();
        }

        private void OnEnable()
        {
            Debug.Log("Enable");
            ResolveTargetCamera();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (_targetCamera == null)
            {
                ResolveTargetCamera();
            }

            if (_targetCamera == null)
            {
                return;
            }

            Vector3 forward = GetTargetForward();
            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            MoveToTargetPosition(forward);

            if (!_faceCamera)
            {
                return;
            }

            Quaternion targetRotation = GetLookAtCameraRotation();
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-Mathf.Max(0f, _rotationSmoothSpeed) * Time.deltaTime));
        }

        private void ResolveTargetCamera()
        {
            if (_targetCamera != null) return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _targetCamera = mainCamera.transform;
            }
        }

        private void SnapToTarget()
        {
            if (_targetCamera == null)
            {
                return;
            }

            Vector3 forward = GetTargetForward();
            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _followDirection = forward;
            _acceptedTargetDirection = forward;
            _hasFollowDirection = true;
            _isTrackingCameraRotation = false;
            transform.position = GetFollowPosition(_followDirection);

            if (_faceCamera)
            {
                transform.rotation = GetLookAtCameraRotation();
            }
        }

        private Vector3 GetTargetForward()
        {
            Vector3 forward = _targetCamera.forward;

            if (_followYawOnly)
            {
                forward.y = 0f;
            }

            return forward.normalized;
        }

        private void MoveToTargetPosition(Vector3 targetDirection)
        {
            if (!_hasFollowDirection)
            {
                _followDirection = targetDirection;
                _acceptedTargetDirection = targetDirection;
                _hasFollowDirection = true;
                _isTrackingCameraRotation = false;
            }

            if (!_isTrackingCameraRotation &&
                Vector3.Angle(_acceptedTargetDirection, targetDirection) >= Mathf.Max(0f, _minCameraRotationAngle))
            {
                _isTrackingCameraRotation = true;
            }

            if (_isTrackingCameraRotation)
            {
                _acceptedTargetDirection = targetDirection;
            }

            switch (_positionFollowMode)
            {
                case PositionFollowMode.MoveTowards:
                    _followDirection = Vector3.RotateTowards(
                        _followDirection,
                        _acceptedTargetDirection,
                        GetMoveTowardsRadiansPerSecond() * Time.deltaTime,
                        0f).normalized;
                    break;

                case PositionFollowMode.SmoothDamp:
                default:
                    _followDirection = Vector3.RotateTowards(
                        _followDirection,
                        _acceptedTargetDirection,
                        GetSmoothDampRadiansThisFrame(_acceptedTargetDirection),
                        0f).normalized;
                    break;
            }

            if (_isTrackingCameraRotation &&
                Vector3.Angle(_followDirection, _acceptedTargetDirection) <= Mathf.Max(0f, _stopTrackingAngle))
            {
                _isTrackingCameraRotation = false;
                _acceptedTargetDirection = targetDirection;
            }

            transform.position = GetFollowPosition(_followDirection);
        }

        private Vector3 GetFollowPosition(Vector3 direction)
        {
            return _targetCamera.position + direction * Mathf.Max(0f, _distance) + Vector3.up * _verticalOffset;
        }

        private float GetMoveTowardsRadiansPerSecond()
        {
            float radius = Mathf.Max(0.001f, _distance);
            return Mathf.Max(0f, _moveSpeed) / radius;
        }

        private float GetSmoothDampRadiansThisFrame(Vector3 targetDirection)
        {
            float angle = Vector3.Angle(_followDirection, targetDirection) * Mathf.Deg2Rad;
            float smoothFactor = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, _positionSmoothTime));
            return angle * smoothFactor;
        }

        private Quaternion GetLookAtCameraRotation()
        {
            Vector3 lookDirection = _targetCamera.position - transform.position;

            if (_followYawOnly)
            {
                lookDirection.y = 0f;
            }

            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                return transform.rotation;
            }

            return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }
}
