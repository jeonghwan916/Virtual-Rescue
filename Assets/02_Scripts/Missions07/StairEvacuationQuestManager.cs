using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Missions09;

namespace VirtualRescue.Missions07
{
    public enum StairEvacuationQuestStep
    {
        WaitingForStart,
        WaitingForStairDoor,
        WaitingForFinishDialogue,
        Completed
    }

    public sealed class StairEvacuationQuestManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private FireExitDoorController _stairDoor;

        [Header("Dialogue Groups")]
        [SerializeField] private string _startDialogueGroup = "quest07_start";
        [SerializeField] private string _finishDialogueGroup = "quest07_finish";

        private StairEvacuationQuestStep _currentStep = StairEvacuationQuestStep.WaitingForStart;

        public StairEvacuationQuestStep CurrentStep => _currentStep;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_stairDoor == null)
            {
                _stairDoor = GetComponentInChildren<FireExitDoorController>(true);
            }

            ValidateReferences();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            
            if (_stairDoor != null)
            {
                _stairDoor.Opened += HandleStairDoorOpened;
            }

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted += HandleDialogueGroupCompleted;
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            
            if (_stairDoor != null)
            {
                _stairDoor.Opened -= HandleStairDoorOpened;
            }

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted -= HandleDialogueGroupCompleted;
            }
        }

        public bool TryStartQuest()
        {
            if (_currentStep != StairEvacuationQuestStep.WaitingForStart || _dialogueManager == null)
            {
                return false;
            }

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(_startDialogueGroup);
            _currentStep = StairEvacuationQuestStep.WaitingForStairDoor;

            if (_stairDoor != null && _stairDoor.IsOpen)
            {
                HandleStairDoorOpened();
            }

            return true;
        }

        private void HandleStairDoorOpened()
        {
            if (_currentStep != StairEvacuationQuestStep.WaitingForStairDoor ||
                _dialogueManager == null)
            {
                return;
            }

            _currentStep = StairEvacuationQuestStep.WaitingForFinishDialogue;
            _dialogueManager.PlayGroup(_finishDialogueGroup);
        }

        private void HandleDialogueGroupCompleted(string groupId)
        {
            if (_currentStep != StairEvacuationQuestStep.WaitingForFinishDialogue ||
                groupId != _finishDialogueGroup)
            {
                return;
            }

            _currentStep = StairEvacuationQuestStep.Completed;
        }

        private void ValidateReferences()
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("Mission 07 could not find DialogueManager.", this);
            }

            if (_stairDoor == null)
            {
                Debug.LogWarning("Mission 07 could not find the emergency stair door.", this);
            }
        }
        
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "S_Env")
            {
                return;
            }

            TryBindStairDoor();
        }
        
        private void TryBindStairDoor()
        {
            FireExitDoorController stairDoor = Mission07References.FireExitDoorController;
            if (stairDoor == null)
            {
                return;
            }

            _stairDoor = stairDoor;
            _stairDoor.Opened += HandleStairDoorOpened;
        }
    }
}
