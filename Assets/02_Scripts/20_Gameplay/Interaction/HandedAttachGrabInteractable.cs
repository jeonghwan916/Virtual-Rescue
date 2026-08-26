using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class HandedAttachGrabInteractable : XRGrabInteractable
{
    [SerializeField] private Transform _leftAttachTransform;
    [SerializeField] private Transform _rightAttachTransform;

    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        if (interactor != null)
        {
            if (interactor.handedness == InteractorHandedness.Left &&
                _leftAttachTransform != null)
            {
                return _leftAttachTransform;
            }

            if (interactor.handedness == InteractorHandedness.Right &&
                _rightAttachTransform != null)
            {
                return _rightAttachTransform;
            }
        }

        return base.GetAttachTransform(interactor);
    }
}
