using System;
using UnityEngine;

public class VignetteTrigger : MonoBehaviour
{
    [SerializeField] private VignetteController _vignetteController;
    [SerializeField, Range(0f, 1f)] float _startApertureSize = 0.5f;
    [SerializeField] private bool _hasTriggerEntered = false;
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_hasTriggerEntered)
        {
            _hasTriggerEntered = true;
            _vignetteController.WipeIn();
        }
    }
}
