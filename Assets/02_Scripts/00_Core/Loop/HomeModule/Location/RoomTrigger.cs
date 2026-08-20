using System;
using UnityEngine;
using VirtualRescue.GameFlow;
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

        public event Action<RoomTrigger, SituationLevel> SituationEntryDialoguePlayed;

        private RoomVisitTracker _visitTracker;
        private string _situationDialogueId;
        private SituationLevel? _situationLevel;
        private bool _hasPlayedEntryDialogue;

        private bool _entryDialogueSuppressed;

        private bool HasSituationDialogue =>
            !string.IsNullOrEmpty(_situationDialogueId);

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

            if (_entryDialogueSuppressed || _hasPlayedEntryDialogue)
            {
                return;
            }

            PlayEntryDialogue();
        }

        public bool TryPlaySituationEntryDialogue()
        {
            if (!HasSituationDialogue)
            {
                return false;
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
                return false;
            }

            if (_entryDialogueSuppressed || _hasPlayedEntryDialogue)
            {
                return false;
            }

            PlayEntryDialogue();
            return true;
        }

        private void PlayEntryDialogue()
        {
            _hasPlayedEntryDialogue = true;
            string dialogueId = HasSituationDialogue
                ? _situationDialogueId
                : Location.ToString();
            _visitTracker.DisplayDialogue(dialogueId);

            if (HasSituationDialogue && _situationLevel.HasValue)
            {
                SituationEntryDialoguePlayed?.Invoke(
                    this,
                    _situationLevel.Value);
            }
        }

        public void ConfigureSituation(
            string dialogueId,
            SituationLevel situationLevel)
        {
            _situationDialogueId = string.IsNullOrWhiteSpace(dialogueId)
                ? string.Empty
                : dialogueId.Trim();

            _situationLevel = HasSituationDialogue
                ? situationLevel
                : null;
        }

        public void SuppressEntryDialogue()
        {
            _entryDialogueSuppressed = true;
        }

        public void ResetDayState()
        {
            _hasPlayedEntryDialogue = false;
            _entryDialogueSuppressed = false;
            _situationDialogueId = string.Empty;
            _situationLevel = null;
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
