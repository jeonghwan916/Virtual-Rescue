using UnityEngine;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Missions;

namespace VirtualRescue.Missions09
{
    public enum RefugeAreaQuestStep
    {
        WaitingForStart,
        WaitingForEntry,
        WaitingForDoorClose,
        WaitingForSealInstruction,
        WaitingForSealing,
        Completed
    }

    public enum RefugeAreaQuestTriggerType
    {
        Start,
        EnterRefugeArea
    }

    public sealed class RefugeAreaQuestManager : MonoBehaviour
    {
        private const string BeginSealingCallbackKey = "Quest09BeginSealing";

        [Header("References")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private FireExitDoorController _doorController;
        [SerializeField] private WindowSeal _windowSeal;

        [Header("Dialogue Groups")]
        [SerializeField] private string _startDialogueGroup = "quest09_start";
        [SerializeField] private string _entryDialogueGroup = "quest09_door";
        [SerializeField] private string _callDialogueGroup = "quest09_call";
        [SerializeField] private string _finishDialogueGroup = "quest09_finish";

        private RefugeAreaQuestStep _currentStep = RefugeAreaQuestStep.WaitingForStart;

        public RefugeAreaQuestStep CurrentStep => _currentStep;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_dialogueManager != null)
            {
                _dialogueManager.RegisterCallback(BeginSealingCallbackKey, BeginSealingStep);
            }

            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_doorController != null)
            {
                _doorController.Closed += HandleDoorClosed;
            }

            if (_windowSeal != null)
            {
                _windowSeal.Sealed += HandleOpeningsSealed;
            }
        }

        private void OnDisable()
        {
            if (_doorController != null)
            {
                _doorController.Closed -= HandleDoorClosed;
            }

            if (_windowSeal != null)
            {
                _windowSeal.Sealed -= HandleOpeningsSealed;
            }
        }

        public bool TryTrigger(RefugeAreaQuestTriggerType triggerType)
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("DialogueManager가 없어 대피공간 퀘스트를 진행할 수 없습니다.", this);
                return false;
            }

            switch (triggerType)
            {
                case RefugeAreaQuestTriggerType.Start:
                    return TryStartQuest();

                case RefugeAreaQuestTriggerType.EnterRefugeArea:
                    return TryEnterRefugeArea();

                default:
                    Debug.LogWarning($"지원하지 않는 대피공간 퀘스트 트리거입니다: {triggerType}", this);
                    return false;
            }
        }

        private bool TryStartQuest()
        {
            if (_currentStep != RefugeAreaQuestStep.WaitingForStart)
            {
                return false;
            }

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(_startDialogueGroup);
            _currentStep = RefugeAreaQuestStep.WaitingForEntry;
            return true;
        }

        private bool TryEnterRefugeArea()
        {
            if (_currentStep != RefugeAreaQuestStep.WaitingForEntry)
            {
                return false;
            }

            _dialogueManager.PlayGroup(_entryDialogueGroup);
            _currentStep = RefugeAreaQuestStep.WaitingForDoorClose;
            return true;
        }

        private void HandleDoorClosed()
        {
            if (_currentStep != RefugeAreaQuestStep.WaitingForDoorClose || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_callDialogueGroup);
            _currentStep = RefugeAreaQuestStep.WaitingForSealInstruction;
        }

        private void BeginSealingStep()
        {
            if (_currentStep != RefugeAreaQuestStep.WaitingForSealInstruction)
            {
                return;
            }

            _currentStep = RefugeAreaQuestStep.WaitingForSealing;

            if (_windowSeal != null && _windowSeal.IsSealed)
            {
                CompleteQuest();
            }
        }

        private void HandleOpeningsSealed()
        {
            if (_currentStep != RefugeAreaQuestStep.WaitingForSealing)
            {
                return;
            }

            CompleteQuest();
        }

        private void CompleteQuest()
        {
            if (_currentStep == RefugeAreaQuestStep.Completed || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_finishDialogueGroup);
            _currentStep = RefugeAreaQuestStep.Completed;
        }

        private void ValidateReferences()
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("대피공간 퀘스트에서 DialogueManager를 찾을 수 없습니다.", this);
            }

            if (_doorController == null)
            {
                Debug.LogWarning("대피공간 퀘스트에 FireExitDoorController를 연결하세요.", this);
            }

            if (_windowSeal == null)
            {
                Debug.LogWarning("대피공간 퀘스트에 WindowSeal을 연결하세요.", this);
            }
        }
    }
}
