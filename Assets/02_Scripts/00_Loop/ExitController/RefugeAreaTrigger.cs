using System;
using UnityEngine;
using VirtualRescue.GameFlow;
using VirtualRescue.Missions09;

public class RefugeAreaTrigger : MonoBehaviour
{
    [SerializeField] private bool _isEntered = false;
    [SerializeField] private FireExitDoorController _door;
    [SerializeField] private ExitController _exitController;
    
    private void OnEnable()
    {
        _door.Closed += OnRefugeAreaDoorHasClosed;
    }

    private void OnDisable()
    {
        _door.Closed -= OnRefugeAreaDoorHasClosed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isEntered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isEntered = false;
        }
    }

    private void OnRefugeAreaDoorHasClosed()
    {
        if (_isEntered) _exitController.RequestExit();
    }
}
