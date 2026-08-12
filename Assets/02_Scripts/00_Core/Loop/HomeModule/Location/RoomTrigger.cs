using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.Player;

namespace VirtualRescue.Locations
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class RoomTrigger : MonoBehaviour
    {
        [SerializeField] private RoomLocation _location;
        public RoomLocation Location => _location;

        [SerializeField] private string _playerTag = "Player";

        private RoomVisitTracker _visitTracker;
        private string _situationDialogueId;
        private bool _hasPlayedEntryDialogue;


        private void Awake()
        {
            ConfigureCollider();
            FindVisitTracker();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayerCollider(other))
            {
                return;
            }

            if (_visitTracker == null)
            {
                FindVisitTracker();
            }

            if (_visitTracker == null)
            {
                Debug.LogWarning(
                    $"{name}: RoomVisitTracker를 찾을 수 없습니다.",
                    this);
                return;
            }

            _visitTracker.Enter(Location);

            if (_hasPlayedEntryDialogue)
            {
                return;
            }

            _hasPlayedEntryDialogue = true;
            string dialogueId = string.IsNullOrEmpty(_situationDialogueId)
                ? Location.ToString()
                : _situationDialogueId;
            _visitTracker.DisplayDialogue(dialogueId);
        }

        public void ConfigureSituation(string dialogueId)
        {
            _situationDialogueId = string.IsNullOrWhiteSpace(dialogueId)
                ? string.Empty
                : dialogueId.Trim();
        }

        public void ResetDayState()
        {
            _hasPlayedEntryDialogue = false;
            _situationDialogueId = string.Empty;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayerCollider(other))
            {
                return;
            }

            if (_visitTracker == null)
            {
                FindVisitTracker();
            }

            if (_visitTracker == null)
            {
                Debug.LogWarning(
                    $"{name}: RoomVisitTracker를 찾을 수 없습니다.",
                    this);
                return;
            }
            
            _visitTracker.Leave();
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other == null || string.IsNullOrWhiteSpace(_playerTag))
            {
                return false;
            }

            Transform current = other.transform;

            while (current != null)
            {
                if (current.CompareTag(_playerTag))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void FindVisitTracker()
        {
            PersistentPlayerRoot playerRoot = PersistentPlayerRoot.Instance;

            if (playerRoot != null)
            {
                _visitTracker =
                    playerRoot.GetComponent<RoomVisitTracker>();
            }
        }

        private void ConfigureCollider()
        {
            Collider triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }
    }
}
