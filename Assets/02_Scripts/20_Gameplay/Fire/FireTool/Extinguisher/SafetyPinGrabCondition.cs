using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class SafetyPinGrabCondition : MonoBehaviour, IXRSelectFilter
{
    [SerializeField] private XRGrabInteractable _extinguisherGrab;
    [SerializeField, Min(0f)] private float _touchDistance = 0.1f;

    public bool canProcess => isActiveAndEnabled;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactor is XRSocketInteractor)
            return true;

        if (_extinguisherGrab == null ||
            _extinguisherGrab.isSelected ||
            interactor.handedness == InteractorHandedness.None)
            return false;

        float touchDistanceSqr = _touchDistance * _touchDistance;

        foreach (IXRHoverInteractor hoveringInteractor in _extinguisherGrab.interactorsHovering)
        {
            if (hoveringInteractor.handedness == InteractorHandedness.None ||
                hoveringInteractor.handedness == interactor.handedness)
            {
                continue;
            }

            Transform attachTransform = hoveringInteractor.GetAttachTransform(_extinguisherGrab);
            if (attachTransform == null)
                continue;

            foreach (Collider bodyCollider in _extinguisherGrab.colliders)
            {
                if (bodyCollider == null || !bodyCollider.enabled)
                    continue;

                Vector3 closestPoint = bodyCollider.ClosestPoint(attachTransform.position);
                if ((closestPoint - attachTransform.position).sqrMagnitude <=
                    touchDistanceSqr)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
