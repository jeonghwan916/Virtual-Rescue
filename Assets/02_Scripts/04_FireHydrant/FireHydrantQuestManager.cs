using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Interactions;

namespace VirtualRescue.Missions05
{
    public enum FireHydrantQuestStep
    {
        Start,
        Alarm,
        Door,
        Hose,
        Valve,
        Finish,
        Completed
    }
    
    public sealed class FireHydrantQuestManager : MonoBehaviour
        {
            [Header("Dialogue")]
            [SerializeField] private DialogueManager _dialogueManager;
            [SerializeField] private string _startDialogueGroup = "start";
            [SerializeField] private string _alarmDialogueGroup = "alarm";
            [SerializeField] private string _doorDialogueGroup = "door";
            [SerializeField] private string _hoseDialogueGroup = "hose";
            [SerializeField] private string _valveDialogueGroup = "valve";
            [SerializeField] private string _finishDialogueGroup = "finish";
            [SerializeField] private bool _autoStartWhenNoPlayerReferenceHub = true;

            [Header("References")]
            [SerializeField] private FireBellButton _fireBellButton;
            [SerializeField] private XRGrabInteractable _nozzle;
            [SerializeField] private HoseDistanceLimiter _hoseDistanceLimiter;
            [SerializeField] private FireHoseValveLever _fireHoseValveLever;
            [SerializeField] private FireHose _fireHose;
            [SerializeField] private FireObject[] _fireObjects;
            private int _fireCnt = 0;
            
            private FireHydrantQuestStep _currentStep = FireHydrantQuestStep.Start;
            private FireHydrantQuestStep _nextStep;
            private string _activeDialogueGroup;
            private bool _isDialoguePlaying;
            private PlayerReferenceHub _playerReferenceHub;

            public FireHydrantQuestStep CurrentStep => _currentStep;
            public bool IsDialoguePlaying => _isDialoguePlaying;
            public bool IsCompleted => _currentStep == FireHydrantQuestStep.Completed;

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
                    TryAdvance(FireHydrantQuestStep.Start);
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
                
                if (_fireBellButton != null)
                {
                    _fireBellButton.Pressed += OnAlarmBellPressed;
                }

                if (_nozzle != null)
                {
                    _nozzle.selectEntered.AddListener(OnNozzleGrabbed);
                }

                if (_hoseDistanceLimiter != null)
                {
                    _hoseDistanceLimiter.DistanceThresholdReached += OnRoped;
                }

                if (_fireHoseValveLever != null)
                {
                    _fireHoseValveLever.Opened += OnValveOpened;
                }

                for (int i = 0; i < _fireObjects.Length; i++)
                {
                    if (_fireObjects[i] != null)
                    {
                        _fireObjects[i].OnExtinguished += OnFireExtinguished;
                    }
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
                
                if (_fireBellButton != null)
                {
                    _fireBellButton.Pressed -= OnAlarmBellPressed;
                }

                if (_nozzle != null)
                {
                    _nozzle.selectEntered.RemoveAllListeners();
                }

                if (_hoseDistanceLimiter != null)
                {
                    _hoseDistanceLimiter.DistanceThresholdReached -= OnRoped;
                }

                if (_fireHoseValveLever != null)
                {
                    _fireHoseValveLever.Opened -= OnValveOpened;
                }

                for (int i = 0; i < _fireObjects.Length; i++)
                {
                    if (_fireObjects[i] != null)
                    {
                        _fireObjects[i].OnExtinguished -= OnFireExtinguished;
                    }
                }
            }

            public bool TryAdvance(FireHydrantQuestStep questStep)
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
                    case FireHydrantQuestStep.Start:
                        PlayGroup(
                            _startDialogueGroup,
                            FireHydrantQuestStep.Alarm);
                        break;

                    case FireHydrantQuestStep.Alarm:
                        PlayGroup(
                            _alarmDialogueGroup,
                            FireHydrantQuestStep.Door);
                        break;

                    case FireHydrantQuestStep.Door:
                        PlayGroup(
                            _doorDialogueGroup,
                            FireHydrantQuestStep.Hose);
                        break;

                    case FireHydrantQuestStep.Hose:
                        PlayGroup(
                            _hoseDialogueGroup,
                            FireHydrantQuestStep.Valve);
                        break;
                    
                    case FireHydrantQuestStep.Valve:
                        PlayGroup(
                            _valveDialogueGroup,
                            FireHydrantQuestStep.Finish);
                        break;

                    case FireHydrantQuestStep.Finish:
                        PlayGroup(
                            _finishDialogueGroup,
                            FireHydrantQuestStep.Completed);
                        break;
                    
                    case FireHydrantQuestStep.Completed:
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
                FireHydrantQuestStep nextStep)
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
                TryAdvance(FireHydrantQuestStep.Start);
            }

            private void OnAlarmBellPressed()
            {
                TryAdvance(FireHydrantQuestStep.Alarm);
            }

            private void OnNozzleGrabbed(SelectEnterEventArgs args)
            {
                TryAdvance(FireHydrantQuestStep.Door);
            }
            
            private void OnRoped()
            {
                TryAdvance(FireHydrantQuestStep.Hose);
            }

            private void OnValveOpened()
            {
                TryAdvance(FireHydrantQuestStep.Valve);
            }

            private void OnFireExtinguished()
            {
                _fireCnt++;

                if (_fireCnt >= 3)
                {
                    TryAdvance(FireHydrantQuestStep.Finish);
                }
            }
        }
}
