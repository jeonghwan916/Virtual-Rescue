using System;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class AshTray : MonoBehaviour
{
    [SerializeField] private BalconyCigaretteSituationController _situationController;
    [SerializeField] private GameObject _cigarette;
    [SerializeField] private VisualEffect _cigaretteEmber;
    [SerializeField] private bool _isFirstEnteredAshtray;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _emberDiableClip;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == _cigarette && !_isFirstEnteredAshtray)
        {
            _isFirstEnteredAshtray = true;
            _situationController.OnCigaretteEnteredAshtray();
            _cigaretteEmber.Stop();
            _audioSource.PlayOneShot(_emberDiableClip);
        }
    }
}
