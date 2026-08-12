using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualRescue.Gameplay.Power
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class SocketCollisionSuppressor : MonoBehaviour
    {
        [SerializeField] private Collider[] _physicalColliders;

        private readonly List<ColliderPair> _ignoredPairs = new();
        private XRGrabInteractable _grabInteractable;
        private Coroutine _restoreRoutine;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();

            if (_physicalColliders == null || _physicalColliders.Length == 0)
            {
                CachePhysicalColliders();
            }
        }

        private void OnEnable()
        {
            if (_grabInteractable == null)
            {
                _grabInteractable = GetComponent<XRGrabInteractable>();
            }

            _grabInteractable.selectEntered.AddListener(HandleSelectEntered);
            _grabInteractable.selectExited.AddListener(HandleSelectExited);
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.RemoveListener(HandleSelectEntered);
                _grabInteractable.selectExited.RemoveListener(HandleSelectExited);
            }

            if (_restoreRoutine != null)
            {
                StopCoroutine(_restoreRoutine);
                _restoreRoutine = null;
            }

            RestoreCollisions();
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is not XRSocketInteractor socket)
            {
                return;
            }

            if (_restoreRoutine != null)
            {
                StopCoroutine(_restoreRoutine);
                _restoreRoutine = null;
            }

            RestoreCollisions();
            SuppressSocketHostCollisions(socket);

            Rigidbody rigidbody = GetComponent<Rigidbody>();

            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor &&
                _ignoredPairs.Count > 0)
            {
                _restoreRoutine = StartCoroutine(
                    RestoreWhenSeparatedFromSocket());
            }
        }

        private void SuppressSocketHostCollisions(XRSocketInteractor socket)
        {
            Transform socketHost = socket.transform.parent;

            if (socketHost == null)
            {
                return;
            }

            Collider[] hostColliders =
                socketHost.GetComponentsInChildren<Collider>(true);

            foreach (Collider ownCollider in _physicalColliders)
            {
                if (!IsPhysicalCollider(ownCollider))
                {
                    continue;
                }

                foreach (Collider hostCollider in hostColliders)
                {
                    if (!IsPhysicalCollider(hostCollider) ||
                        ReferenceEquals(ownCollider, hostCollider))
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(ownCollider, hostCollider, true);
                    _ignoredPairs.Add(
                        new ColliderPair(ownCollider, hostCollider));
                }
            }
        }

        private IEnumerator RestoreWhenSeparatedFromSocket()
        {
            WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

            while (HasOverlappingPair())
            {
                yield return waitForFixedUpdate;
            }

            RestoreCollisions();
            _restoreRoutine = null;
        }

        private bool HasOverlappingPair()
        {
            foreach (ColliderPair pair in _ignoredPairs)
            {
                if (pair.First != null &&
                    pair.Second != null &&
                    pair.First.bounds.Intersects(pair.Second.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void RestoreCollisions()
        {
            foreach (ColliderPair pair in _ignoredPairs)
            {
                if (pair.First != null && pair.Second != null)
                {
                    Physics.IgnoreCollision(
                        pair.First,
                        pair.Second,
                        false);
                }
            }

            _ignoredPairs.Clear();
        }

        private void CachePhysicalColliders()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            List<Collider> physicalColliders = new List<Collider>();

            foreach (Collider collider in colliders)
            {
                if (IsPhysicalCollider(collider))
                {
                    physicalColliders.Add(collider);
                }
            }

            _physicalColliders = physicalColliders.ToArray();
        }

        private static bool IsPhysicalCollider(Collider collider)
        {
            return collider != null && collider.enabled && !collider.isTrigger;
        }

        private readonly struct ColliderPair
        {
            public ColliderPair(Collider first, Collider second)
            {
                First = first;
                Second = second;
            }

            public Collider First { get; }

            public Collider Second { get; }
        }
    }
}
