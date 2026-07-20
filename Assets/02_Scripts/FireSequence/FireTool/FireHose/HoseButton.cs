using System;
using UnityEngine;

public class HoseButton : MonoBehaviour
{
    public event Action OnButtonPressed;

    // todo : Temporary - Init
    private void Start()
    {
        Press();
    }

    public void Press()
    {
        Debug.Log($"{nameof(HoseButton)} pressed.", this);
        OnButtonPressed?.Invoke();
    }
}
