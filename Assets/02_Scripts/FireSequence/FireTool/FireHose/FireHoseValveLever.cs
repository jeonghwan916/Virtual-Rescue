using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSimpleInteractable))]
public sealed class FireHoseValveLever : MonoBehaviour
{
    private const float SmallDistanceThreshold = 0.0001f;

    [Header("References")]
    [SerializeField] private Transform valvePivot;
    [SerializeField] private XRSimpleInteractable interactable;
    [SerializeField] private HoseButton hoseButton;

    [Header("Valve Rotation")]
    [SerializeField] private float minimumAngle = 0f;
    [SerializeField] private float maximumAngle = 90f;
    [SerializeField] private float enableThresholdAngle = 45f;
    [SerializeField] private float rotationSensitivity = 1f;
    [SerializeField] private float maximumDegreesPerFrame = 8f;
    [SerializeField] private float minimumInputDegrees = 0.02f;

    private Quaternion initialLocalRotation;
    private IXRSelectInteractor activeInteractor;
    private Transform activeInteractorTransform;
    private Vector3 previousInteractorPosition;
    private float currentAngle;
    private bool hasPreviousInteractorPosition;
    private bool isLeverEnabled;

    public float CurrentAngle => currentAngle;
    public bool IsLeverEnabled => isLeverEnabled;

    private void Reset()
    {
        valvePivot = transform;
        interactable = GetComponent<XRSimpleInteractable>();
        hoseButton = GetComponent<HoseButton>();
    }

    private void Awake()
    {
        if (valvePivot == null)
            valvePivot = transform;

        if (interactable == null)
            interactable = GetComponent<XRSimpleInteractable>();

        if (hoseButton == null)
            hoseButton = GetComponent<HoseButton>();

        initialLocalRotation = valvePivot.localRotation;
        SetAngle(currentAngle);
        EvaluateLeverState();
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

        float angleDelta = CalculateAngleDelta(previousInteractorPosition, currentPosition);
        previousInteractorPosition = currentPosition;

        if (Mathf.Abs(angleDelta) < minimumInputDegrees)
            return;

        SetAngle(currentAngle + angleDelta);
        EvaluateLeverState();
    }

    private void HandleSelected(SelectEnterEventArgs args)
    {
        if (args.interactorObject == null)
            return;

        activeInteractor = args.interactorObject;
        activeInteractorTransform = args.interactorObject.GetAttachTransform(args.interactableObject);

        if (activeInteractorTransform == null)
            activeInteractorTransform = args.interactorObject.transform;

        previousInteractorPosition = activeInteractorTransform.position;
        hasPreviousInteractorPosition = true;
    }

    private void HandleDeselected(SelectExitEventArgs args)
    {
        if (args.interactorObject == null || !ReferenceEquals(activeInteractor, args.interactorObject))
            return;

        ClearInteractor();
    }

    private void ClearInteractor()
    {
        activeInteractor = null;
        activeInteractorTransform = null;
        hasPreviousInteractorPosition = false;
    }

    private float CalculateAngleDelta(Vector3 previousPosition, Vector3 currentPosition)
    {
        Vector3 worldAxis = valvePivot.TransformDirection(Vector3.right);
        Vector3 previousOffset = Vector3.ProjectOnPlane(previousPosition - valvePivot.position, worldAxis);
        Vector3 currentOffset = Vector3.ProjectOnPlane(currentPosition - valvePivot.position, worldAxis);

        if (previousOffset.sqrMagnitude < SmallDistanceThreshold ||
            currentOffset.sqrMagnitude < SmallDistanceThreshold)
            return 0f;

        float signedAngle = Vector3.SignedAngle(previousOffset, currentOffset, worldAxis);
        float angleDelta = -signedAngle * rotationSensitivity;
        return Mathf.Clamp(angleDelta, -maximumDegreesPerFrame, maximumDegreesPerFrame);
    }

    private void SetAngle(float angle)
    {
        currentAngle = Mathf.Clamp(angle, minimumAngle, maximumAngle);
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        Quaternion angleRotation = Quaternion.AngleAxis(-currentAngle, Vector3.right);
        valvePivot.localRotation = initialLocalRotation * angleRotation;
    }

    private void EvaluateLeverState()
    {
        bool shouldBeEnabled = currentAngle >= enableThresholdAngle;

        if (isLeverEnabled == shouldBeEnabled)
            return;

        isLeverEnabled = shouldBeEnabled;

        if (isLeverEnabled)
            hoseButton?.LeverEnabled();
        else
            hoseButton?.LeverDisabled();
    }

    private void OnValidate()
    {
        if (maximumAngle < minimumAngle)
            maximumAngle = minimumAngle;

        enableThresholdAngle = Mathf.Clamp(enableThresholdAngle, minimumAngle, maximumAngle);
        rotationSensitivity = Mathf.Max(0f, rotationSensitivity);
        maximumDegreesPerFrame = Mathf.Max(0.01f, maximumDegreesPerFrame);
        minimumInputDegrees = Mathf.Max(0f, minimumInputDegrees);
    }
}
