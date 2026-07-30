using System.Collections;
using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.SmokeStairs
{
    public enum SmokeStairsQuestStep
    {
        Start,
        HandkerChief,
        Exit,
        Elevator,
        Finish,
        Completed
    }

    public sealed class SmokeStairsQuestManager : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private string _startDialogueGroup = "start";
        [SerializeField] private string _handkerchiefDialogueGroup = "handkerchief";
        [SerializeField] private string _exitDialogueGroup = "exit";
        [SerializeField] private string _elevatorDialogueGroup = "elevator";
        [SerializeField] private string _finishDialogueGroup = "finish";
        
        [Header("References")]
        [SerializeField] private VignetteController _vignetteController;
        [SerializeField] private bool _autoStartWhenNoPlayerReferenceHub = true;

        private SmokeStairsQuestStep _currentStep = SmokeStairsQuestStep.Start;
        private SmokeStairsQuestStep _nextStep;
        private string _activeDialogueGroup;
        private bool _isDialoguePlaying;
        private PlayerReferenceHub _playerReferenceHub;
        
        public SmokeStairsQuestStep CurrentStep => _currentStep;
        public bool IsDialoguePlaying => _isDialoguePlaying;
        public bool IsCompleted => _currentStep == SmokeStairsQuestStep.Completed;
        
        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_dialogueManager == null)
            {
                Debug.LogWarning(
                    "Alarm Stairs 퀘스트에서 DialogueManager를 찾을 수 없습니다.",
                    this);
            }

            BindPlayerReferences();
        }

        private IEnumerator Start()
        {
            yield return null;

            if (_autoStartWhenNoPlayerReferenceHub && PlayerReferenceHub.Instance == null)
            {
                TryAdvance(SmokeStairsQuestStep.Start);
                WipeVignetteIn();
            }
        }

        private void OnEnable()
        {
            BindPlayerReferences();

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted += HandleDialogueGroupCompleted;
            }

            if (_playerReferenceHub != null)
            {
                _playerReferenceHub.SceneReady += HandleSceneReady;
            }
        }

        private void OnDisable()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted -= HandleDialogueGroupCompleted;
            }

            if (_playerReferenceHub != null)
            {
                _playerReferenceHub.SceneReady -= HandleSceneReady;
                _playerReferenceHub = null;
            }
        }

        public bool TryAdvance(SmokeStairsQuestStep questStep)
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning(
                    "DialogueManager가 없어 Partition Escape 퀘스트를 진행할 수 없습니다.",
                    this);
                return false;
            }

            if (_isDialoguePlaying || questStep != _currentStep)
            {
                return false;
            }

            switch (questStep)
            {
                case SmokeStairsQuestStep.Start:
                    PlayGroup(
                        _startDialogueGroup,
                        SmokeStairsQuestStep.HandkerChief);
                    break;

                case SmokeStairsQuestStep.HandkerChief:
                    PlayGroup(
                        _handkerchiefDialogueGroup,
                        SmokeStairsQuestStep.Exit);
                    break;

                case SmokeStairsQuestStep.Exit:
                    PlayGroup(
                        _exitDialogueGroup,
                        SmokeStairsQuestStep.Elevator);
                    break;

                case SmokeStairsQuestStep.Elevator:
                    PlayGroup(
                        _elevatorDialogueGroup,
                        SmokeStairsQuestStep.Finish);
                    break;
                
                case SmokeStairsQuestStep.Finish:
                    PlayGroup(
                        _finishDialogueGroup,
                        SmokeStairsQuestStep.Completed);
                    break;

                case SmokeStairsQuestStep.Completed:
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
            SmokeStairsQuestStep nextStep)
        {
            _activeDialogueGroup = groupId;
            _nextStep = nextStep;
            _isDialoguePlaying = true;

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(groupId);
        }

        private void BindPlayerReferences()
        {
            _playerReferenceHub = PlayerReferenceHub.Instance;
            if (_playerReferenceHub == null)
            {
                return;
            }

            if (_vignetteController == null)
            {
                _vignetteController = _playerReferenceHub.VignetteController;
            }
        }

        private void HandleSceneReady()
        {
            TryAdvance(SmokeStairsQuestStep.Start);
            WipeVignetteIn();
        }

        private void WipeVignetteIn()
        {
            if (_vignetteController != null)
            {
                _vignetteController.WipeIn();
            }
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
    }
}
