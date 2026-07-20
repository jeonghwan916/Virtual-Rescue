using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VirtualRescue.Interactions
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class FireBellButton : MonoBehaviour
    {
        [SerializeField] private Light _emergencyLight;
        [SerializeField] private Transform _buttonVisual;
        [SerializeField] private Vector3 _pressedLocalPosition = new Vector3(0f, 0f, 0f);
        [SerializeField] private Collider _buttonCollider;
        [SerializeField] private Behaviour _pokeFollowBehaviour;

        private AudioSource _emergencyBell;
        private XRSimpleInteractable _interactable;
        private bool _isPressed;

        private void Awake()
        {
            _emergencyBell = GetComponent<AudioSource>();
            _interactable = GetComponent<XRSimpleInteractable>();

            if (_emergencyLight != null)
            {
                _emergencyLight.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.AddListener(OnSelectEntered);
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            PressButton();
        }

        public void PressButton()
        {
            if (_isPressed)
            {
                return;
            }

            _isPressed = true;

            if (_emergencyBell != null)
            {
                _emergencyBell.Play();
            }

            if (_emergencyLight != null)
            {
                _emergencyLight.enabled = true;
            }

            StartCoroutine(LockButtonAfterPress());
        }

        private IEnumerator LockButtonAfterPress()
        {
            yield return new WaitForSeconds(0.1f);

            if (_pokeFollowBehaviour != null)
            {
                _pokeFollowBehaviour.enabled = false;
            }

            if (_buttonVisual != null)
            {
                _buttonVisual.localPosition = _pressedLocalPosition;
            }

            if (_buttonCollider != null)
            {
                _buttonCollider.enabled = false;
            }

            if (_interactable != null)
            {
                _interactable.enabled = false;
            }
        }
    }
}