using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public abstract class FireTool : MonoBehaviour
{
    [Header("Particle")]
    [SerializeField] private ParticleSystem _smokeParticle;

    [Header("Raycast")]
    [SerializeField] private Transform _rayOrigin;
    [SerializeField] private float _range = 5f;
    [SerializeField] private LayerMask _fireLayer;

    [Header("Suppressant")]
    [SerializeField] private FireSuppressantType _suppressantType =
        FireSuppressantType.GeneralPurpose;

    [Header("Audio Source")]
    [SerializeField] private AudioSource _extinguisherSFX;

    [Header("Grab Interactable")]
    [SerializeField] private XRGrabInteractable _grabInteractable;
    [SerializeField] private bool _isFiring = false;

    protected bool IsFiring => _isFiring;
    protected XRGrabInteractable GrabInteractable => _grabInteractable;

    protected virtual void Awake()
    {
        if (_rayOrigin == null)
            _rayOrigin = transform;
    }

    protected virtual void OnEnable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.activated.AddListener(OnFireStart);
            _grabInteractable.deactivated.AddListener(OnFireEnd);
        }
    }

    protected virtual void OnDisable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.activated.RemoveListener(OnFireStart);
            _grabInteractable.deactivated.RemoveListener(OnFireEnd);
        }

        StopFiring();
    }

    protected virtual void OnFireStart(ActivateEventArgs args)
    {
        TryStartFiring();
    }

    protected virtual void OnFireEnd(DeactivateEventArgs args)
    {
        StopFiring();
    }

    protected void TryStartFiring()
    {
        if (_isFiring)
            return;

        if (!CanStartFiring())
            return;

        if (_smokeParticle != null)
            _smokeParticle.Play();

        if (_extinguisherSFX != null && !_extinguisherSFX.isPlaying)
            _extinguisherSFX.Play();

        _isFiring = true;
        OnFiringStarted();
    }

    protected void StopFiring()
    {
        bool wasFiring = _isFiring;

        if (_smokeParticle != null && _isFiring)
            _smokeParticle.Stop();

        if (_extinguisherSFX != null)
            _extinguisherSFX.Stop();

        _isFiring = false;

        if (wasFiring)
            OnFiringStopped();
    }

    protected virtual bool CanStartFiring()
    {
        return true;
    }

    protected virtual void OnFiringStarted()
    {
    }

    protected virtual void OnFiringStopped()
    {
    }

    private void Update()
    {
        if (_rayOrigin == null)
            return;

        Debug.DrawRay(_rayOrigin.position, _rayOrigin.forward * _range, Color.red);

        if (!_isFiring)
            return;

        if (Physics.Raycast(_rayOrigin.position, _rayOrigin.forward, out RaycastHit hit, _range, _fireLayer, QueryTriggerInteraction.Collide))
        {
            FireObject fire = hit.collider.GetComponentInParent<FireObject>();

            if (fire != null)
            {
                fire.TakeExtinguish(_suppressantType, Time.deltaTime);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = _rayOrigin != null ? _rayOrigin : transform;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin.position, origin.position + origin.forward * _range);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin.position, 0.05f);
        Gizmos.DrawWireSphere(origin.position + origin.forward * _range, 0.08f);
    }
}
