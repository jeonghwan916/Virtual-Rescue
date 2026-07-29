using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.Missions08
{
    public enum LightweightPartitionQuestStep
    {
        Start,
        BatGuide,
        Finish,
        Completed
    }

    public sealed class LightweightPartitionQuestManager : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private string _startDialogueGroup = "quest08_start";
        [SerializeField] private string _batGuideDialogueGroup = "quest08_bat";
        [SerializeField] private string _finishDialogueGroup = "quest08_finish";

        private LightweightPartitionQuestStep _currentStep = LightweightPartitionQuestStep.Start;

        public LightweightPartitionQuestStep CurrentStep => _currentStep;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_dialogueManager == null)
            {
                Debug.LogWarning("경량칸막이 퀘스트에서 DialogueManager를 찾을 수 없습니다.", this);
            }
        }

        public bool TryAdvance(LightweightPartitionQuestStep questStep)
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("DialogueManager가 없어 경량칸막이 퀘스트를 진행할 수 없습니다.", this);
                return false;
            }

            if (questStep != _currentStep)
            {
                return false;
            }

            switch (questStep)
            {
                case LightweightPartitionQuestStep.Start:
                    _dialogueManager.PlayGroup(_startDialogueGroup);
                    _currentStep = LightweightPartitionQuestStep.BatGuide;
                    break;

                case LightweightPartitionQuestStep.BatGuide:
                    _dialogueManager.PlayGroup(_batGuideDialogueGroup);
                    _currentStep = LightweightPartitionQuestStep.Finish;
                    break;

                case LightweightPartitionQuestStep.Finish:
                    _dialogueManager.PlayGroup(_finishDialogueGroup);
                    _currentStep = LightweightPartitionQuestStep.Completed;
                    break;

                case LightweightPartitionQuestStep.Completed:
                    return false;

                default:
                    Debug.LogWarning($"지원하지 않는 경량칸막이 퀘스트 단계입니다: {questStep}", this);
                    return false;
            }

            return true;
        }
    }
}
