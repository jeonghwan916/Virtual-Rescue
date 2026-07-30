using UnityEngine;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Interaction;
using VirtualRescue.Missions09;

namespace VirtualRescue.Missions05
{
    public enum DoorSafetyCheckQuestStep
    {
        WaitingForStart,
        WaitingForDangerHandle,
        WaitingForSafeHandle,
        WaitingForSafeDoor,
        WaitingForPassage,
        WaitingForFinishDialogue,
        Completed
    }

    public sealed class DoorSafetyCheckQuestManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager _dialogueManager;

        [Header("Dialogue Groups")]
        [SerializeField] private string _startDialogueGroup = "quest05_start";
        [SerializeField] private string _dangerDialogueGroup = "quest05_danger";
        [SerializeField] private string _safeDialogueGroup = "quest05_safe";
        [SerializeField] private string _finishDialogueGroup = "quest05_finish";

        private DoorHandleHeatVisual[] _dangerHandleVisuals;
        private FireExitDoorHandle[] _doorHandles;
        private FireExitDoorController[] _doorControllers;
        private DoorSafetyCheckQuestStep _currentStep =
            DoorSafetyCheckQuestStep.WaitingForStart;

        public DoorSafetyCheckQuestStep CurrentStep => _currentStep;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            _dangerHandleVisuals =
                GetComponentsInChildren<DoorHandleHeatVisual>(true);
            _doorHandles =
                GetComponentsInChildren<FireExitDoorHandle>(true);
            _doorControllers =
                GetComponentsInChildren<FireExitDoorController>(true);

            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_dangerHandleVisuals != null)
            {
                foreach (DoorHandleHeatVisual visual in _dangerHandleVisuals)
                {
                    visual.HoverStarted += HandleDangerHandleHovered;
                }
            }

            if (_doorHandles != null)
            {
                foreach (FireExitDoorHandle handle in _doorHandles)
                {
                    handle.HoverStarted += HandleSafeHandleHovered;
                }
            }

            if (_doorControllers != null)
            {
                foreach (FireExitDoorController door in _doorControllers)
                {
                    door.Opened += HandleDoorOpened;
                }
            }

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted +=
                    HandleDialogueGroupCompleted;
            }
        }

        private void OnDisable()
        {
            if (_dangerHandleVisuals != null)
            {
                foreach (DoorHandleHeatVisual visual in _dangerHandleVisuals)
                {
                    visual.HoverStarted -= HandleDangerHandleHovered;
                }
            }

            if (_doorHandles != null)
            {
                foreach (FireExitDoorHandle handle in _doorHandles)
                {
                    handle.HoverStarted -= HandleSafeHandleHovered;
                }
            }

            if (_doorControllers != null)
            {
                foreach (FireExitDoorController door in _doorControllers)
                {
                    door.Opened -= HandleDoorOpened;
                }
            }

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted -=
                    HandleDialogueGroupCompleted;
            }
        }

        public bool TryStartQuest()
        {
            if (_currentStep != DoorSafetyCheckQuestStep.WaitingForStart ||
                _dialogueManager == null)
            {
                return false;
            }

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(_startDialogueGroup);
            _currentStep =
                DoorSafetyCheckQuestStep.WaitingForDangerHandle;
            return true;
        }

        private void HandleDangerHandleHovered(
            DoorHandleHeatVisual visual)
        {
            if (_currentStep !=
                    DoorSafetyCheckQuestStep.WaitingForDangerHandle ||
                _dialogueManager == null ||
                !visual.IsDangerous)
            {
                return;
            }

            _dialogueManager.PlayGroup(_dangerDialogueGroup);
            _currentStep =
                DoorSafetyCheckQuestStep.WaitingForSafeHandle;
        }

        private void HandleSafeHandleHovered(
            FireExitDoorHandle handle)
        {
            if (_currentStep !=
                    DoorSafetyCheckQuestStep.WaitingForSafeHandle ||
                _dialogueManager == null ||
                !handle.CanOperate)
            {
                return;
            }

            _dialogueManager.PlayGroup(_safeDialogueGroup);
            _currentStep =
                DoorSafetyCheckQuestStep.WaitingForSafeDoor;
        }

        private void HandleDoorOpened()
        {
            if (_currentStep !=
                    DoorSafetyCheckQuestStep.WaitingForSafeDoor)
            {
                return;
            }

            _currentStep =
                DoorSafetyCheckQuestStep.WaitingForPassage;
        }

        public bool TryCompletePassage()
        {
            if (_currentStep !=
                    DoorSafetyCheckQuestStep.WaitingForPassage ||
                _dialogueManager == null)
            {
                return false;
            }

            _dialogueManager.PlayGroup(_finishDialogueGroup);
            _currentStep =
                DoorSafetyCheckQuestStep.WaitingForFinishDialogue;
            return true;
        }

        private void HandleDialogueGroupCompleted(string groupId)
        {
            if (_currentStep ==
                    DoorSafetyCheckQuestStep.WaitingForFinishDialogue &&
                groupId == _finishDialogueGroup)
            {
                _currentStep = DoorSafetyCheckQuestStep.Completed;
            }
        }

        private void ValidateReferences()
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning(
                    "05 문 안전 확인 퀘스트에서 DialogueManager를 찾을 수 없습니다.",
                    this);
            }

            if (_dangerHandleVisuals == null ||
                _dangerHandleVisuals.Length == 0)
            {
                Debug.LogWarning(
                    "05 문 안전 확인 구역에서 위험 손잡이 시각 감지기를 찾을 수 없습니다.",
                    this);
            }

            if (_doorHandles == null ||
                _doorHandles.Length == 0)
            {
                Debug.LogWarning(
                    "05 문 안전 확인 구역에서 문 손잡이 컨트롤러를 찾을 수 없습니다.",
                    this);
            }

            if (_doorControllers == null ||
                _doorControllers.Length == 0)
            {
                Debug.LogWarning(
                    "05 문 안전 확인 구역에서 문 컨트롤러를 찾을 수 없습니다.",
                    this);
            }
        }
    }
}
