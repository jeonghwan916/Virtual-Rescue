using UnityEngine;
using VirtualRescue.Destruction;

namespace VirtualRescue.Missions08
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LocalizedFracturePiece))]
    public sealed class DestructionBatImpactReceiver : MonoBehaviour
    {
        private const string DestructionToolTag = "DestructionTool";

        private LocalizedFracturePiece _piece;

        private void Awake()
        {
            _piece = GetComponent<LocalizedFracturePiece>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_piece == null || _piece.IsReleased || !HasDestructionToolTag(collision.collider))
            {
                return;
            }

            Vector3 impactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;
            Vector3 impactDirection = collision.relativeVelocity.sqrMagnitude > 0.0001f
                ? collision.relativeVelocity.normalized
                : -transform.forward;

            _piece.Release(impactDirection * 0.02f, impactPoint);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_piece == null || _piece.IsReleased || !HasDestructionToolTag(other))
            {
                return;
            }

            _piece.Release(-transform.forward * 0.02f, transform.position);
        }

        private static bool HasDestructionToolTag(Component component)
        {
            for (Transform current = component != null ? component.transform : null;
                 current != null;
                 current = current.parent)
            {
                if (current.CompareTag(DestructionToolTag))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
