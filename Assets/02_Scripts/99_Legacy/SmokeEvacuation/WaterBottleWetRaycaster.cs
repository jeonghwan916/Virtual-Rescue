using UnityEngine;

public class WaterBottleWetRaycaster : MonoBehaviour
{
    [SerializeField] private Transform _rayOrigin;
    [SerializeField] private LayerMask _clothLayer;
    [SerializeField] private float _rayRange = 5f;
    [SerializeField, Range(0f, 180f)] private float _pourAngleThreshold = 60f;
    [SerializeField] private float _wetBlueSpeed = 0.25f;

    private float _pourDotThreshold;

    private void Awake()
    {
        if (_rayOrigin == null)
            _rayOrigin = transform;

        _pourDotThreshold = Mathf.Cos(_pourAngleThreshold * Mathf.Deg2Rad);
    }

    private void Update()
    {
        if (_rayOrigin == null || !IsPouring())
            return;

        Debug.DrawRay(_rayOrigin.position, _rayOrigin.forward * _rayRange, Color.blue);

        if (Physics.Raycast(_rayOrigin.position, _rayOrigin.forward, out RaycastHit hit, _rayRange, _clothLayer, QueryTriggerInteraction.Collide))
        {
            HandkerChiefWet wetTarget = hit.collider.GetComponentInParent<HandkerChiefWet>();

            if (wetTarget != null)
                wetTarget.ApplyWet(Time.deltaTime * _wetBlueSpeed);
        }
    }

    private bool IsPouring()
    {
        return Vector3.Dot(_rayOrigin.forward, Vector3.down) >= _pourDotThreshold;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = _rayOrigin != null ? _rayOrigin : transform;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin.position, origin.position + origin.forward * _rayRange);
        Gizmos.DrawWireSphere(origin.position, 0.05f);
        Gizmos.DrawWireSphere(origin.position + origin.forward * _rayRange, 0.08f);
    }
}
