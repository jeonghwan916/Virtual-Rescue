using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HoseDistanceLimiter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRSocketInteractor socketInteractor;
    [SerializeField] private Transform hydrantAnchor;
    [SerializeField] private Transform limitedTarget;
    [SerializeField] private VerletRope rope;

    [Header("Distance Limit")]
    [SerializeField] private float slack = 0.15f;
    [SerializeField] private bool limitHorizontalOnly = true;

    [Header("Grab Requirement")]
    [SerializeField] private bool requireHeldTarget = true;

    private bool isConnected = true;
    private int heldSelectCount;

    public bool IsConnected => isConnected;
    public bool IsHeld => heldSelectCount > 0;
    public Transform HydrantAnchor => hydrantAnchor;
    public Transform LimitedTarget => limitedTarget;
    public VerletRope Rope => rope;
    public event Action<bool> ConnectionChanged;

    private void Awake()
    {
        if (socketInteractor == null)
            socketInteractor = GetComponent<XRSocketInteractor>();
    }

    private IEnumerator Start()
    {
        SyncConnectionFromSocket();
        yield return null;
        SyncConnectionFromSocket();
    }

    // Applies the distance limit after other movement systems have updated the target position.
    private void LateUpdate()
    {
        if (!CanLimitDistance())
            return;

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

        heldSelectCount++;
    }

    // Tracks when the hose end is released by a non-socket interactor.
    public void OnRequiredGrabExited(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor)
            return;

        heldSelectCount = Mathf.Max(0, heldSelectCount - 1);
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

    // Replaces the rope reference used to calculate the maximum hose distance.
    public void SetRope(VerletRope targetRope)
    {
        rope = targetRope;
    }

    // Checks whether all required state and references are available before limiting movement.
    private bool CanLimitDistance()
    {
        return isConnected
            && (!requireHeldTarget || IsHeld)
            && hydrantAnchor != null
            && limitedTarget != null
            && rope != null;
    }

    // Clamps the target position so it stays within the allowed hose distance.
    private void LimitDistance()
    {
        float maxDistance = GetMaxDistance();
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

    // Calculates the maximum allowed distance from the rope segment length and slack value.
    private float GetMaxDistance()
    {
        return Mathf.Max(0.0f, rope.constraintDistance * (rope.pointsNb - 1) - slack);
    }

    // Calculates the target offset from the anchor, optionally ignoring vertical movement.
    private Vector3 GetOffsetFromAnchor()
    {
        Vector3 offset = limitedTarget.position - hydrantAnchor.position;

        if (limitHorizontalOnly)
            offset.y = 0.0f;

        return offset;
    }
}
