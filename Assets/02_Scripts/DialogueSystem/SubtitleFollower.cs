using UnityEngine;

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
    [SerializeField] private Transform targetCamera;

    [Header("Placement")]
    [Tooltip("카메라로부터 자막을 얼마나 앞에 배치할지 정함")]
    [SerializeField] private float distance = 2.0f;
    [Tooltip("카메라 기준 자막의 위아래 위치를 조절\n음수면 아래로 내려감")]
    [SerializeField] private float verticalOffset = -0.35f;

    [Header("Smoothing")]
    [Tooltip("자막 위치가 목표 방향을 따라가는 방식을 정함\nSmoothDamp는 부드럽게 감속\nMoveTowards는 일정 속도로 따라감")]
    [SerializeField] private PositionFollowMode positionFollowMode = PositionFollowMode.SmoothDamp;
    [Tooltip("SmoothDamp 모드에서 자막 위치가 목표 방향에 부드럽게 도달하는 데 걸리는 시간\n값이 작을수록 빠르게 따라감")]
    [SerializeField] private float positionSmoothTime = 0.5f;
    [Tooltip("MoveTowards 모드에서 자막 위치가 목표 방향을 따라가는 이동 속도\n값이 클수록 빠르게 따라감")]
    [SerializeField] private float moveSpeed = 6f;
    [Tooltip("자막이 카메라를 바라보도록 회전할 때의 부드러움\n값이 클수록 빠르게 회전")]
    [SerializeField] private float rotationSmoothSpeed = 12f;

    [Header("Rotation")]
    [Tooltip("카메라가 이 각도 이상 회전했을 때만 자막의 목표 방향을 갱신\n작은 시점 흔들림을 무시하기 위해 사용")]
    [SerializeField] private float minCameraRotationAngle = 27.5f;
    [Tooltip("자막이 현재 카메라 방향에 이 각도 이내로 가까워지면 추적을 멈추고 다시 작은 흔들림을 무시")]
    [SerializeField] private float stopTrackingAngle = 1f;
    
    [Header("Camera Follow")]
    [Tooltip("켜져 있으면 카메라의 좌우 회전만 따라감\n위아래 회전은 자막 위치 계산에서 무시")]
    [SerializeField] private bool followYawOnly = true;
    [Tooltip("켜져 있으면 자막이 항상 카메라를 바라보도록 회전")]
    [SerializeField] private bool faceCamera = true;

    private Vector3 followDirection;
    private Vector3 acceptedTargetDirection;
    private bool hasFollowDirection;
    private bool isTrackingCameraRotation;

    private void Awake()
    {
        ResolveTargetCamera();
    }

    private void OnEnable()
    {
        ResolveTargetCamera();
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            ResolveTargetCamera();
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 forward = GetTargetForward();
        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        MoveToTargetPosition(forward);

        if (!faceCamera)
        {
            return;
        }

        Quaternion targetRotation = GetLookAtCameraRotation();
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-Mathf.Max(0f, rotationSmoothSpeed) * Time.deltaTime));
    }

    private void ResolveTargetCamera()
    {
        if (targetCamera != null) return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            targetCamera = mainCamera.transform;
        }
    }

    private void SnapToTarget()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 forward = GetTargetForward();
        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        followDirection = forward;
        acceptedTargetDirection = forward;
        hasFollowDirection = true;
        isTrackingCameraRotation = false;
        transform.position = GetFollowPosition(followDirection);

        if (faceCamera)
        {
            transform.rotation = GetLookAtCameraRotation();
        }
    }

    private Vector3 GetTargetForward()
    {
        Vector3 forward = targetCamera.forward;

        if (followYawOnly)
        {
            forward.y = 0f;
        }

        return forward.normalized;
    }

    private void MoveToTargetPosition(Vector3 targetDirection)
    {
        if (!hasFollowDirection)
        {
            followDirection = targetDirection;
            acceptedTargetDirection = targetDirection;
            hasFollowDirection = true;
            isTrackingCameraRotation = false;
        }

        if (!isTrackingCameraRotation &&
            Vector3.Angle(acceptedTargetDirection, targetDirection) >= Mathf.Max(0f, minCameraRotationAngle))
        {
            isTrackingCameraRotation = true;
        }

        if (isTrackingCameraRotation)
        {
            acceptedTargetDirection = targetDirection;
        }

        switch (positionFollowMode)
        {
            case PositionFollowMode.MoveTowards:
                followDirection = Vector3.RotateTowards(
                    followDirection,
                    acceptedTargetDirection,
                    GetMoveTowardsRadiansPerSecond() * Time.deltaTime,
                    0f).normalized;
                break;

            case PositionFollowMode.SmoothDamp:
            default:
                followDirection = Vector3.RotateTowards(
                    followDirection,
                    acceptedTargetDirection,
                    GetSmoothDampRadiansThisFrame(acceptedTargetDirection),
                    0f).normalized;
                break;
        }

        if (isTrackingCameraRotation &&
            Vector3.Angle(followDirection, acceptedTargetDirection) <= Mathf.Max(0f, stopTrackingAngle))
        {
            isTrackingCameraRotation = false;
            acceptedTargetDirection = targetDirection;
        }

        transform.position = GetFollowPosition(followDirection);
    }

    private Vector3 GetFollowPosition(Vector3 direction)
    {
        return targetCamera.position + direction * Mathf.Max(0f, distance) + Vector3.up * verticalOffset;
    }

    private float GetMoveTowardsRadiansPerSecond()
    {
        float radius = Mathf.Max(0.001f, distance);
        return Mathf.Max(0f, moveSpeed) / radius;
    }

    private float GetSmoothDampRadiansThisFrame(Vector3 targetDirection)
    {
        float angle = Vector3.Angle(followDirection, targetDirection) * Mathf.Deg2Rad;
        float smoothFactor = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, positionSmoothTime));
        return angle * smoothFactor;
    }

    private Quaternion GetLookAtCameraRotation()
    {
        Vector3 lookDirection = targetCamera.position - transform.position;

        if (followYawOnly)
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
