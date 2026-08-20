using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace VirtualRescue.GameFlow
{
    public enum CellPhoneContact
    {
        Emergency119,
        Management
    }

    [DisallowMultipleComponent]
    public sealed class CellPhoneScreen : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private XRGrabInteractable _grabInteractable;
        [SerializeField] private LayerMask _touchLayerMask = 1 << 12;
        [SerializeField] private float _touchDebounceTime = 0.15f;

        [Header("Panels")]
        [SerializeField] private GameObject _panel119;
        [SerializeField] private GameObject _panelManagement;

        [Header("Call Touch Areas")]
        [SerializeField] private BoxCollider _call119TouchArea;
        [SerializeField] private BoxCollider _callManagementTouchArea;

        [Header("Haptics")]
        [SerializeField] private HapticImpulsePlayer _hapticPlayer;
        [SerializeField] private float _hapticAmplitude = 0.3f;
        [SerializeField] private float _hapticDuration = 0.05f;

        private readonly Collider[] _touchHits = new Collider[8];
        private bool _isTouchActive;
        private float _nextTouchTime;

        public event Action ScreenOpened;
        public event Action ScreenClosed;
        public event Action CallRequested;

        public bool IsHeld { get; private set; }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(HandleSelectEntered);
                _grabInteractable.selectExited.AddListener(HandleSelectExited);
            }

            IsHeld = false;
            ResetTouchState();
            HideAll();
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.RemoveListener(HandleSelectEntered);
                _grabInteractable.selectExited.RemoveListener(HandleSelectExited);
            }

            bool wasHeld = IsHeld;
            IsHeld = false;
            ResetTouchState();
            HideAll();

            if (wasHeld)
            {
                ScreenClosed?.Invoke();
            }
        }

        private void Update()
        {
            if (!IsHeld)
            {
                return;
            }

            BoxCollider touchArea = GetActiveTouchArea();
            if (touchArea == null || !IsTouching(touchArea))
            {
                _isTouchActive = false;
                return;
            }

            if (_isTouchActive || Time.time < _nextTouchTime)
            {
                _isTouchActive = true;
                return;
            }

            _isTouchActive = true;
            _nextTouchTime = Time.time + _touchDebounceTime;
            PlayHaptic();
            CallRequested?.Invoke();
        }

        private void OnValidate()
        {
            _touchDebounceTime = Mathf.Max(0f, _touchDebounceTime);

            if (_grabInteractable == null)
            {
                _grabInteractable = GetComponent<XRGrabInteractable>();
            }
        }

        public void ShowContact(CellPhoneContact contact)
        {
            if (!IsHeld)
            {
                HideAll();
                return;
            }

            bool showManagement = contact == CellPhoneContact.Management;

            if (_panel119 != null)
            {
                _panel119.SetActive(!showManagement);
            }

            if (_panelManagement != null)
            {
                _panelManagement.SetActive(showManagement);
            }

            ResetTouchState();
        }

        public void HideAll()
        {
            if (_panel119 != null)
            {
                _panel119.SetActive(false);
            }

            if (_panelManagement != null)
            {
                _panelManagement.SetActive(false);
            }
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (IsHeld)
            {
                return;
            }

            IsHeld = true;
            ResetTouchState();
            HideAll();
            ScreenOpened?.Invoke();
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            if (!IsHeld)
            {
                HideAll();
                return;
            }

            IsHeld = false;
            ResetTouchState();
            HideAll();
            ScreenClosed?.Invoke();
        }

        private BoxCollider GetActiveTouchArea()
        {
            if (_panelManagement != null &&
                _panelManagement.activeInHierarchy &&
                IsAvailable(_callManagementTouchArea))
            {
                return _callManagementTouchArea;
            }

            if (_panel119 != null &&
                _panel119.activeInHierarchy &&
                IsAvailable(_call119TouchArea))
            {
                return _call119TouchArea;
            }

            return null;
        }

        private bool IsTouching(BoxCollider touchArea)
        {
            Transform touchTransform = touchArea.transform;
            Vector3 center = touchTransform.TransformPoint(touchArea.center);
            Vector3 halfExtents = Vector3.Scale(
                touchArea.size * 0.5f,
                Abs(touchTransform.lossyScale));
            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                _touchHits,
                touchTransform.rotation,
                _touchLayerMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _touchHits[i];
                if (hit != null && hit != touchArea)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlayHaptic()
        {
            if (_hapticPlayer != null)
            {
                _hapticPlayer.SendHapticImpulse(
                    _hapticAmplitude,
                    _hapticDuration);
            }
        }

        private void ResetTouchState()
        {
            _isTouchActive = false;
            _nextTouchTime = 0f;
        }

        private static bool IsAvailable(BoxCollider touchArea)
        {
            return touchArea != null &&
                   touchArea.enabled &&
                   touchArea.gameObject.activeInHierarchy;
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(
                Mathf.Abs(value.x),
                Mathf.Abs(value.y),
                Mathf.Abs(value.z));
        }
    }
}
