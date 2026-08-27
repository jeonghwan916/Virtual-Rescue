using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public abstract class FireTool : MonoBehaviour
{
    private const float OriginOverlapRadius = 0.01f;
    private const float OriginContainmentTolerance = 0.001f;
    private readonly Collider[] _originOverlapResults = new Collider[8];

    [Header("Particle")]
    [SerializeField] private ParticleSystem _smokeParticle;

    [Header("Raycast")]
    [SerializeField] private Transform _rayOrigin;
    [SerializeField] private float _range = 5f;
    [SerializeField] private LayerMask _fireLayer;

    [Header("Suppressant")]
    [SerializeField] private FireSuppressantType _suppressantType =
        FireSuppressantType.GeneralPurpose;
    [SerializeField] private LayerMask _contactOnlyFireLayer;

    [Header("Operation")]
    [SerializeField] private bool _isOperational = true;

    [Header("Audio Source")]
    [SerializeField] private AudioSource _extinguisherSFX;

    [Header("Grab Interactable")]
    [SerializeField] private XRGrabInteractable _grabInteractable;
    [SerializeField] private bool _isFiring = false;

    protected bool IsFiring => _isFiring;
    protected XRGrabInteractable GrabInteractable => _grabInteractable;
    public bool IsOperational => _isOperational;

    public void SetOperational(bool isOperational)
    {
        _isOperational = isOperational;

        if (!_isOperational)
        {
            StopFiring();
        }
    }

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
        if (!_isOperational)
        {
            return;
        }

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

        int detectionLayerMask =
            _fireLayer.value | _contactOnlyFireLayer.value;

        if (TrySuppressContainingFire(detectionLayerMask, Time.deltaTime))
        {
            return;
        }

        if (Physics.Raycast(
                _rayOrigin.position,
                _rayOrigin.forward,
                out RaycastHit hit,
                _range,
                detectionLayerMask,
                QueryTriggerInteraction.Collide))
        {
            TryApplySuppressant(hit.collider, Time.deltaTime);
        }
    }

    private bool TrySuppressContainingFire(
        int detectionLayerMask,
        float deltaTime)
    {
        Vector3 originPosition = _rayOrigin.position;
        int overlapCount = Physics.OverlapSphereNonAlloc(
            originPosition,
            OriginOverlapRadius,
            _originOverlapResults,
            detectionLayerMask,
            QueryTriggerInteraction.Collide);

        Collider closestCollider = null;
        float closestSqrDistance = float.PositiveInfinity;
        float containmentToleranceSqr =
            OriginContainmentTolerance * OriginContainmentTolerance;

        for (int index = 0; index < overlapCount; index++)
        {
            Collider candidate = _originOverlapResults[index];

            if (candidate == null ||
                candidate.GetComponentInParent<FireObject>() == null)
            {
                continue;
            }

            Vector3 closestPoint = candidate.ClosestPoint(originPosition);

            if ((closestPoint - originPosition).sqrMagnitude >
                containmentToleranceSqr)
            {
                continue;
            }

            float sqrDistance =
                (candidate.bounds.center - originPosition).sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
            {
                continue;
            }

            closestCollider = candidate;
            closestSqrDistance = sqrDistance;
        }

        return TryApplySuppressant(closestCollider, deltaTime);
    }

    private bool TryApplySuppressant(Collider targetCollider, float deltaTime)
    {
        if (targetCollider == null)
        {
            return false;
        }

        FireObject fire = targetCollider.GetComponentInParent<FireObject>();

        if (fire == null)
        {
            return false;
        }

        int hitLayerMask = 1 << targetCollider.gameObject.layer;

        if ((_fireLayer.value & hitLayerMask) != 0)
        {
            fire.TakeExtinguish(_suppressantType, deltaTime);
            return true;
        }

        if ((_contactOnlyFireLayer.value & hitLayerMask) != 0)
        {
            fire.NotifySuppressantContact(_suppressantType);
            return true;
        }

        return false;
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
