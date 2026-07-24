using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class FireExtinguisher : FireTool
{
    [Header("SafetyPin")]
    [SerializeField] private bool _isSafetyPinHasPulledOff = false;
    [SerializeField] private XRSocketInteractor _safetyPinSocket;
    
    [Header("Nozzle")]
    [SerializeField] private XRGrabInteractable _nozzleGrabInteractable;
    private Rigidbody _nozzleRigidBody;

    public event Action Grabbed;
    public event Action SafetyPinPulled;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (GrabInteractable != null)
        {
            GrabInteractable.selectEntered.AddListener(HandleGrabbed);
        }

        if (_safetyPinSocket != null)
        {
            _safetyPinSocket.selectExited.AddListener(PulledOffSafetyPin);
        }

        if (_nozzleGrabInteractable != null)
        {
            _nozzleRigidBody = _nozzleGrabInteractable.GetComponent<Rigidbody>();
            _nozzleGrabInteractable.selectEntered.AddListener(GrabbedNozzle);
            _nozzleGrabInteractable.selectExited.AddListener(ReleaseNozzle);
        }
    }

    protected override void OnDisable()
    {
        if (GrabInteractable != null)
        {
            GrabInteractable.selectEntered.RemoveListener(HandleGrabbed);
        }

        if (_safetyPinSocket != null)
        {
            _safetyPinSocket.selectExited.RemoveListener(PulledOffSafetyPin);
        }

        if (_nozzleGrabInteractable != null)
        {
            _nozzleGrabInteractable.selectEntered.RemoveListener(GrabbedNozzle);
            _nozzleGrabInteractable.selectExited.RemoveListener(ReleaseNozzle);
        }

        base.OnDisable();
    }

    protected override bool CanStartFiring()
    {
        return _isSafetyPinHasPulledOff;
    }

    private void PulledOffSafetyPin(SelectExitEventArgs args)
    {
        if (_isSafetyPinHasPulledOff)
        {
            return;
        }

        _isSafetyPinHasPulledOff = true;
        SafetyPinPulled?.Invoke();

        if (_safetyPinSocket != null)
        {
            _safetyPinSocket.gameObject.SetActive(false);
        }
    }

    private void HandleGrabbed(SelectEnterEventArgs args)
    {
        Grabbed?.Invoke();
    }

    private void GrabbedNozzle(SelectEnterEventArgs args)
    {
        if (_nozzleRigidBody != null)
        {
            _nozzleRigidBody.isKinematic = false;
        }
    }
    
    private void ReleaseNozzle(SelectExitEventArgs args)
    {
        if (_nozzleRigidBody != null)
        {
            _nozzleRigidBody.isKinematic = false;
        }
    }
}
