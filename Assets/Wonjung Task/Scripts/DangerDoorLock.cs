using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VirtualRescue.Missions09;

namespace VirtualRescue.Interaction
{
    public sealed class DangerDoorsLock : MonoBehaviour
    {
        [SerializeField]
        private DoorHandleTemperature _temperature;

        [SerializeField]
        private XRSimpleInteractable _handleInteractable;

        [SerializeField]
        private XRSimpleInteractable _doorInteractable;

        [SerializeField]
        private FireExitDoorController _doorController;

        private bool _previousDangerState;

        private void Awake()
        {
            _previousDangerState = !_temperature.IsDangerous;
            ApplyLock();
        }

        private void Update()
        {
            if (_temperature == null)
            {
                return;
            }

            if (_previousDangerState ==
                _temperature.IsDangerous)
            {
                return;
            }

            ApplyLock();
        }

        private void ApplyLock()
        {
            bool isDangerous = _temperature.IsDangerous;
            bool canOpen = !isDangerous;

            if (_handleInteractable != null)
            {
                _handleInteractable.enabled = canOpen;
            }

            if (_doorInteractable != null)
            {
                _doorInteractable.enabled = canOpen;
            }

            if (_doorController != null)
            {
                _doorController.enabled = canOpen;
            }

            _previousDangerState = isDangerous;
        }
    }
}
