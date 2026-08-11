using System;
using UnityEngine;
using VirtualRescue.GameFlow;

public class EmergencyStairTrigger : MonoBehaviour
{
    [SerializeField] private ExitController _exitController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _exitController.RequestExit();
        }
    }
}
