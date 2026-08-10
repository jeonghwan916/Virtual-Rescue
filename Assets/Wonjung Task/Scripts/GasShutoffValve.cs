using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSimpleInteractable))]
public sealed class GasShutoffValve : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    private const float SmallDistanceThreshold = 0.0001f;

    [Header("References")]
    [SerializeField] private Transform valvePivot;
    [SerializeField] private XRSimpleInteractable interactable;

    [Header("Rotation")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.X;

    // 열림: 0도, 잠김: -90도
    [SerializeField] private float minimumAngle = -90f;
    [SerializeField] private float maximumAngle = 0f;

    [SerializeField] private float rotationSensitivity = 1f;
    [SerializeField] private float maximumDegreesPerFrame = 8f;
    [SerializeField] private float minimumInputDegrees = 0.02f;

    [Tooltip("손을 왼쪽으로 돌렸는데 각도가 반대로 움직이면 체크합니다.")]
    [SerializeField] private bool invertDirection;

    [Header("State Thresholds")]
    [Tooltip("이 각도 이하가 되면 잠긴 것으로 판정합니다.")]
    [SerializeField] private float closedThresholdAngle = -85f;

    [Tooltip("다시 이 각도 이상이 되면 열린 것으로 판정합니다.")]
    [SerializeField] private float openedThresholdAngle = -5f;

    private Quaternion initialLocalRotation;

    private IXRSelectInteractor activeInteractor;
    private Transform activeInteractorTransform;
    private Vector3 previousInteractorPosition;

    private float currentAngle;
    private bool hasPreviousInteractorPosition;
    private bool isClosed;

    public float CurrentAngle => currentAngle;
    public bool IsClosed => isClosed;
    public bool IsOpen => !isClosed;

    public event Action Closed;
    public event Action Opened;

    private void Reset()
    {
        valvePivot = transform;
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void Awake()
    {
        if (valvePivot == null)
            valvePivot = transform;

        if (interactable == null)
            interactable = GetComponent<XRSimpleInteractable>();

        initialLocalRotation = valvePivot.localRotation;

        // 시작 상태는 0도, 즉 열린 상태
        currentAngle = maximumAngle;

        ApplyRotation();
        EvaluateValveState();
    }

    private void OnEnable()
    {
        if (interactable == null)
            return;

        interactable.selectEntered.AddListener(HandleSelected);
        interactable.selectExited.AddListener(HandleDeselected);
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(HandleSelected);
            interactable.selectExited.RemoveListener(HandleDeselected);
        }

        ClearInteractor();
    }

    private void Update()
    {
        if (activeInteractorTransform == null)
            return;

        Vector3 currentPosition = activeInteractorTransform.position;

        if (!hasPreviousInteractorPosition)
        {
            previousInteractorPosition = currentPosition;
            hasPreviousInteractorPosition = true;
            return;
        }

        float angleDelta = CalculateAngleDelta(
            previousInteractorPosition,
            currentPosition);

        previousInteractorPosition = currentPosition;

        if (Mathf.Abs(angleDelta) < minimumInputDegrees)
            return;

        SetAngle(currentAngle + angleDelta);
        EvaluateValveState();
    }

    private void HandleSelected(SelectEnterEventArgs args)
    {
        if (args.interactorObject == null)
            return;

        activeInteractor = args.interactorObject;

        activeInteractorTransform =
            args.interactorObject.GetAttachTransform(
                args.interactableObject);

        if (activeInteractorTransform == null)
            activeInteractorTransform = args.interactorObject.transform;

        previousInteractorPosition =
            activeInteractorTransform.position;

        hasPreviousInteractorPosition = true;
    }

    private void HandleDeselected(SelectExitEventArgs args)
    {
        if (args.interactorObject == null ||
            !ReferenceEquals(
                activeInteractor,
                args.interactorObject))
        {
            return;
        }

        ClearInteractor();
    }

    private void ClearInteractor()
    {
        activeInteractor = null;
        activeInteractorTransform = null;
        hasPreviousInteractorPosition = false;
    }

    private float CalculateAngleDelta(
        Vector3 previousPosition,
        Vector3 currentPosition)
    {
        Vector3 localAxis = GetLocalRotationAxis();

        Vector3 worldAxis =
            valvePivot.TransformDirection(localAxis);

        Vector3 previousOffset = Vector3.ProjectOnPlane(
            previousPosition - valvePivot.position,
            worldAxis);

        Vector3 currentOffset = Vector3.ProjectOnPlane(
            currentPosition - valvePivot.position,
            worldAxis);

        if (previousOffset.sqrMagnitude < SmallDistanceThreshold ||
            currentOffset.sqrMagnitude < SmallDistanceThreshold)
        {
            return 0f;
        }

        float signedAngle = Vector3.SignedAngle(
            previousOffset,
            currentOffset,
            worldAxis);

        float angleDelta =
            signedAngle * rotationSensitivity;

        if (invertDirection)
            angleDelta = -angleDelta;

        return Mathf.Clamp(
            angleDelta,
            -maximumDegreesPerFrame,
            maximumDegreesPerFrame);
    }

    private void SetAngle(float angle)
    {
        currentAngle = Mathf.Clamp(
            angle,
            minimumAngle,
            maximumAngle);

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        Vector3 localAxis = GetLocalRotationAxis();

        Quaternion angleRotation = Quaternion.AngleAxis(
            currentAngle,
            localAxis);

        valvePivot.localRotation =
            initialLocalRotation * angleRotation;
    }

    private void EvaluateValveState()
    {
        // 열림 상태에서 -85도 이하가 되면 잠김
        if (!isClosed &&
            currentAngle <= closedThresholdAngle)
        {
            isClosed = true;
            Closed?.Invoke();
            return;
        }

        // 잠긴 상태에서 -5도 이상으로 돌아오면 다시 열림
        if (isClosed &&
            currentAngle >= openedThresholdAngle)
        {
            isClosed = false;
            Opened?.Invoke();
        }
    }

    private Vector3 GetLocalRotationAxis()
    {
        switch (rotationAxis)
        {
            case RotationAxis.Y:
                return Vector3.up;

            case RotationAxis.Z:
                return Vector3.forward;

            default:
                return Vector3.right;
        }
    }

    private void OnValidate()
    {
        if (maximumAngle < minimumAngle)
            maximumAngle = minimumAngle;

        closedThresholdAngle = Mathf.Clamp(
            closedThresholdAngle,
            minimumAngle,
            maximumAngle);

        openedThresholdAngle = Mathf.Clamp(
            openedThresholdAngle,
            closedThresholdAngle,
            maximumAngle);

        rotationSensitivity =
            Mathf.Max(0f, rotationSensitivity);

        maximumDegreesPerFrame =
            Mathf.Max(0.01f, maximumDegreesPerFrame);

        minimumInputDegrees =
            Mathf.Max(0f, minimumInputDegrees);
    }
}
