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

        [Tooltip("완전히 젖은 손수건만 소켓에 고정합니다.")]
        [SerializeField] private bool _requireWetHandkerchief;

        [Header("Valid Hover Haptic")]
        [SerializeField, Range(0f, 1f)] private float _hapticAmplitude = 0.35f;
        [SerializeField, Min(0.01f)] private float _hapticDuration = 0.08f;

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
            if (_socketInteractor == null)
            {
                _socketInteractor = GetComponent<XRSocketInteractor>();
            }

            _socketInteractor.selectFilters.Add(this);
            _socketInteractor.selectEntered.AddListener(HandleSelectEntered);
            _socketInteractor.hoverEntered.AddListener(HandleHoverEntered);
        }

        private void OnDisable()
        {
            _socketInteractor.selectEntered.RemoveListener(HandleSelectEntered);
            _socketInteractor.hoverEntered.RemoveListener(HandleHoverEntered);
            _socketInteractor.selectFilters.Remove(this);

            if (_lockedInteractable != null)
            {
                _lockedInteractable.selectFilters.Remove(this);
                _lockedInteractable = null;
            }

            _isLocked = false;
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

            if (_requireWetHandkerchief &&
                !IsCompletelyWetHandkerchief(grabInteractable))
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

        private void HandleHoverEntered(HoverEnterEventArgs args)
        {
            if (_isLocked ||
                args.interactableObject is not IXRSelectInteractable interactable ||
                !IsCompletelyWetHandkerchief(interactable))
            {
                return;
            }

            foreach (IXRSelectInteractor selectingInteractor in interactable.interactorsSelecting)
            {
                if (ReferenceEquals(selectingInteractor, _socketInteractor))
                {
                    continue;
                }

                if (selectingInteractor is XRBaseInputInteractor inputInteractor)
                {
                    inputInteractor.SendHapticImpulse(
                        _hapticAmplitude,
                        _hapticDuration);
                    return;
                }
            }
        }

        private static bool IsCompletelyWetHandkerchief(
            XRGrabInteractable grabInteractable)
        {
            HandkerChiefWet handkerchief =
                grabInteractable.GetComponentInParent<HandkerChiefWet>();

            return handkerchief != null &&
                   handkerchief.IsCompletelyWet;
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (ReferenceEquals(interactor, _socketInteractor))
            {
                if (_lockOnlyOnce &&
                    _isLocked &&
                    !ReferenceEquals(interactable, _lockedInteractable))
                {
                    return false;
                }

                if (_requireWetHandkerchief &&
                    !IsCompletelyWetHandkerchief(interactable))
                {
                    return false;
                }

                return true;
            }

            if (!_isLocked ||
                !ReferenceEquals(interactable, _lockedInteractable))
            {
                return true;
            }

            return false;
        }

        private static bool IsCompletelyWetHandkerchief(
            IXRSelectInteractable interactable)
        {
            HandkerChiefWet handkerchief =
                interactable.transform.GetComponentInParent<HandkerChiefWet>();

            return handkerchief != null &&
                   handkerchief.IsCompletelyWet;
        }

        private void OnValidate()
        {
            _hapticAmplitude = Mathf.Clamp01(_hapticAmplitude);
            _hapticDuration = Mathf.Max(0.01f, _hapticDuration);
        }
    }
}
