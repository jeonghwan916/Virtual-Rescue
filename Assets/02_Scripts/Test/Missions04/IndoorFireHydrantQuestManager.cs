using System.Collections;
using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.Missions04
{
    public enum IndoorFireHydrantQuestStep
    {
        WaitingForStart,
        WaitingForCabinetDoor,
        WaitingForValve,
        WaitingForHoseGrab,
        WaitingForWater,
        WaitingForExtinguishing,
        Completed
    }

    public sealed class IndoorFireHydrantQuestManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private HydrantCabinetDoorController _cabinetDoor;
        [SerializeField] private FireHoseValveLever _valveLever;
        [SerializeField] private FireHose _fireHose;
        [SerializeField] private FireObject _fireObject;
        [SerializeField] private bool _autoStartWhenNoPlayerReferenceHub = true;

        [Header("Dialogue Groups")]
        [SerializeField] private string _startDialogueGroup = "quest04_start";
        [SerializeField] private string _doorDialogueGroup = "quest04_door";
        [SerializeField] private string _valveDialogueGroup = "quest04_valve";
        [SerializeField] private string _hoseDialogueGroup = "quest04_hose";
        [SerializeField] private string _finishDialogueGroup = "quest04_finish";

        private IndoorFireHydrantQuestStep _currentStep = IndoorFireHydrantQuestStep.WaitingForStart;
        private PlayerReferenceHub _playerReferenceHub;

        public IndoorFireHydrantQuestStep CurrentStep => _currentStep;

        private void Awake()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (_cabinetDoor == null)
            {
                _cabinetDoor = GetComponentInChildren<HydrantCabinetDoorController>(true);
            }

            if (_valveLever == null)
            {
                _valveLever = GetComponentInChildren<FireHoseValveLever>(true);
            }

            if (_fireHose == null)
            {
                _fireHose = GetComponentInChildren<FireHose>(true);
            }

            if (_fireObject == null)
            {
                _fireObject = GetComponentInChildren<FireObject>(true);
            }

            ValidateReferences();
            BindPlayerReferences();
        }

        private IEnumerator Start()
        {
            yield return null;

            if (_autoStartWhenNoPlayerReferenceHub && PlayerReferenceHub.Instance == null)
            {
                TryStartQuest();
            }
        }

        private void OnEnable()
        {
            BindPlayerReferences();

            if (_cabinetDoor != null)
            {
                _cabinetDoor.Opened += HandleCabinetDoorOpened;
            }

            if (_valveLever != null)
            {
                _valveLever.Opened += HandleValveOpened;
            }

            if (_fireHose != null)
            {
                _fireHose.Grabbed += HandleHoseGrabbed;
                _fireHose.FiringStarted += HandleFiringStarted;
            }

            if (_fireObject != null)
            {
                _fireObject.OnExtinguished += HandleFireExtinguished;
            }

            if (_playerReferenceHub != null)
            {
                _playerReferenceHub.SceneReady += HandleSceneReady;
            }
        }

        private void OnDisable()
        {
            if (_cabinetDoor != null)
            {
                _cabinetDoor.Opened -= HandleCabinetDoorOpened;
            }

            if (_valveLever != null)
            {
                _valveLever.Opened -= HandleValveOpened;
            }

            if (_fireHose != null)
            {
                _fireHose.Grabbed -= HandleHoseGrabbed;
                _fireHose.FiringStarted -= HandleFiringStarted;
            }

            if (_fireObject != null)
            {
                _fireObject.OnExtinguished -= HandleFireExtinguished;
            }

            if (_playerReferenceHub != null)
            {
                _playerReferenceHub.SceneReady -= HandleSceneReady;
                _playerReferenceHub = null;
            }
        }

        public bool TryStartQuest()
        {
            if (_currentStep != IndoorFireHydrantQuestStep.WaitingForStart || _dialogueManager == null)
            {
                return false;
            }

            _dialogueManager.Stop();
            _dialogueManager.PlayGroup(_startDialogueGroup);
            _currentStep = IndoorFireHydrantQuestStep.WaitingForCabinetDoor;
            return true;
        }

        private void BindPlayerReferences()
        {
            _playerReferenceHub = PlayerReferenceHub.Instance;
        }

        private void HandleSceneReady()
        {
            TryStartQuest();
        }

        private void HandleCabinetDoorOpened()
        {
            if (_currentStep != IndoorFireHydrantQuestStep.WaitingForCabinetDoor || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_doorDialogueGroup);
            _currentStep = IndoorFireHydrantQuestStep.WaitingForValve;
        }

        private void HandleValveOpened()
        {
            if (_currentStep != IndoorFireHydrantQuestStep.WaitingForValve || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_valveDialogueGroup);
            _currentStep = IndoorFireHydrantQuestStep.WaitingForHoseGrab;
        }

        private void HandleHoseGrabbed()
        {
            if (_currentStep != IndoorFireHydrantQuestStep.WaitingForHoseGrab || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_hoseDialogueGroup);
            _currentStep = IndoorFireHydrantQuestStep.WaitingForWater;
        }

        private void HandleFiringStarted()
        {
            if (_currentStep == IndoorFireHydrantQuestStep.WaitingForWater)
            {
                _currentStep = IndoorFireHydrantQuestStep.WaitingForExtinguishing;
            }
        }

        private void HandleFireExtinguished()
        {
            if (_currentStep != IndoorFireHydrantQuestStep.WaitingForExtinguishing || _dialogueManager == null)
            {
                return;
            }

            _dialogueManager.PlayGroup(_finishDialogueGroup);
            _currentStep = IndoorFireHydrantQuestStep.Completed;
        }

        private void ValidateReferences()
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("04 옥내소화전 퀘스트에서 DialogueManager를 찾을 수 없습니다.", this);
            }

            if (_cabinetDoor == null)
            {
                Debug.LogWarning("04 옥내소화전 구역에서 캐비닛 문 컨트롤러를 찾을 수 없습니다.", this);
            }

            if (_valveLever == null)
            {
                Debug.LogWarning("04 옥내소화전 구역에서 밸브 레버를 찾을 수 없습니다.", this);
            }

            if (_fireHose == null)
            {
                Debug.LogWarning("04 옥내소화전 구역에서 소방 호스를 찾을 수 없습니다.", this);
            }

            if (_fireObject == null)
            {
                Debug.LogWarning("04 옥내소화전 구역에서 FireObject를 찾을 수 없습니다.", this);
            }
        }
    }
}
