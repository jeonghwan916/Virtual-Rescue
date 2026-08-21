using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Locations
{
    [DisallowMultipleComponent]
    public sealed class RoomSituationController : MonoBehaviour
    {
        [Header("Level 2 Timed Dialogue")]
        [SerializeField, Range(0f, 1f)]
        [Tooltip("남은 제한시간 비율이 이 값 이하가 되면 상황 대사를 자동 재생합니다.")]
        private float _level2DialogueRemainingTimeRatio = 0.75f;

        private RoomTrigger[] _roomTriggers;
        private bool _roomDialoguesSuppressed;
        private RoomTrigger _situationRoomTrigger;
        private SituationController _timedLevel2Controller;
        private SituationDefinition _timedLevel2Definition;
        private bool _areDayEntryDialoguesPrepared;

        [SerializeField] private SituationDiscoveryTracker _discoveryTracker;

        private void Awake()
        {
            _roomTriggers = GetComponentsInChildren<RoomTrigger>(true);

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.SituationEntryDialoguePlayed +=
                    HandleSituationEntryDialoguePlayed;
            }
        }

        private void OnDestroy()
        {
            if (_roomTriggers == null)
            {
                return;
            }

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.SituationEntryDialoguePlayed -=
                    HandleSituationEntryDialoguePlayed;
            }
        }

        public void PrepareDayEntryDialogues()
        {
            _roomDialoguesSuppressed = false;
            _situationRoomTrigger = null;
            _timedLevel2Controller = null;
            _timedLevel2Definition = null;
            _areDayEntryDialoguesPrepared = true;

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.PrepareDayEntryDialogue();
            }
        }

        public void ActivateDayEntryDialogues()
        {
            if (!_areDayEntryDialoguesPrepared)
            {
                Debug.LogWarning(
                    $"{name}: 날짜 진입 대사가 준비되기 전에 활성화가 요청되었습니다.",
                    this);
                return;
            }

            _areDayEntryDialoguesPrepared = false;

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.EnableDayEntryDialogue();
            }

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.TryPlayCurrentRoomEntryDialogue();
            }
        }

        public void SuppressEntryDialogues()
        {
            if (_roomDialoguesSuppressed)
            {
                return;
            }

            _roomDialoguesSuppressed = true;

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.SuppressEntryDialogue();
            }
        }

        public void Configure(
            SituationDefinition definition,
            SituationController controller)
        {
            if (definition == null ||
                definition.RoomLocation == RoomLocation.None)
            {
                return;
            }

            string dialogueId = GetSituationDialogueId(definition.Level);
            bool found = false;

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                if (roomTrigger.Location != definition.RoomLocation)
                {
                    continue;
                }

                roomTrigger.ConfigureSituation(dialogueId, definition.Level);
                _situationRoomTrigger ??= roomTrigger;
                found = true;
            }

            if (!found)
            {
                Debug.LogWarning(
                    $"{name}: {definition.RoomLocation}에 해당하는 RoomTrigger를 찾을 수 없습니다.",
                    this);
                return;
            }

            if (definition.Level == SituationLevel.Level2 &&
                definition.UsesTimeLimit &&
                controller != null)
            {
                _timedLevel2Controller = controller;
                _timedLevel2Definition = definition;
            }
        }

        private void Update()
        {
            if (_roomDialoguesSuppressed ||
                _situationRoomTrigger == null ||
                _timedLevel2Controller == null ||
                _timedLevel2Definition == null ||
                !_timedLevel2Controller.IsActive)
            {
                return;
            }

            float dialogueTriggerTime =
                _timedLevel2Definition.TimeLimitSeconds *
                _level2DialogueRemainingTimeRatio;

            if (_timedLevel2Controller.RemainingTime > dialogueTriggerTime)
            {
                return;
            }

            _situationRoomTrigger.TryPlaySituationEntryDialogue();
        }

        private static string GetSituationDialogueId(SituationLevel level)
        {
            return level switch
            {
                SituationLevel.Level0 => "Situation0",
                SituationLevel.Level1 => "Situation1",
                SituationLevel.Level2 => "Situation2",
                _ => string.Empty
            };
        }

        private void HandleSituationEntryDialoguePlayed(
            RoomTrigger source,
            SituationLevel situationLevel)
        {
            if (_roomDialoguesSuppressed ||
                !ShouldSuppressAfterSituationEntry(situationLevel))
            {
                return;
            }

            _discoveryTracker?.MarkDiscovered(situationLevel);
            SuppressEntryDialogues();
        }

        private static bool ShouldSuppressAfterSituationEntry(
            SituationLevel situationLevel)
        {
            return situationLevel == SituationLevel.Level1 ||
                   situationLevel == SituationLevel.Level2;
        }

        private void OnValidate()
        {
            _level2DialogueRemainingTimeRatio = Mathf.Clamp01(
                _level2DialogueRemainingTimeRatio);
        }
    }
}
