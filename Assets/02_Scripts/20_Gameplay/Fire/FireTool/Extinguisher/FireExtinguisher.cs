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
    [SerializeField] private Collider _nozzleGrabPointCollider;
    [SerializeField] private Collider[] _bodyColliders;
    private Rigidbody _nozzleRigidBody;

    [Header("Handle")]
    [SerializeField] private Transform _handle;
    [SerializeField] private float _handlePressedXAngle = -15f;
    [SerializeField] private float _handleRotationSpeed = 180f;
    private Quaternion _handleInitialLocalRotation;

    public event Action Grabbed;
    public event Action SafetyPinPulled;

    protected override void Awake()
    {
        base.Awake();

        if (_handle != null)
        {
            _handleInitialLocalRotation = _handle.localRotation;
        }

        IgnoreInternalNozzleCollisions();
    }

    private void LateUpdate()
    {
        UpdateHandleRotation();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        IgnoreInternalNozzleCollisions();

        if (GrabInteractable != null)
        {
            GrabInteractable.selectEntered.AddListener(HandleGrabbed);
            GrabInteractable.selectExited.AddListener(HandleReleased);
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
            GrabInteractable.selectExited.RemoveListener(HandleReleased);
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

    private void HandleReleased(SelectExitEventArgs args)
    {
        StopFiring();
    }

    private void UpdateHandleRotation()
    {
        if (_handle == null)
        {
            return;
        }

        Quaternion targetRotation = IsFiring
            ? _handleInitialLocalRotation * Quaternion.Euler(_handlePressedXAngle, 0f, 0f)
            : _handleInitialLocalRotation;

        _handle.localRotation = Quaternion.RotateTowards(
            _handle.localRotation,
            targetRotation,
            _handleRotationSpeed * Time.deltaTime);
    }

    private void IgnoreInternalNozzleCollisions()
    {
        if (_nozzleGrabPointCollider == null || _bodyColliders == null)
        {
            return;
        }

        foreach (Collider bodyCollider in _bodyColliders)
        {
            if (bodyCollider == null)
            {
                continue;
            }

            Physics.IgnoreCollision(_nozzleGrabPointCollider, bodyCollider, true);
        }
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
