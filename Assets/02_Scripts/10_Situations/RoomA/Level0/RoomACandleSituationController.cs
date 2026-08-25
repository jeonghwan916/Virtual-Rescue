using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations
{
    public sealed class RoomACandleSituationController
        : SituationController
    {
        [Header("References")]
        [SerializeField]
        private XRSocketInteractor _capSocket;

        [SerializeField]
        private GameObject _fireEffect;

        private bool HasRequiredReferences =>
            _capSocket != null &&
            _fireEffect != null;

        private void Awake()
        {
            if (_capSocket == null)
            {
                Debug.LogError(
                    $"[{name}] Cap Socket is not assigned.",
                    this);
            }

            if (_fireEffect == null)
            {
                Debug.LogError(
                    $"[{name}] Fire Effect is not assigned.",
                    this);
            }
        }

        protected override void OnActivated()
        {
            if (!HasRequiredReferences)
            {
                return;
            }

            _fireEffect.SetActive(true);

            // 재활성화될 때 이벤트가 중복 등록되는 것을 방지한다.
            UnsubscribeFromSocket();
            _capSocket.selectEntered.AddListener(OnCapClosed);
        }

        protected override void OnResolved()
        {
            if (_fireEffect != null)
            {
                _fireEffect.SetActive(false);
            }

            UnsubscribeFromSocket();
        }

        protected override void OnReset()
        {
            if (_fireEffect != null)
            {
                _fireEffect.SetActive(false);
            }

            UnsubscribeFromSocket();
        }

        private void OnCapClosed(SelectEnterEventArgs args)
        {
            if (!IsActive)
            {
                return;
            }

            StageClear();
        }

        private void StageClear()
        {
            ResolveSituation();
        }

        private void OnDisable()
        {
            UnsubscribeFromSocket();
        }

        private void UnsubscribeFromSocket()
        {
            if (_capSocket != null)
            {
                _capSocket.selectEntered.RemoveListener(OnCapClosed);
            }
        }
    }
}