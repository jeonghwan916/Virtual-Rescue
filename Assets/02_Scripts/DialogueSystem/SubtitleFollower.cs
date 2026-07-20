using UnityEngine;

[DisallowMultipleComponent]
public class SubtitleFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform targetCamera;

    [Header("Placement")]
    [SerializeField] private float distance = 2.0f;
    [SerializeField] private float verticalOffset = -0.35f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.18f;
    [SerializeField] private float rotationSmoothSpeed = 8f;
    [SerializeField] private float followAngleThresholdDegrees = 25f;

    [Header("Rotation")]
    [SerializeField] private bool followYawOnly = true;
    [SerializeField] private bool faceCamera = true;

    private Vector3 positionVelocity;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasFollowTarget;

    private void Awake()
    {
        ResolveTargetCamera();
    }

    private void OnEnable()
    {
        ResolveTargetCamera();
        hasFollowTarget = false;
        positionVelocity = Vector3.zero;
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

        if (!hasFollowTarget)
        {
            SetFollowTarget(forward, true);
        }
        else if (ShouldRecenter(forward))
        {
            SetFollowTarget(forward, false);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref positionVelocity,
            Mathf.Max(0.001f, positionSmoothTime));

        if (!faceCamera)
        {
            return;
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-Mathf.Max(0f, rotationSmoothSpeed) * Time.deltaTime));
    }

    private void ResolveTargetCamera()
    {
        if (targetCamera != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            targetCamera = mainCamera.transform;
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

    private bool ShouldRecenter(Vector3 forward)
    {
        Vector3 directionToSubtitle = targetPosition - targetCamera.position;
        if (followYawOnly)
        {
            directionToSubtitle.y = 0f;
        }

        if (directionToSubtitle.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        float angleFromView = Vector3.Angle(forward, directionToSubtitle.normalized);
        return angleFromView > Mathf.Max(0f, followAngleThresholdDegrees);
    }

    private void SetFollowTarget(Vector3 forward, bool snapToTarget)
    {
        targetPosition = targetCamera.position + forward * Mathf.Max(0f, distance) + Vector3.up * verticalOffset;
        targetRotation = GetTargetRotation(forward);
        hasFollowTarget = true;

        if (!snapToTarget)
        {
            return;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        positionVelocity = Vector3.zero;
    }

    private Quaternion GetTargetRotation(Vector3 forward)
    {
        if (!faceCamera)
        {
            return transform.rotation;
        }

        if (followYawOnly)
        {
            return Quaternion.LookRotation(-forward, Vector3.up);
        }

        Vector3 lookDirection = targetCamera.position - targetPosition;
        return lookDirection.sqrMagnitude < 0.0001f
            ? transform.rotation
            : Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }
}
