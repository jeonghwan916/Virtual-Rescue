using UnityEngine;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Missions09;

namespace VirtualRescue.CallRefuge
{
    public enum CallRefugeQuestStep
    {
        Start,
        Entrance,
        Close,
        BeforeCall,
        AfterCall,
        Completed
    }
    
    public sealed class CallRefugeQuestManager : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private string _startDialogueGroup = "start";
        [SerializeField] private string _entranceDialogueGroup = "entrance";
        [SerializeField] private string _closeDialogueGroup = "close";
        [SerializeField] private string _beforecallDialogueGroup = "beforecall";
        [SerializeField] private string _aftercallDialogueGroup = "aftercall";

        [Header("In-Scene Component")]
        [SerializeField] private NumPad _numPad;
        [SerializeField] private FireExitDoorController _fireExitDoorController;

        private CallRefugeQuestStep _currentStep = CallRefugeQuestStep.Start;
        private CallRefugeQuestStep _nextStep;
        private string _activeDialogueGroup;
        private bool _isDialoguePlaying;
        
        public CallRefugeQuestStep CurrentStep => _currentStep;
        public bool IsDialoguePlaying => _isDialoguePlaying;
        public bool IsCompleted => _currentStep == CallRefugeQuestStep.Completed;
        
        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_dialogueManager == null)
            {
                Debug.LogWarning(
                    "Call Refuge 퀘스트에서 DialogueManager를 찾을 수 없습니다.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted += HandleDialogueGroupCompleted;
                _numPad.OnCorrectNumber += OnCellPhoneCall;
                _fireExitDoorController.Closed += OnDoorClosed;
            }
        }

        private void OnDisable()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted -= HandleDialogueGroupCompleted;
                _numPad.OnCorrectNumber -= OnCellPhoneCall;
                _fireExitDoorController.Closed -= OnDoorClosed;
            }
        }

        public bool TryAdvance(CallRefugeQuestStep questStep)
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning(
                    "DialogueManager가 없어 Call Refuge 퀘스트를 진행할 수 없습니다.",
                    this);
                return false;
            }

            if (_isDialoguePlaying || questStep != _currentStep)
            {
                return false;
            }

            switch (questStep)
            {
                case CallRefugeQuestStep.Start:
                    PlayGroup(
                        _startDialogueGroup,
                        CallRefugeQuestStep.Entrance);
                    break;

                case CallRefugeQuestStep.Entrance:
                    PlayGroup(
                        _entranceDialogueGroup,
                        CallRefugeQuestStep.Close);
                    break;

                case CallRefugeQuestStep.Close:
                    PlayGroup(
                        _closeDialogueGroup,
                        CallRefugeQuestStep.BeforeCall);
                    break;

                case CallRefugeQuestStep.BeforeCall:
                    PlayGroup(
                        _beforecallDialogueGroup,
                        CallRefugeQuestStep.AfterCall);
                    break;
                
                case CallRefugeQuestStep.AfterCall:
                    PlayGroup(
                        _aftercallDialogueGroup,
                        CallRefugeQuestStep.Completed);
                    break;

                case CallRefugeQuestStep.Completed:
                    return false;

                default:
                    Debug.LogWarning(
                        $"지원하지 않는 Alarm Stairs 퀘스트 단계입니다: {questStep}",
                        this);
                    return false;
            }

            return true;
        }

        private void PlayGroup(
            string groupId,
            CallRefugeQuestStep nextStep)
        {
            _activeDialogueGroup = groupId;
            _nextStep = nextStep;
            _isDialoguePlaying = true;

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(groupId);
        }

        private void HandleDialogueGroupCompleted(string groupId)
        {
            if (!_isDialoguePlaying || groupId != _activeDialogueGroup)
            {
                return;
            }

            _currentStep = _nextStep;
            _activeDialogueGroup = string.Empty;
            _isDialoguePlaying = false;
        }

        private void OnCellPhoneCall()
        {
            TryAdvance(CallRefugeQuestStep.AfterCall);
        }

        private void OnDoorClosed()
        {
            TryAdvance(CallRefugeQuestStep.BeforeCall);
        }
    }
}
