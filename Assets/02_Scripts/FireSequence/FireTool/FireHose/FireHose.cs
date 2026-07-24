using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FireHose : FireTool
{
    [Header("Fire Requirements")]
    [SerializeField] private HoseDistanceLimiter _hoseConnectionState;
    [SerializeField] private HoseButton _hoseButton;

    private bool _isActivateHeld;
    private bool _hasButtonPressSignal;

    protected override void OnEnable()
    {
        base.OnEnable();

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
}
