using UnityEngine;
using VirtualRescue.Destruction;

namespace VirtualRescue.Missions08
{
    public sealed class FractureDebrisCollisionController : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private CharacterController _playerController;

        [Header("Landing")]
        [SerializeField] private LayerMask _landingLayers = ~0;
        [SerializeField, Range(0f, 1f)] private float _minimumUpwardNormal = 0.5f;

        private Collider[] _playerColliders;

        private void Awake()
        {
            if (_playerController == null)
            {
                _playerController = FindFirstObjectByType<CharacterController>();
            }

            CachePlayerColliders();
            AttachLandingDetectors();
        }

        internal bool IsLandingCollision(
            LocalizedFracturePiece piece,
            Collision collision)
        {
            if (piece == null ||
                !piece.IsReleased ||
                collision == null ||
                collision.rigidbody != null ||
                IsPlayerObject(collision.gameObject) ||
                !IsLandingLayer(collision.gameObject.layer))
            {
                return false;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                if (Vector3.Dot(contact.normal, Vector3.up) >= _minimumUpwardNormal)
                {
                    return true;
                }
            }

            return false;
        }

        internal void IgnorePlayerCollisions(LocalizedFracturePiece piece)
        {
            Collider fragmentCollider = piece != null ? piece.FragmentCollider : null;
            if (fragmentCollider == null || _playerColliders == null)
            {
                return;
            }

            foreach (Collider playerCollider in _playerColliders)
            {
                if (playerCollider != null && playerCollider != fragmentCollider)
                {
                    Physics.IgnoreCollision(fragmentCollider, playerCollider, true);
                }
            }
        }

        private void CachePlayerColliders()
        {
            if (_playerController == null)
            {
                Debug.LogWarning("파괴 잔해 충돌 관리자에서 플레이어 CharacterController를 찾을 수 없습니다.", this);
                _playerColliders = System.Array.Empty<Collider>();
                return;
            }

            Transform playerRoot = _playerController.transform.root;
            _playerColliders = playerRoot.GetComponentsInChildren<Collider>(true);
        }

        private void AttachLandingDetectors()
        {
            LocalizedFracturePiece[] pieces =
                GetComponentsInChildren<LocalizedFracturePiece>(true);

            foreach (LocalizedFracturePiece piece in pieces)
            {
                FractureDebrisLandingDetector detector =
                    piece.GetComponent<FractureDebrisLandingDetector>();

                if (detector == null)
                {
                    detector = piece.gameObject.AddComponent<FractureDebrisLandingDetector>();
                }

                detector.Initialize(piece, this);
            }
        }

        private bool IsLandingLayer(int layer)
        {
            return (_landingLayers.value & (1 << layer)) != 0;
        }

        private bool IsPlayerObject(GameObject collisionObject)
        {
            if (_playerController == null || collisionObject == null)
            {
                return false;
            }

            return collisionObject.transform.IsChildOf(_playerController.transform.root);
        }
    }
}
