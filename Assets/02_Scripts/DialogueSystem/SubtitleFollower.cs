using UnityEngine;

namespace VirtualRescue.DialogueSystem
{
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
        [SerializeField] private float rotationSmoothSpeed = 12f;

        [Header("Rotation")]
        [SerializeField] private bool followYawOnly = true;
        [SerializeField] private bool faceCamera = true;

        private Vector3 positionVelocity;

        private void Awake()
        {
            ResolveTargetCamera();
        }

        private void OnEnable()
        {
            ResolveTargetCamera();
            SnapToTarget();
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

            Vector3 targetPosition = targetCamera.position + forward * Mathf.Max(0f, distance) + Vector3.up * verticalOffset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref positionVelocity,
                Mathf.Max(0.001f, positionSmoothTime));

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

            transform.position = targetCamera.position + forward * Mathf.Max(0f, distance) + Vector3.up * verticalOffset;

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
}
