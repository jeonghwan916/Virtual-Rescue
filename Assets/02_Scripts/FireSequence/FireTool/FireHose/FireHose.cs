using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class FireHose : FireTool
{
    [Header("Pipe Twist")]
    [SerializeField] private Transform _pipe;
    [SerializeField] private float _pipeMaxAngle = 90f;
    [SerializeField] private float _fireThresholdAngle = 45f;
    [SerializeField] private bool _invertTwistDirection = false;

    [Header("Valve Requirement")]
    [SerializeField] private FireHoseValveLever _valveLever;

    private readonly List<IXRSelectInteractor> _selectingInteractors = new();

    private IXRSelectInteractor _secondaryInteractor;
    private Transform _secondaryInteractorTransform;
    private Quaternion _secondaryInitialRotation;
    private Quaternion _pipeInitialLocalRotation;
    private float _secondaryInitialPipeAngle;
    private float _pipeAngle;

    public event Action Grabbed;
    public event Action FiringStarted;

    protected override void Awake()
    {
        base.Awake();

        if (_pipe != null)
            _pipeInitialLocalRotation = _pipe.localRotation;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (GrabInteractable != null)
        {
            GrabInteractable.selectEntered.AddListener(HandleGrabbed);
            GrabInteractable.selectExited.AddListener(HandleReleased);
        }

    }

    protected override void OnDisable()
    {
        if (GrabInteractable != null)
        {
            GrabInteractable.selectEntered.RemoveListener(HandleGrabbed);
            GrabInteractable.selectExited.RemoveListener(HandleReleased);
        }

        ClearGrabState();

        base.OnDisable();
    }

    private void HandleGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject != null && !_selectingInteractors.Contains(args.interactorObject))
        {
            _selectingInteractors.Add(args.interactorObject);

            if (_selectingInteractors.Count == 2)
                SetSecondaryInteractor(args.interactorObject, args);
        }

        Grabbed?.Invoke();
    }

    private void HandleReleased(SelectExitEventArgs args)
    {
        if (args.interactorObject == null)
            return;

        bool releasedSecondary = ReferenceEquals(_secondaryInteractor, args.interactorObject);

        _selectingInteractors.Remove(args.interactorObject);

        if (releasedSecondary || _selectingInteractors.Count < 2)
            ClearSecondaryInteractor();
    }

    protected override void OnFireStart(ActivateEventArgs args)
    {
    }

    protected override void OnFireEnd(DeactivateEventArgs args)
    {
    }

    private void LateUpdate()
    {
        UpdatePipeTwist();
        EvaluateFiringState();
    }

    private void SetSecondaryInteractor(IXRSelectInteractor interactor, SelectEnterEventArgs args)
    {
        _secondaryInteractor = interactor;
        _secondaryInteractorTransform = interactor.GetAttachTransform(args.interactableObject);

        if (_secondaryInteractorTransform == null)
            _secondaryInteractorTransform = interactor.transform;

        if (_secondaryInteractorTransform != null)
            _secondaryInitialRotation = _secondaryInteractorTransform.rotation;

        _secondaryInitialPipeAngle = _pipeAngle;
    }

    private void ClearGrabState()
    {
        _selectingInteractors.Clear();
        ClearSecondaryInteractor();
    }

    private void ClearSecondaryInteractor()
    {
        _secondaryInteractor = null;
        _secondaryInteractorTransform = null;
    }

    private void UpdatePipeTwist()
    {
        if (_secondaryInteractorTransform == null)
            return;

        Vector3 twistAxis = _pipe != null ? _pipe.TransformDirection(Vector3.up) : transform.up;
        Quaternion rotationDelta = _secondaryInteractorTransform.rotation * Quaternion.Inverse(_secondaryInitialRotation);
        rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f)
            angle -= 360f;

        float direction = Vector3.Dot(axis, twistAxis) >= 0f ? 1f : -1f;
        float signedAngle = angle * direction;

        if (_invertTwistDirection)
            signedAngle = -signedAngle;

        SetPipeAngle(_secondaryInitialPipeAngle + signedAngle);
    }

    private void SetPipeAngle(float angle)
    {
        _pipeAngle = Mathf.Clamp(angle, 0f, _pipeMaxAngle);

        if (_pipe == null)
            return;

        _pipe.localRotation = _pipeInitialLocalRotation * Quaternion.AngleAxis(_pipeAngle, Vector3.up);
    }

    private void EvaluateFiringState()
    {
        if (CanStartFiring())
            TryStartFiring();
        else if (IsFiring)
            StopFiring();
    }

    protected override bool CanStartFiring()
    {
        return _pipeAngle >= _fireThresholdAngle
            && _valveLever != null
            && _valveLever.IsLeverEnabled;
    }

    protected override void OnFiringStarted()
    {
        FiringStarted?.Invoke();
    }

    private void OnValidate()
    {
        _pipeMaxAngle = Mathf.Max(0f, _pipeMaxAngle);
        _fireThresholdAngle = Mathf.Clamp(_fireThresholdAngle, 0f, _pipeMaxAngle);
    }
}
