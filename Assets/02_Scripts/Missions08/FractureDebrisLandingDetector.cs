using UnityEngine;
using VirtualRescue.Destruction;

namespace VirtualRescue.Missions08
{
    [DisallowMultipleComponent]
    public sealed class FractureDebrisLandingDetector : MonoBehaviour
    {
        private LocalizedFracturePiece _piece;
        private Rigidbody _fragmentRigidbody;
        private Collider _fragmentCollider;
        private FractureDebrisCollisionController _controller;
        private bool _hasLanded;

        internal bool IsReleased
        {
            get
            {
                if (_piece != null)
                {
                    return _piece.IsReleased;
                }

                return _fragmentRigidbody != null &&
                    _fragmentRigidbody.constraints == RigidbodyConstraints.None;
            }
        }

        internal Collider FragmentCollider
        {
            get
            {
                return _piece != null
                    ? _piece.FragmentCollider
                    : _fragmentCollider;
            }
        }

        internal void Initialize(
            LocalizedFracturePiece piece,
            FractureDebrisCollisionController controller)
        {
            _piece = piece;
            _fragmentRigidbody = piece != null
                ? piece.GetComponent<Rigidbody>()
                : null;
            _fragmentCollider = piece != null ? piece.FragmentCollider : null;
            _controller = controller;
        }

        internal void Initialize(
            Rigidbody fragmentRigidbody,
            Collider fragmentCollider,
            FractureDebrisCollisionController controller)
        {
            _piece = null;
            _fragmentRigidbody = fragmentRigidbody;
            _fragmentCollider = fragmentCollider;
            _controller = controller;
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryHandleLanding(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            TryHandleLanding(collision);
        }

        private void TryHandleLanding(Collision collision)
        {
            if (_hasLanded ||
                _controller == null ||
                !_controller.IsLandingCollision(this, collision))
            {
                return;
            }

            _hasLanded = true;
            _controller.IgnorePlayerCollisions(this);
        }
    }
}
