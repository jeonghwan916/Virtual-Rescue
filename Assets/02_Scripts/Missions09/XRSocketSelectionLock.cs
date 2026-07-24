using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualRescue.Interaction
{
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class XRSocketSelectionLock : MonoBehaviour, IXRSelectFilter
    {
        [Tooltip("한 번 잠긴 뒤에는 추가 선택 이벤트를 무시합니다.")]
        [SerializeField] private bool _lockOnlyOnce = true;

        private XRSocketInteractor _socketInteractor;
        private XRGrabInteractable _lockedInteractable;
        private bool _isLocked;

        public bool canProcess => isActiveAndEnabled;

        private void Awake()
        {
            _socketInteractor = GetComponent<XRSocketInteractor>();
        }

        private void OnEnable()
        {
            _socketInteractor.selectEntered.AddListener(HandleSelectEntered);
        }

        private void OnDisable()
        {
            _socketInteractor.selectEntered.RemoveListener(HandleSelectEntered);

            if (_lockedInteractable != null)
            {
                _lockedInteractable.selectFilters.Remove(this);
                _lockedInteractable = null;
            }
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (_lockOnlyOnce && _isLocked)
            {
                return;
            }

            if (args.interactableObject is not XRGrabInteractable grabInteractable)
            {
                return;
            }

            // XRGrabInteractable을 비활성화하면 현재 소켓 선택까지 해제될 수 있습니다.
            // 컴포넌트는 유지하고 SelectFilter로 다른 손/레이 인터랙터의 재선택만 막습니다.
            grabInteractable.throwOnDetach = false;
            grabInteractable.forceGravityOnDetach = false;
            grabInteractable.selectFilters.Add(this);

            _lockedInteractable = grabInteractable;
            _isLocked = true;
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            // 잠기기 전에는 기존 XRI 선택 규칙을 그대로 따릅니다.
            if (!_isLocked)
            {
                return true;
            }

            // 이 필터가 잠근 오브젝트가 아니면 다른 상호작용에 영향을 주지 않습니다.
            if (!ReferenceEquals(interactable, _lockedInteractable))
            {
                return true;
            }

            // 잠긴 오브젝트는 현재 소켓만 계속 선택할 수 있고, 손/레이는 다시 선택할 수 없습니다.
            return ReferenceEquals(interactor, _socketInteractor);
        }
    }
}
