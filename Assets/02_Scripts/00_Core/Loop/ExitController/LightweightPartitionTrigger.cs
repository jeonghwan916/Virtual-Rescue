using System;
using UnityEngine;
using VirtualRescue.GameFlow;

public class LightweightPartitionTrigger : MonoBehaviour
{
    [SerializeField] private ExitController _exitController;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            _exitController.RequestExit();
        }
    }
}
