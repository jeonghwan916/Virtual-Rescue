using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class SafetyPinGrabCondition : MonoBehaviour, IXRSelectFilter
{
    [SerializeField] private XRGrabInteractable _extinguisherGrab;
    [SerializeField] private bool _requireBodyTouch;
    [SerializeField] private Collider[] _touchColliders;
    [SerializeField, Min(0f)] private float _touchDistance = 0.1f;

    private readonly List<IXRInteractor> _registeredInteractors = new();

    public bool canProcess => isActiveAndEnabled;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactor is XRSocketInteractor)
            return true;

        if (!_requireBodyTouch)
            return _extinguisherGrab != null && _extinguisherGrab.isSelected;

        if (_extinguisherGrab == null ||
            _extinguisherGrab.isSelected ||
            interactor.handedness == InteractorHandedness.None)
            return false;

        float touchDistanceSqr = _touchDistance * _touchDistance;
        XRInteractionManager interactionManager = _extinguisherGrab.interactionManager;
        if (interactionManager == null)
            return false;

        interactionManager.GetRegisteredInteractors(_registeredInteractors);

        foreach (IXRInteractor registeredInteractor in _registeredInteractors)
        {
            if (registeredInteractor is XRSocketInteractor ||
                registeredInteractor.handedness == InteractorHandedness.None ||
                registeredInteractor.handedness == interactor.handedness)
            {
                continue;
            }

            IReadOnlyList<Collider> touchColliders =
                _touchColliders != null && _touchColliders.Length > 0
                    ? _touchColliders
                    : _extinguisherGrab.colliders;

            if (IsWithinTouchDistance(
                    registeredInteractor.transform.position,
                    touchColliders,
                    touchDistanceSqr))
                return true;
        }

        return false;
    }

    private static bool IsWithinTouchDistance(
        Vector3 position,
        IReadOnlyList<Collider> colliders,
        float touchDistanceSqr)
    {
        foreach (Collider bodyCollider in colliders)
        {
            if (bodyCollider == null || !bodyCollider.enabled)
                continue;

            Vector3 closestPoint = bodyCollider.ClosestPoint(position);
            if ((closestPoint - position).sqrMagnitude <= touchDistanceSqr)
                return true;
        }

        return false;
    }
}
