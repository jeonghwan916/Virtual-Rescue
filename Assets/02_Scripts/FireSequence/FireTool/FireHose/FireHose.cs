using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FireHose : FireTool
{
    [Header("Fire Requirements")]
    [SerializeField] private HoseDistanceLimiter _hoseConnectionState;
    [SerializeField] private HoseButton _hoseButton;

    private bool _isActivateHeld;
    private bool _hasButtonPressSignal;

    public event Action Grabbed;
    public event Action FiringStarted;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (GrabInteractable != null)
        {
            GrabInteractable.selectEntered.AddListener(HandleGrabbed);
        }

        if (_hoseButton != null)
        {
            _hoseButton.OnButtonPressed += OnHoseButtonPressed;
            _hoseButton.OnButtonUnPressed += OnHoseButtonUnPressed;
        }

        if (_hoseConnectionState != null)
            _hoseConnectionState.ConnectionChanged += OnHoseConnectionChanged;
    }

    protected override void OnDisable()
    {
        if (GrabInteractable != null)
        {
            GrabInteractable.selectEntered.RemoveListener(HandleGrabbed);
        }

        if (_hoseButton != null)
        {
            _hoseButton.OnButtonPressed -= OnHoseButtonPressed;
            _hoseButton.OnButtonUnPressed -= OnHoseButtonUnPressed;
        }

        if (_hoseConnectionState != null)
            _hoseConnectionState.ConnectionChanged -= OnHoseConnectionChanged;

        _isActivateHeld = false;
        _hasButtonPressSignal = false;

        base.OnDisable();
    }

    private void HandleGrabbed(SelectEnterEventArgs args)
    {
        Grabbed?.Invoke();
    }

    protected override void OnFireStart(ActivateEventArgs args)
    {
        _isActivateHeld = true;
        TryStartFiring();
    }

    protected override void OnFireEnd(DeactivateEventArgs args)
    {
        _isActivateHeld = false;
        StopFiring();
    }

    private void OnHoseButtonPressed()
    {
        _hasButtonPressSignal = true;
        TryStartFiring();
    }

    private void OnHoseButtonUnPressed()
    {
        _hasButtonPressSignal = false;
        StopFiring();
    }

    private void OnHoseConnectionChanged(bool connected)
    {
        if (connected)
            TryStartFiring();
        else
            StopFiring();
    }

    protected override bool CanStartFiring()
    {
        return _isActivateHeld
            && _hasButtonPressSignal
            && _hoseConnectionState != null
            && _hoseConnectionState.IsConnected;
    }

    protected override void OnFiringStarted()
    {
        FiringStarted?.Invoke();
    }
}
