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
    Rigidbody _nozzleRigidBody;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        _nozzleRigidBody = _nozzleGrabInteractable.transform.GetComponent<Rigidbody>();
        
        _safetyPinSocket.selectExited.AddListener(PulledOffSafetyPin);
        _nozzleGrabInteractable.selectEntered.AddListener(GrabbedNozzle);
        _nozzleGrabInteractable.selectExited.AddListener(ReleaseNozzle);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        _safetyPinSocket.selectExited.RemoveAllListeners();
        _nozzleGrabInteractable.selectExited.RemoveAllListeners();
        _nozzleGrabInteractable.selectExited.RemoveAllListeners();
    }

    protected override bool CanStartFiring()
    {
        return _isSafetyPinHasPulledOff;
    }

    private void PulledOffSafetyPin(SelectExitEventArgs args)
    {
        _isSafetyPinHasPulledOff = true;

        if (_safetyPinSocket != null) _safetyPinSocket.gameObject.SetActive(false);
    }

    private void GrabbedNozzle(SelectEnterEventArgs args)
    {
        _nozzleRigidBody.isKinematic = false;
    }
    
    private void ReleaseNozzle(SelectExitEventArgs args)
    {
        _nozzleRigidBody.isKinematic = false;
    }
}
