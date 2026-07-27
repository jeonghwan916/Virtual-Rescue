using UnityEngine;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Interactions;

namespace VirtualRescue.Missions02
{
    public enum FireAlarmQuestStep
    {
        WaitingForStart,
        WaitingForBell,
        WaitingForFinishDialogue,
        Completed
    }

    public sealed class FireAlarmQuestManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private FireBellButton _fireBellButton;

        [Header("Dialogue Groups")]
        [SerializeField] private string _startDialogueGroup = "quest02_start";
        [SerializeField] private string _finishDialogueGroup = "quest02_finish";

        private FireAlarmQuestStep _currentStep = FireAlarmQuestStep.WaitingForStart;

        public FireAlarmQuestStep CurrentStep => _currentStep;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_fireBellButton == null)
            {
                _fireBellButton = GetComponentInChildren<FireBellButton>(true);
            }

            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_fireBellButton != null)
            {
                _fireBellButton.Pressed += HandleBellPressed;
            }

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted += HandleDialogueGroupCompleted;
            }
        }

        private void OnDisable()
        {
            if (_fireBellButton != null)
            {
                _fireBellButton.Pressed -= HandleBellPressed;
            }

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted -= HandleDialogueGroupCompleted;
            }
        }

        public bool TryStartQuest()
        {
            if (_currentStep != FireAlarmQuestStep.WaitingForStart || _dialogueManager == null)
            {
                return false;
            }

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(_startDialogueGroup);
            _currentStep = FireAlarmQuestStep.WaitingForBell;
            return true;
        }

        private void HandleBellPressed()
        {
            if (_currentStep != FireAlarmQuestStep.WaitingForBell || _dialogueManager == null)
            {
                return;
            }

            _currentStep = FireAlarmQuestStep.WaitingForFinishDialogue;
            _dialogueManager.PlayGroup(_finishDialogueGroup);
        }

        private void HandleDialogueGroupCompleted(string groupId)
        {
            if (_currentStep != FireAlarmQuestStep.WaitingForFinishDialogue ||
                groupId != _finishDialogueGroup)
            {
                return;
            }

            if (_fireBellButton != null)
            {
                _fireBellButton.StopBell();
            }

            _currentStep = FireAlarmQuestStep.Completed;
        }

        private void ValidateReferences()
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("02 소방벨 퀘스트에서 DialogueManager를 찾을 수 없습니다.", this);
            }

            if (_fireBellButton == null)
            {
                Debug.LogWarning("02 소방벨 구역에서 FireBellButton을 찾을 수 없습니다.", this);
            }
        }
    }
}
