using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class AshTray : MonoBehaviour
{
    [SerializeField] private BalconyCigaretteSituationController _situationController;
    [SerializeField] private GameObject _cigarette;
    [SerializeField] private bool _entered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == _cigarette)
        {
            _entered = true;
            _situationController.OnCigaretteEnteredAshtray();
            other.GetComponent<XRGrabInteractable>().enabled = false;
        }
    }

    /*
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == _cigarette)
        {
            _entered = false;
            _situationController.OnCigaretteExitedAshtray();
        }
    }
    */
}
