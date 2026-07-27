using UnityEngine;
using VirtualRescue.Destruction;

namespace VirtualRescue.Missions08
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LocalizedFracturePiece))]
    public sealed class FractureDebrisLandingDetector : MonoBehaviour
    {
        private LocalizedFracturePiece _piece;
        private FractureDebrisCollisionController _controller;
        private bool _hasLanded;

        internal void Initialize(
            LocalizedFracturePiece piece,
            FractureDebrisCollisionController controller)
        {
            _piece = piece;
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
                !_controller.IsLandingCollision(_piece, collision))
            {
                return;
            }

            _hasLanded = true;
            _controller.IgnorePlayerCollisions(_piece);
        }
    }
}
