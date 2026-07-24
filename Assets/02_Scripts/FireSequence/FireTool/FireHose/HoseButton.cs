using System;
using UnityEngine;

public class HoseButton : MonoBehaviour
{
    [SerializeField] private bool _isLeverEnable = false;
    
    public event Action OnButtonPressed;
    public event Action OnButtonUnPressed;

    // todo : Temporary - Init
    private void Start()
    {
        LeverEnabled();
    }

    public void LeverEnabled()
    {
        Debug.Log($"{nameof(HoseButton)} enabled.", this);
        _isLeverEnable = true;
        OnButtonPressed?.Invoke();
    }

    public void LeverDisabled()
    {
        Debug.Log($"{nameof(HoseButton)} disabled.", this);
        _isLeverEnable = false;
        OnButtonUnPressed?.Invoke();
    }
}
