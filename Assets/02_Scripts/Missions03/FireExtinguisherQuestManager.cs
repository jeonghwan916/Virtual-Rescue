using UnityEngine;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Interactions;

namespace VirtualRescue.Missions03
{
    public enum FireExtinguisherQuestStep
    {
        WaitingForStart,
        WaitingForExtinguisherGrab,
        WaitingForSafetyPin,
        WaitingForExtinguishing,
        Completed
    }

    public sealed class FireExtinguisherQuestManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private FireExtinguisher _fireExtinguisher;
        [SerializeField] private FireObject _fireObject;
        [SerializeField] private FireBellButton _fireBellButton;

        [Header("Dialogue Groups")]
        [SerializeField] private string _startDialogueGroup = "quest03_start";
        [SerializeField] private string _safetyPinDialogueGroup = "quest03_pin";
        [SerializeField] private string _sprayDialogueGroup = "quest03_spray";
        [SerializeField] private string _finishDialogueGroup = "quest03_finish";

        private FireExtinguisherQuestStep _currentStep = FireExtinguisherQuestStep.WaitingForStart;

        public FireExtinguisherQuestStep CurrentStep => _currentStep;

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

            if (_fireObject == null)
            {
                _fireObject = GetComponentInChildren<FireObject>(true);
            }

            if (_fireBellButton == null)
            {
                _fireBellButton = FindFirstObjectByType<FireBellButton>();
            }

            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_fireExtinguisher != null)
            {
                _fireExtinguisher.Grabbed += HandleExtinguisherGrabbed;
                _fireExtinguisher.SafetyPinPulled += HandleSafetyPinPulled;
            }

            if (_fireObject != null)
            {
                _fireObject.OnExtinguished += HandleFireExtinguished;
            }
        }

        private void OnDisable()
        {
            if (_fireExtinguisher != null)
            {
                _fireExtinguisher.Grabbed -= HandleExtinguisherGrabbed;
                _fireExtinguisher.SafetyPinPulled -= HandleSafetyPinPulled;
            }

            if (_fireObject != null)
            {
                _fireObject.OnExtinguished -= HandleFireExtinguished;
            }
        }

        public bool TryStartQuest()
        {
            if (_currentStep != FireExtinguisherQuestStep.WaitingForStart || _dialogueManager == null)
            {
                return false;
            }

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(_startDialogueGroup);
            _fireBellButton?.StartBell();
            _currentStep = FireExtinguisherQuestStep.WaitingForExtinguisherGrab;
            return true;
        }

        private void HandleExtinguisherGrabbed()
        {
            if (_currentStep != FireExtinguisherQuestStep.WaitingForExtinguisherGrab || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_safetyPinDialogueGroup);
            _currentStep = FireExtinguisherQuestStep.WaitingForSafetyPin;
        }

        private void HandleSafetyPinPulled()
        {
            if (_currentStep != FireExtinguisherQuestStep.WaitingForSafetyPin || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_sprayDialogueGroup);
            _currentStep = FireExtinguisherQuestStep.WaitingForExtinguishing;
        }

        private void HandleFireExtinguished()
        {
            if (_currentStep != FireExtinguisherQuestStep.WaitingForExtinguishing || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_finishDialogueGroup);
            _currentStep = FireExtinguisherQuestStep.Completed;
        }

        private void ValidateReferences()
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("03 소화기 퀘스트에서 DialogueManager를 찾을 수 없습니다.", this);
            }

            if (_fireExtinguisher == null)
            {
                Debug.LogWarning("03 소화기 퀘스트 구역 안에서 FireExtinguisher를 찾을 수 없습니다.", this);
            }

            if (_fireObject == null)
            {
                Debug.LogWarning("03 소화기 퀘스트 구역 안에서 FireObject를 찾을 수 없습니다.", this);
            }
        }
    }
}
