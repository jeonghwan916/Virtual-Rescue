using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.Missions03
{
    public enum FireExtinguisherQuestStep
    {
        WaitingForStart,
        WaitingForExtinguisherGrab,
        WaitingForSafetyPin,
        WaitingForExtinguishing,
        PlayingCompletion,
        Completed
    }

    public sealed class FireExtinguisherQuestManager : MonoBehaviour
    {
        private const int ExpectedFireCount = 3;

        [Header("References")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private FireExtinguisher _fireExtinguisher;
        [SerializeField] private FireObject[] _fireObjects;

        [Header("Dialogue Groups")]
        [SerializeField] private string _startGroup = "start";
        [SerializeField] private string _grabGroup = "grab";
        [SerializeField] private string _removePinGroup = "remove_pin";
        [SerializeField] private string _completeGroup = "complete";

        private readonly Dictionary<FireObject, Action> _fireEventHandlers = new();
        private readonly HashSet<FireObject> _extinguishedFireObjects = new();

        private FireExtinguisherQuestStep _currentStep =
            FireExtinguisherQuestStep.WaitingForStart;
        private string _activeDialogueGroup = string.Empty;
        private bool _isDialoguePlaying;
        private bool _hasExtinguisherBeenGrabbed;
        private bool _hasSafetyPinBeenPulled;

        public FireExtinguisherQuestStep CurrentStep => _currentStep;
        public int ExtinguishedFireCount => _extinguishedFireObjects.Count;
        public int RequiredFireCount => _fireEventHandlers.Count;
        public bool IsCompleted => _currentStep == FireExtinguisherQuestStep.Completed;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_fireExtinguisher == null)
            {
                _fireExtinguisher = GetComponentInChildren<FireExtinguisher>(true);
            }

            if (_fireObjects == null || _fireObjects.Length == 0)
            {
                _fireObjects = GetComponentsInChildren<FireObject>(true);
            }

            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted += HandleDialogueGroupCompleted;
            }

            if (_fireExtinguisher != null)
            {
                _fireExtinguisher.Grabbed += HandleExtinguisherGrabbed;
                _fireExtinguisher.SafetyPinPulled += HandleSafetyPinPulled;
            }

            BindFireEvents();
        }

        private void OnDisable()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted -= HandleDialogueGroupCompleted;
            }

            if (_fireExtinguisher != null)
            {
                _fireExtinguisher.Grabbed -= HandleExtinguisherGrabbed;
                _fireExtinguisher.SafetyPinPulled -= HandleSafetyPinPulled;
            }

            UnbindFireEvents();
        }

        public bool TryStartQuest()
        {
            if (_currentStep != FireExtinguisherQuestStep.WaitingForStart ||
                _dialogueManager == null ||
                _isDialoguePlaying)
            {
                return false;
            }

            PlayGroup(
                _startGroup,
                FireExtinguisherQuestStep.WaitingForExtinguisherGrab);
            return true;
        }

        private void HandleExtinguisherGrabbed()
        {
            _hasExtinguisherBeenGrabbed = true;
            TryAdvanceFromPendingEvents();
        }

        private void HandleSafetyPinPulled()
        {
            _hasSafetyPinBeenPulled = true;
            TryAdvanceFromPendingEvents();
        }

        private void HandleFireExtinguished(FireObject fireObject)
        {
            if (fireObject == null || !_fireEventHandlers.ContainsKey(fireObject))
            {
                return;
            }

            _extinguishedFireObjects.Add(fireObject);
            TryAdvanceFromPendingEvents();
        }

        private void HandleDialogueGroupCompleted(string groupId)
        {
            if (!_isDialoguePlaying || groupId != _activeDialogueGroup)
            {
                return;
            }

            _activeDialogueGroup = string.Empty;
            _isDialoguePlaying = false;

            if (_currentStep == FireExtinguisherQuestStep.PlayingCompletion)
            {
                _currentStep = FireExtinguisherQuestStep.Completed;
                return;
            }

            TryAdvanceFromPendingEvents();
        }

        private void TryAdvanceFromPendingEvents()
        {
            if (_dialogueManager == null || _isDialoguePlaying)
            {
                return;
            }

            switch (_currentStep)
            {
                case FireExtinguisherQuestStep.WaitingForExtinguisherGrab:
                    if (_hasExtinguisherBeenGrabbed)
                    {
                        PlayGroup(
                            _grabGroup,
                            FireExtinguisherQuestStep.WaitingForSafetyPin);
                    }

                    break;

                case FireExtinguisherQuestStep.WaitingForSafetyPin:
                    if (_hasSafetyPinBeenPulled)
                    {
                        PlayGroup(
                            _removePinGroup,
                            FireExtinguisherQuestStep.WaitingForExtinguishing);
                    }

                    break;

                case FireExtinguisherQuestStep.WaitingForExtinguishing:
                    if (AreAllFiresExtinguished())
                    {
                        PlayGroup(
                            _completeGroup,
                            FireExtinguisherQuestStep.PlayingCompletion);
                    }

                    break;
            }
        }

        private void PlayGroup(
            string groupId,
            FireExtinguisherQuestStep nextStep)
        {
            _activeDialogueGroup = groupId;
            _isDialoguePlaying = true;
            _currentStep = nextStep;

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(groupId);
        }

        private bool AreAllFiresExtinguished()
        {
            return RequiredFireCount > 0 &&
                   _extinguishedFireObjects.Count >= RequiredFireCount;
        }

        private void BindFireEvents()
        {
            UnbindFireEvents();

            if (_fireObjects == null)
            {
                return;
            }

            foreach (FireObject fireObject in _fireObjects)
            {
                if (fireObject == null || _fireEventHandlers.ContainsKey(fireObject))
                {
                    continue;
                }

                FireObject target = fireObject;
                Action handler = () => HandleFireExtinguished(target);
                _fireEventHandlers.Add(target, handler);
                target.OnExtinguished += handler;
            }
        }

        private void UnbindFireEvents()
        {
            foreach (KeyValuePair<FireObject, Action> entry in _fireEventHandlers)
            {
                if (entry.Key != null)
                {
                    entry.Key.OnExtinguished -= entry.Value;
                }
            }

            _fireEventHandlers.Clear();
        }

        private void ValidateReferences()
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning(
                    "03 소화기 미션에서 DialogueManager를 찾을 수 없습니다.",
                    this);
            }

            if (_fireExtinguisher == null)
            {
                Debug.LogWarning(
                    "03 소화기 미션에서 FireExtinguisher를 찾을 수 없습니다.",
                    this);
            }

            HashSet<FireObject> validFireObjects = new();
            if (_fireObjects != null)
            {
                foreach (FireObject fireObject in _fireObjects)
                {
                    if (fireObject != null)
                    {
                        validFireObjects.Add(fireObject);
                    }
                }
            }

            if (validFireObjects.Count != ExpectedFireCount)
            {
                Debug.LogWarning(
                    $"03 소화기 미션의 화재 대상은 {ExpectedFireCount}개여야 합니다. " +
                    $"현재 연결된 대상: {validFireObjects.Count}개",
                    this);
            }
        }
    }
}
