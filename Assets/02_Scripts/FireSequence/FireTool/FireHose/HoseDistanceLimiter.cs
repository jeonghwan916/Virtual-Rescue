using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HoseDistanceLimiter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRSocketInteractor socketInteractor;
    [SerializeField] private Transform hydrantAnchor;
    [SerializeField] private Transform limitedTarget;
    [SerializeField] private XRGrabInteractable requiredGrabInteractable;
    [SerializeField] private bool usePlayerReferenceHubAsLimitedTarget = true;

    [Header("Distance Limit")]
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private bool limitHorizontalOnly = true;
    [SerializeField] private float distanceEventThreshold = 5f;

    [Header("Grab Requirement")]
    [SerializeField] private bool requireHeldTarget = true;

    private bool isConnected = true;
    private readonly HashSet<IXRSelectInteractor> holdingInteractors = new();

    public bool IsConnected => isConnected;
    public bool IsHeld => holdingInteractors.Count > 0;
    public Transform HydrantAnchor => hydrantAnchor;
    public Transform LimitedTarget => limitedTarget;
    public event Action<bool> ConnectionChanged;
    public event Action DistanceThresholdReached;

    private void Awake()
    {
        if (socketInteractor == null)
            socketInteractor = GetComponent<XRSocketInteractor>();
    }

    private IEnumerator Start()
    {
        BindLimitedTargetFromPlayerReferenceHub();
        SyncConnectionFromSocket();
        SyncHeldStateFromRequiredGrab();
        yield return null;
        BindLimitedTargetFromPlayerReferenceHub();
        SyncConnectionFromSocket();
        SyncHeldStateFromRequiredGrab();
    }

    private void OnEnable()
    {
        if (requiredGrabInteractable == null)
            return;

        requiredGrabInteractable.selectEntered.AddListener(OnRequiredGrabEntered);
        requiredGrabInteractable.selectExited.AddListener(OnRequiredGrabExited);
    }

    private void OnDisable()
    {
        if (requiredGrabInteractable != null)
        {
            requiredGrabInteractable.selectEntered.RemoveListener(OnRequiredGrabEntered);
            requiredGrabInteractable.selectExited.RemoveListener(OnRequiredGrabExited);
        }

        holdingInteractors.Clear();
    }

    // Applies the distance limit after other movement systems have updated the target position.
    private void LateUpdate()
    {
        BindLimitedTargetFromPlayerReferenceHub();

        if (!CanLimitDistance())
            return;

        EvaluateDistanceThreshold();
        LimitDistance();
    }

    // Enables or disables distance limiting based on the hose connection state.
    public void SetConnected(bool connected)
    {
        if (isConnected == connected)
            return;

        isConnected = connected;
        ConnectionChanged?.Invoke(isConnected);
    }

    private void SyncConnectionFromSocket()
    {
        if (socketInteractor == null)
            return;

        if (socketInteractor.hasSelection)
            SetConnected(true);
    }

    private void SyncHeldStateFromRequiredGrab()
    {
        if (requiredGrabInteractable == null)
            return;

        holdingInteractors.Clear();

        foreach (IXRSelectInteractor interactor in requiredGrabInteractable.interactorsSelecting)
        {
            if (interactor is XRSocketInteractor)
                continue;

            holdingInteractors.Add(interactor);
        }
    }

    private void BindLimitedTargetFromPlayerReferenceHub()
    {
        if (!usePlayerReferenceHubAsLimitedTarget || limitedTarget != null)
            return;

        PlayerReferenceHub playerReferenceHub = PlayerReferenceHub.Instance;

        if (playerReferenceHub == null || playerReferenceHub.PlayerTransform == null)
            return;

        limitedTarget = playerReferenceHub.PlayerTransform;
    }

    // Enables distance limiting from a UnityEvent without requiring event arguments.
    public void Connect()
    {
        SetConnected(true);
    }

    // Disables distance limiting from a UnityEvent without requiring event arguments.
    public void Disconnect()
    {
        SetConnected(false);
    }

    // Handles XR socket selection and enables distance limiting when the hose is connected.
    public void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        SetConnected(true);
    }

    // Handles XR socket release and disables distance limiting when the hose is disconnected.
    public void OnSocketSelectExited(SelectExitEventArgs args)
    {
        SetConnected(false);
    }

    // Tracks when the hose end is grabbed by a non-socket interactor.
    public void OnRequiredGrabEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor)
            return;

        holdingInteractors.Add(args.interactorObject);
    }

    // Tracks when the hose end is released by a non-socket interactor.
    public void OnRequiredGrabExited(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor)
            return;

        holdingInteractors.Remove(args.interactorObject);
    }

    // Replaces the hydrant anchor used as the distance limit origin.
    public void SetHydrantAnchor(Transform anchor)
    {
        hydrantAnchor = anchor;
    }

    // Replaces the target transform whose movement is constrained by the hose length.
    public void SetLimitedTarget(Transform target)
    {
        limitedTarget = target;
    }

    // Checks whether all required state and references are available before limiting movement.
    private bool CanLimitDistance()
    {
        return isConnected
            && (!requireHeldTarget || IsHeld)
            && hydrantAnchor != null
            && limitedTarget != null;
    }

    // Clamps the target position so it stays within the configured anchor distance.
    private void LimitDistance()
    {
        float maxDistanceSqr = maxDistance * maxDistance;
        Vector3 offset = GetOffsetFromAnchor();

        if (offset.sqrMagnitude <= maxDistanceSqr)
            return;

        Vector3 clampedOffset = offset.normalized * maxDistance;
        Vector3 correctedPosition = hydrantAnchor.position + clampedOffset;

        if (limitHorizontalOnly)
            correctedPosition.y = limitedTarget.position.y;

        limitedTarget.position = correctedPosition;
    }

    private void EvaluateDistanceThreshold()
    {
        float distance = GetOffsetFromAnchor().magnitude;
        bool reachedThreshold = distance >= distanceEventThreshold;

        if (reachedThreshold)
        {
            Debug.Log($"{nameof(HoseDistanceLimiter)} distance threshold reached. Distance: {distance:F2}", this);
            DistanceThresholdReached?.Invoke();
        }
    }

    // Calculates the target offset from the anchor, optionally ignoring vertical movement.
    private Vector3 GetOffsetFromAnchor()
    {
        Vector3 offset = limitedTarget.position - hydrantAnchor.position;

        if (limitHorizontalOnly)
            offset.y = 0.0f;

        return offset;
    }

    private void OnValidate()
    {
        maxDistance = Mathf.Max(0.0f, maxDistance);
        distanceEventThreshold = Mathf.Max(0.0f, distanceEventThreshold);
    }
}
