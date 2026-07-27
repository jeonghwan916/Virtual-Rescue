using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.Effects;

namespace VirtualRescue.Missions
{
    public sealed class WindowSeal : MonoBehaviour
    {
        [Serializable]
        private sealed class SealPoint
        {
            [SerializeField] private XRSocketInteractor _socket;
            [SerializeField] private ParticleFadeOut _particle;

            public XRSocketInteractor Socket => _socket;
            public ParticleFadeOut Particle => _particle;
        }

        [Tooltip("각 소켓과 해당 위치에 미리 배치한 파티클을 연결합니다.")]
        [SerializeField] private SealPoint[] _sealPoints = Array.Empty<SealPoint>();

        [Tooltip("활성화하면 완전히 젖은 손수건만 막기 완료로 인정합니다.")]
        [SerializeField] private bool _requireWetHandkerchief;

        private readonly HashSet<HandkerChiefWet> _observedHandkerchiefs = new();
        private bool[] _sealedPoints;
        private bool _isSealed;

        public event Action Sealed;

        public bool IsSealed => _isSealed;

        private void Awake()
        {
            _sealedPoints = new bool[_sealPoints != null ? _sealPoints.Length : 0];
        }

        private void OnEnable()
        {
            SubscribeSocketEvents();
            CheckSealState();
        }

        private void OnDisable()
        {
            UnsubscribeSocketEvents();
            UnsubscribeHandkerchiefEvents();
        }

        private void SubscribeSocketEvents()
        {
            if (_sealPoints == null)
            {
                return;
            }

            foreach (SealPoint sealPoint in _sealPoints)
            {
                if (sealPoint == null || sealPoint.Socket == null)
                {
                    continue;
                }

                sealPoint.Socket.selectEntered.AddListener(HandleSocketSelectionChanged);
            }
        }

        private void UnsubscribeSocketEvents()
        {
            if (_sealPoints == null)
            {
                return;
            }

            foreach (SealPoint sealPoint in _sealPoints)
            {
                if (sealPoint == null || sealPoint.Socket == null)
                {
                    continue;
                }

                sealPoint.Socket.selectEntered.RemoveListener(HandleSocketSelectionChanged);
            }
        }

        private void HandleSocketSelectionChanged(SelectEnterEventArgs args)
        {
            ObserveHandkerchief(args.interactableObject.transform);
            UpdateSealPointEffects();
            CheckSealState();
        }

        private void ObserveHandkerchief(Transform selectedTransform)
        {
            if (!_requireWetHandkerchief ||
                selectedTransform == null)
            {
                return;
            }

            HandkerChiefWet handkerchief =
                selectedTransform.GetComponentInParent<HandkerChiefWet>();

            if (handkerchief == null ||
                !_observedHandkerchiefs.Add(handkerchief))
            {
                return;
            }

            handkerchief.CompletelyWet += HandleHandkerchiefWet;
        }

        private void HandleHandkerchiefWet()
        {
            UpdateSealPointEffects();
            CheckSealState();
        }

        private void UnsubscribeHandkerchiefEvents()
        {
            foreach (HandkerChiefWet handkerchief in _observedHandkerchiefs)
            {
                if (handkerchief != null)
                {
                    handkerchief.CompletelyWet -= HandleHandkerchiefWet;
                }
            }

            _observedHandkerchiefs.Clear();
        }

        private void CheckSealState()
        {
            if (_isSealed)
            {
                return;
            }

            if (!AreAllSealPointsFilled())
            {
                return;
            }

            _isSealed = true;
            Sealed?.Invoke();
        }

        private bool AreAllSealPointsFilled()
        {
            if (_sealPoints == null || _sealPoints.Length == 0)
            {
                return false;
            }

            foreach (SealPoint sealPoint in _sealPoints)
            {
                if (sealPoint == null || sealPoint.Socket == null)
                {
                    Debug.LogWarning("WindowSeal에 비어 있는 막기 지점 또는 소켓 참조가 있습니다.", this);
                    return false;
                }

                if (!IsSocketValid(sealPoint.Socket))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSocketValid(XRSocketInteractor socketPoint)
        {
            if (socketPoint == null || !socketPoint.hasSelection)
            {
                return false;
            }

            if (!_requireWetHandkerchief)
            {
                return true;
            }

            foreach (IXRSelectInteractable selectedInteractable
                     in socketPoint.interactablesSelected)
            {
                HandkerChiefWet handkerchief =
                    selectedInteractable.transform
                        .GetComponentInParent<HandkerChiefWet>();

                if (handkerchief != null &&
                    handkerchief.IsCompletelyWet)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateSealPointEffects()
        {
            if (_sealPoints == null || _sealedPoints == null)
            {
                return;
            }

            int sealPointCount = Mathf.Min(_sealPoints.Length, _sealedPoints.Length);

            for (int i = 0; i < sealPointCount; i++)
            {
                SealPoint sealPoint = _sealPoints[i];
                if (_sealedPoints[i] ||
                    sealPoint == null ||
                    !IsSocketValid(sealPoint.Socket))
                {
                    continue;
                }

                _sealedPoints[i] = true;
                if (sealPoint.Particle != null)
                {
                    sealPoint.Particle.StopImmediately();
                }
            }
        }
    }
}
