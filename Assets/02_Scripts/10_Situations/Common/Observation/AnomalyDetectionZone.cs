using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.Situations.AnomalyObservation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class AnomalyDetectionZone : MonoBehaviour
    {
        [SerializeField] private string _playerTag = "Player";

        private readonly HashSet<Collider> _playerColliders = new();

        public bool IsPlayerInside => _playerColliders.Count > 0;

        private void Awake()
        {
            ConfigurePhysicsComponents();
        }

        private void OnDisable()
        {
            _playerColliders.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayerCollider(other))
            {
                _playerColliders.Add(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            _playerColliders.Remove(other);
        }

        private void OnValidate()
        {
            ConfigurePhysicsComponents();
        }

        public void ResetZone()
        {
            _playerColliders.Clear();
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other == null || string.IsNullOrWhiteSpace(_playerTag))
            {
                return false;
            }

            Transform currentTransform = other.transform;

            while (currentTransform != null)
            {
                if (currentTransform.CompareTag(_playerTag))
                {
                    return true;
                }

                currentTransform = currentTransform.parent;
            }

            return false;
        }

        private void ConfigurePhysicsComponents()
        {
            if (TryGetComponent(out SphereCollider sphereCollider))
            {
                sphereCollider.isTrigger = true;
            }

            if (TryGetComponent(out Rigidbody rigidbodyComponent))
            {
                rigidbodyComponent.isKinematic = true;
                rigidbodyComponent.useGravity = false;
            }
        }

    }
}
