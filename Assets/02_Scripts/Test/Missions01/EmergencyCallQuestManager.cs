using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.Missions01
{
    public enum EmergencyCallQuestStep
    {
        WaitingForStart,
        WaitingForEmergencyCall,
        WaitingForCallDialogue,
        WaitingForFinishDialogue,
        Completed
    }

    public sealed class EmergencyCallQuestManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private global::NumPad _numPad;

        [Header("Dialogue Groups")]
        [SerializeField] private string _startDialogueGroup = "quest01_start";
        [SerializeField] private string _callDialogueGroup = "quest01_call";
        [SerializeField] private string _finishDialogueGroup = "quest01_finish";

        private EmergencyCallQuestStep _currentStep = EmergencyCallQuestStep.WaitingForStart;

        public EmergencyCallQuestStep CurrentStep => _currentStep;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_numPad == null)
            {
                _numPad = GetComponentInChildren<global::NumPad>(true);
            }

            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_numPad != null)
            {
                _numPad.OnCorrectNumber += HandleCorrectNumber;
            }

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted += HandleDialogueGroupCompleted;
            }
        }

        private void OnDisable()
        {
            if (_numPad != null)
            {
                _numPad.OnCorrectNumber -= HandleCorrectNumber;
            }

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted -= HandleDialogueGroupCompleted;
            }
        }

        public bool TryStartQuest()
        {
            if (_currentStep != EmergencyCallQuestStep.WaitingForStart || _dialogueManager == null)
            {
                return false;
            }

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(_startDialogueGroup);
            _currentStep = EmergencyCallQuestStep.WaitingForEmergencyCall;
            return true;
        }

        private void HandleCorrectNumber()
        {
            if (_currentStep != EmergencyCallQuestStep.WaitingForEmergencyCall || _dialogueManager == null)
            {
                return;
            }

            _currentStep = EmergencyCallQuestStep.WaitingForCallDialogue;
            _dialogueManager.PlayGroup(_callDialogueGroup);
        }

        private void HandleDialogueGroupCompleted(string groupId)
        {
            if (_currentStep == EmergencyCallQuestStep.WaitingForCallDialogue &&
                groupId == _callDialogueGroup)
            {
                _currentStep = EmergencyCallQuestStep.WaitingForFinishDialogue;
                _dialogueManager.PlayGroup(_finishDialogueGroup);
                return;
            }

            if (_currentStep == EmergencyCallQuestStep.WaitingForFinishDialogue &&
                groupId == _finishDialogueGroup)
            {
                _currentStep = EmergencyCallQuestStep.Completed;
            }
        }

        private void ValidateReferences()
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("Mission 01 could not find DialogueManager.", this);
            }

            if (_numPad == null)
            {
                Debug.LogWarning("Mission 01 could not find NumPad under 01_EmergencyCall.", this);
            }
        }
    }
}
