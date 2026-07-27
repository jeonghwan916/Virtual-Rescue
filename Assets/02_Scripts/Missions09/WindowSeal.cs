using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.Effects;

namespace VirtualRescue.Missions
{
    [RequireComponent(typeof(ParticleFadeOut))]
    public sealed class WindowSeal : MonoBehaviour
    {
        [Tooltip("창문을 막기 위해 모두 채워져야 하는 소켓 목록입니다.")]
        [SerializeField] private XRSocketInteractor[] _socketPoints = new XRSocketInteractor[0];

        private ParticleFadeOut _particleFadeOut;
        private bool _isSealed;

        public event Action Sealed;

        public bool IsSealed => _isSealed;

        private void Awake()
        {
            _particleFadeOut = GetComponent<ParticleFadeOut>();
        }

        private void OnEnable()
        {
            SubscribeSocketEvents();
            CheckSealState();
        }

        private void OnDisable()
        {
            UnsubscribeSocketEvents();
        }

        private void SubscribeSocketEvents()
        {
            if (_socketPoints == null)
            {
                return;
            }

            foreach (XRSocketInteractor socketPoint in _socketPoints)
            {
                if (socketPoint == null)
                {
                    continue;
                }

                socketPoint.selectEntered.AddListener(HandleSocketSelectionChanged);
            }
        }

        private void UnsubscribeSocketEvents()
        {
            if (_socketPoints == null)
            {
                return;
            }

            foreach (XRSocketInteractor socketPoint in _socketPoints)
            {
                if (socketPoint == null)
                {
                    continue;
                }

                socketPoint.selectEntered.RemoveListener(HandleSocketSelectionChanged);
            }
        }

        private void HandleSocketSelectionChanged(SelectEnterEventArgs args)
        {
            CheckSealState();
        }

        private void CheckSealState()
        {
            if (_isSealed)
            {
                return;
            }

            if (!AreAllSocketsFilled())
            {
                return;
            }

            _isSealed = true;
            StartSealEffect();
            Sealed?.Invoke();
        }

        private bool AreAllSocketsFilled()
        {
            if (_socketPoints == null || _socketPoints.Length == 0)
            {
                return false;
            }

            foreach (XRSocketInteractor socketPoint in _socketPoints)
            {
                if (socketPoint == null)
                {
                    Debug.LogWarning("WindowSeal에 비어 있는 소켓 참조가 있습니다.", this);
                    return false;
                }

                if (!socketPoint.hasSelection)
                {
                    return false;
                }
            }

            return true;
        }

        private void StartSealEffect()
        {
            if (_particleFadeOut == null)
            {
                Debug.LogWarning("WindowSeal과 같은 오브젝트에 ParticleFadeOut이 없습니다.", this);
                return;
            }

            _particleFadeOut.FadeOut();
        }
    }
}
