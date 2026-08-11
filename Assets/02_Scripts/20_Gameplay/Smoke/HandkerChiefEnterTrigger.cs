using System;
using System.Collections.Generic;
using UnityEngine;

public class HandkerChiefEnterTrigger : MonoBehaviour
{
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private VignetteController _vignetteController;
    [SerializeField] private string _smokeTag = "SmokeZone";
    [SerializeField, Range(0f, 1f)] private float _smokeApertureSize = 0.85f;

    private readonly HashSet<Collider> _activeSmokeColliders = new();
    private bool _hasAppliedWetHandkerchief;

    public event Action WetHandkerchiefApplied;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_smokeTag))
        {
            bool wasOutsideSmoke = _activeSmokeColliders.Count == 0;
            _activeSmokeColliders.Add(other);

            if (wasOutsideSmoke && _vignetteController != null)
            {
                _vignetteController.SetApertureSize(_smokeApertureSize);
            }

            return;
        }

        if (_hasAppliedWetHandkerchief ||
            ((1 << other.gameObject.layer) & _targetLayer) == 0)
        {
            return;
        }

        HandkerChiefWet wetHandkerchief = other.GetComponentInParent<HandkerChiefWet>();
        if (wetHandkerchief == null || !wetHandkerchief.IsCompletelyWet)
        {
            return;
        }

        _hasAppliedWetHandkerchief = true;

        if (_activeSmokeColliders.Count == 0 && _vignetteController != null)
        {
            _vignetteController.WipeOut();
        }

        WetHandkerchiefApplied?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(_smokeTag) || !_activeSmokeColliders.Remove(other))
        {
            return;
        }

        if (_activeSmokeColliders.Count == 0 && _vignetteController != null)
        {
            _vignetteController.WipeOut();
        }
    }

    private void OnDisable()
    {
        _activeSmokeColliders.Clear();

        if (_vignetteController != null)
        {
            _vignetteController.SetApertureSize(1f);
        }
    }
}
