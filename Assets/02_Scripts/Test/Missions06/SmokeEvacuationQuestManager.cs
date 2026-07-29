using UnityEngine;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Missions09;

namespace VirtualRescue.Missions06
{
    public enum SmokeEvacuationQuestStep
    {
        WaitingForStart,
        WaitingForWetHandkerchief,
        WaitingForFaceProtection,
        WaitingForSmokeEntry,
        WaitingForDoorOpen,
        WaitingForDoorPass,
        Completed
    }

    public enum SmokeEvacuationQuestTriggerType
    {
        Start,
        SmokeEntry,
        Finish
    }

    public sealed class SmokeEvacuationQuestManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private HandkerChiefWet _handkerchief;
        [SerializeField] private HandkerChiefEnterTrigger _faceTrigger;
        [SerializeField] private FireExitDoorController _doorController;
        [SerializeField] private ParticleSystem _smokeParticle;

        [Header("Dialogue Groups")]
        [SerializeField] private string _startDialogueGroup = "quest06_start";
        [SerializeField] private string _handkerchiefDialogueGroup = "quest06_handkerchief";
        [SerializeField] private string _doorDialogueGroup = "quest06_door";
        [SerializeField] private string _smokeDialogueGroup = "quest06_smoke";
        [SerializeField] private string _finishDialogueGroup = "quest06_finish";

        private SmokeEvacuationQuestStep _currentStep = SmokeEvacuationQuestStep.WaitingForStart;

        public SmokeEvacuationQuestStep CurrentStep => _currentStep;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_handkerchief == null)
            {
                _handkerchief = GetComponentInChildren<HandkerChiefWet>(true);
            }

            if (_faceTrigger == null)
            {
                _faceTrigger = FindFirstObjectByType<HandkerChiefEnterTrigger>();
            }

            if (_doorController == null)
            {
                _doorController = GetComponentInChildren<FireExitDoorController>(true);
            }

            if (_smokeParticle == null)
            {
                _smokeParticle = GetComponentInChildren<ParticleSystem>(true);
            }

            if (_smokeParticle != null)
            {
                _smokeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_handkerchief != null)
            {
                _handkerchief.CompletelyWet += HandleHandkerchiefWet;
            }

            if (_faceTrigger != null)
            {
                _faceTrigger.WetHandkerchiefApplied += HandleFaceProtected;
            }

            if (_doorController != null)
            {
                _doorController.Opened += HandleDoorOpened;
            }
        }

        private void OnDisable()
        {
            if (_handkerchief != null)
            {
                _handkerchief.CompletelyWet -= HandleHandkerchiefWet;
            }

            if (_faceTrigger != null)
            {
                _faceTrigger.WetHandkerchiefApplied -= HandleFaceProtected;
            }

            if (_doorController != null)
            {
                _doorController.Opened -= HandleDoorOpened;
            }
        }

        public bool TryTrigger(SmokeEvacuationQuestTriggerType triggerType)
        {
            switch (triggerType)
            {
                case SmokeEvacuationQuestTriggerType.Start:
                    return TryStartQuest();

                case SmokeEvacuationQuestTriggerType.SmokeEntry:
                    return TryEnterSmokeArea();

                case SmokeEvacuationQuestTriggerType.Finish:
                    return TryFinishQuest();

                default:
                    return false;
            }
        }

        private bool TryStartQuest()
        {
            if (_currentStep != SmokeEvacuationQuestStep.WaitingForStart || _dialogueManager == null)
            {
                return false;
            }

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(_startDialogueGroup);
            _currentStep = SmokeEvacuationQuestStep.WaitingForWetHandkerchief;
            return true;
        }

        private void HandleHandkerchiefWet()
        {
            if (_currentStep != SmokeEvacuationQuestStep.WaitingForWetHandkerchief || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_handkerchiefDialogueGroup);
            _currentStep = SmokeEvacuationQuestStep.WaitingForFaceProtection;
        }

        private void HandleFaceProtected()
        {
            if (_currentStep != SmokeEvacuationQuestStep.WaitingForFaceProtection || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_doorDialogueGroup);
            _currentStep = SmokeEvacuationQuestStep.WaitingForSmokeEntry;
        }

        private bool TryEnterSmokeArea()
        {
            if (_currentStep != SmokeEvacuationQuestStep.WaitingForSmokeEntry || _dialogueManager == null)
            {
                return false;
            }

            if (_smokeParticle != null)
            {
                _smokeParticle.Play(true);
            }

            _dialogueManager.PlayGroup(_smokeDialogueGroup);
            _currentStep = SmokeEvacuationQuestStep.WaitingForDoorOpen;
            return true;
        }

        private void HandleDoorOpened()
        {
            if (_currentStep == SmokeEvacuationQuestStep.WaitingForDoorOpen)
            {
                _currentStep = SmokeEvacuationQuestStep.WaitingForDoorPass;
            }
        }

        private bool TryFinishQuest()
        {
            if (_currentStep != SmokeEvacuationQuestStep.WaitingForDoorPass || _dialogueManager == null)
            {
                return false;
            }

            _dialogueManager.PlayGroup(_finishDialogueGroup);
            _currentStep = SmokeEvacuationQuestStep.Completed;
            return true;
        }

        private void ValidateReferences()
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("06 연기 대피 퀘스트에서 DialogueManager를 찾을 수 없습니다.", this);
            }

            if (_handkerchief == null)
            {
                Debug.LogWarning("06 연기 대피 구역에서 HandkerChiefWet을 찾을 수 없습니다.", this);
            }

            if (_faceTrigger == null)
            {
                Debug.LogWarning("PlayerPrefabs에서 HMD 손수건 감지 트리거를 찾을 수 없습니다.", this);
            }

            if (_doorController == null)
            {
                Debug.LogWarning("06 연기 대피 구역에서 비상문 컨트롤러를 찾을 수 없습니다.", this);
            }

            if (_smokeParticle == null)
            {
                Debug.LogWarning("06 연기 대피 구역에서 Smoke ParticleSystem을 찾을 수 없습니다.", this);
            }
        }
    }
}
