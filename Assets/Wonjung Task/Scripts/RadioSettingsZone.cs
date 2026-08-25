using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.Player;

namespace VirtualRescue.Lobby
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RadioSettingsZone : MonoBehaviour
    {
        [SerializeField]
        private GameObject _settingsUI;

        private readonly HashSet<Collider> _playerColliders = new();

        private void Reset()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        private void Awake()
        {
            if (_settingsUI == null)
            {
                Debug.LogError(
                    $"{nameof(RadioSettingsZone)}: Setting UI가 연결되지 않았습니다.",
                    this);
                return;
            }

            _settingsUI.SetActive(false);
        }

        private void Start()
        {
            SetFarInteractionState(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            PersistentPlayerRoot player =
                other.GetComponentInParent<PersistentPlayerRoot>();

            if (player == null ||
                player != PersistentPlayerRoot.Instance)
            {
                return;
            }

            bool isFirstPlayerCollider = _playerColliders.Count == 0;
            if (!_playerColliders.Add(other) || !isFirstPlayerCollider)
            {
                return;
            }

            _settingsUI.SetActive(true);
            SetFarInteractionState(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_playerColliders.Remove(other))
            {
                return;
            }

            if (_playerColliders.Count == 0 && _settingsUI != null)
            {
                _settingsUI.SetActive(false);
                SetFarInteractionState(false);
            }
        }

        private void OnDisable()
        {
            _playerColliders.Clear();

            if (_settingsUI != null)
            {
                _settingsUI.SetActive(false);
            }

            SetFarInteractionState(false);
        }

        private static void SetFarInteractionState(bool isExtended)
        {
            if (PlayerReferenceHub.Instance != null)
            {
                PlayerReferenceHub.Instance.SetFarInteractionState(isExtended);
            }
        }
    }
}
