using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

public sealed class SlidingDoorGrabTransformer : XRGeneralGrabTransformer
{
    private enum SlideDirection
    {
        Right,
        Left
    }

    [SerializeField] private Transform axisOrigin;
    [SerializeField] private SlideDirection slideDirection;
    [SerializeField, Min(0f)] private float slideDistance = 1.4f;

    private Vector3 _initialLocalPosition;
    private Quaternion _initialWorldRotation;

    public override void OnLink(XRGrabInteractable grabInteractable)
    {
        base.OnLink(grabInteractable);

        _initialLocalPosition =
            axisOrigin.InverseTransformPoint(grabInteractable.transform.position);

        _initialWorldRotation = grabInteractable.transform.rotation;
    }

    public override void Process(
        XRGrabInteractable grabInteractable,
        XRInteractionUpdateOrder.UpdatePhase updatePhase,
        ref Pose targetPose,
        ref Vector3 localScale)
    {
        base.Process(
            grabInteractable,
            updatePhase,
            ref targetPose,
            ref localScale);

        Vector3 localPosition =
            axisOrigin.InverseTransformPoint(targetPose.position);

        localPosition.x = _initialLocalPosition.x;
        localPosition.y = _initialLocalPosition.y;

        float minZ = slideDirection == SlideDirection.Left ? -slideDistance : 0f;
        float maxZ = slideDirection == SlideDirection.Right ? slideDistance : 0f;
        localPosition.z = Mathf.Clamp(localPosition.z, minZ, maxZ);

        targetPose.position = axisOrigin.TransformPoint(localPosition);
        targetPose.rotation = _initialWorldRotation;
    }
}
