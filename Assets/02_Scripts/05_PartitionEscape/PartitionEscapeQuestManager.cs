using System.Collections;
using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.PartitionEscape
{
    public enum PartitionEscapeQuestStep
    {
        Start,
        Entrance,
        Partition,
        Complete,
        Completed
    }

    public sealed class PartitionEscapeQuestManager : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private string _startDialogueGroup = "start";
        [SerializeField] private string _entranceDialogueGroup = "entrance";
        [SerializeField] private string _partitionDialogueGroup = "partition";
        [SerializeField] private string _completeDialogueGroup = "complete";
        [SerializeField] private bool _autoStartWhenNoPlayerReferenceHub = true;

        private PartitionEscapeQuestStep _currentStep = PartitionEscapeQuestStep.Start;
        private PartitionEscapeQuestStep _nextStep;
        private string _activeDialogueGroup;
        private bool _isDialoguePlaying;
        private PlayerReferenceHub _playerReferenceHub;

        public PartitionEscapeQuestStep CurrentStep => _currentStep;
        public bool IsDialoguePlaying => _isDialoguePlaying;
        public bool IsCompleted => _currentStep == PartitionEscapeQuestStep.Completed;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_dialogueManager == null)
            {
                Debug.LogWarning(
                    "Partition Escape 퀘스트에서 DialogueManager를 찾을 수 없습니다.",
                    this);
            }

            BindPlayerReferences();
        }

        private IEnumerator Start()
        {
            yield return null;

            if (_autoStartWhenNoPlayerReferenceHub && PlayerReferenceHub.Instance == null)
            {
                TryAdvance(PartitionEscapeQuestStep.Start);
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

        public bool TryAdvance(PartitionEscapeQuestStep questStep)
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
                case PartitionEscapeQuestStep.Start:
                    PlayGroup(
                        _startDialogueGroup,
                        PartitionEscapeQuestStep.Entrance);
                    break;

                case PartitionEscapeQuestStep.Entrance:
                    PlayGroup(
                        _entranceDialogueGroup,
                        PartitionEscapeQuestStep.Partition);
                    break;

                case PartitionEscapeQuestStep.Partition:
                    PlayGroup(
                        _partitionDialogueGroup,
                        PartitionEscapeQuestStep.Complete);
                    break;

                case PartitionEscapeQuestStep.Complete:
                    PlayGroup(
                        _completeDialogueGroup,
                        PartitionEscapeQuestStep.Completed);
                    break;

                case PartitionEscapeQuestStep.Completed:
                    return false;

                default:
                    Debug.LogWarning(
                        $"지원하지 않는 Partition Escape 퀘스트 단계입니다: {questStep}",
                        this);
                    return false;
            }

            return true;
        }

        private void PlayGroup(
            string groupId,
            PartitionEscapeQuestStep nextStep)
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
        
        private void HandleSceneReady()
        {
            TryAdvance(PartitionEscapeQuestStep.Start);
        }
    }
}
