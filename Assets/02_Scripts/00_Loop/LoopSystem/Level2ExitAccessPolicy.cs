using UnityEngine;
using VirtualRescue.Missions09;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class Level2ExitAccessPolicy : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private DayFlowController _dayFlowController;
        [SerializeField] private SituationSceneLoader _situationSceneLoader;

        [Header("Emergency Stairs")]
        [SerializeField] private string _stairDoorId = "Exit_Stairs";

        private FireExitDoorController _stairDoor;
        private bool _stairDoorWasLocked;
        private bool _hasStairDoorOverride;

        public static Level2ExitAccessPolicy Instance { get; private set; }

        public bool IsLevel2Situation =>
            _situationSceneLoader != null &&
            _situationSceneLoader.CurrentDefinition != null &&
            _situationSceneLoader.CurrentDefinition.Level == SituationLevel.Level2;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError(
                    "Only one Level2ExitAccessPolicy may exist at a time.",
                    this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            if (_dayFlowController == null)
            {
                Debug.LogError(
                    $"{name}: DayFlowController is not assigned.",
                    this);
                return;
            }

            if (_situationSceneLoader == null)
            {
                Debug.LogError(
                    $"{name}: SituationSceneLoader is not assigned. " +
                    "Level 2 exits will remain unavailable.",
                    this);
            }

            _dayFlowController.StateChanged += HandleDayFlowStateChanged;

            if (_dayFlowController.CurrentState == DayFlowState.Playing)
            {
                ApplyStairDoorRestriction();
            }
        }

        private void OnDisable()
        {
            if (_dayFlowController != null)
            {
                _dayFlowController.StateChanged -= HandleDayFlowStateChanged;
            }

            RestoreStairDoor();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnValidate()
        {
            _stairDoorId = DoorRegistry.NormalizeId(_stairDoorId);
        }

        public bool CanUseLevel2Exit(ExitType exitType)
        {
            SituationDefinition definition =
                _situationSceneLoader != null
                    ? _situationSceneLoader.CurrentDefinition
                    : null;

            return _dayFlowController != null &&
                   _dayFlowController.CurrentState == DayFlowState.Playing &&
                   definition != null &&
                   definition.Level == SituationLevel.Level2 &&
                   definition.IsExitAllowed(exitType);
        }

        private void HandleDayFlowStateChanged(DayFlowState state)
        {
            if (state == DayFlowState.Playing)
            {
                ApplyStairDoorRestriction();
                return;
            }

            RestoreStairDoor();
        }

        private void ApplyStairDoorRestriction()
        {
            RestoreStairDoor();

            if (CanUseLevel2Exit(ExitType.EmergencyStairs))
            {
                return;
            }

            DoorRegistry registry = DoorRegistry.Instance;
            if (registry == null)
            {
                Debug.LogError($"{name}: DoorRegistry was not found.", this);
                return;
            }

            if (!registry.TryGetDoor(_stairDoorId, out FireExitDoorController door))
            {
                Debug.LogError(
                    $"{name}: Stair door ID '{_stairDoorId}' is not registered.",
                    this);
                return;
            }

            _stairDoor = door;
            _stairDoorWasLocked = door.IsLocked;
            _hasStairDoorOverride = true;
            door.SetLocked(true);
        }

        private void RestoreStairDoor()
        {
            if (!_hasStairDoorOverride)
            {
                return;
            }

            if (_stairDoor != null)
            {
                _stairDoor.SetLocked(_stairDoorWasLocked);
            }

            _stairDoor = null;
            _stairDoorWasLocked = false;
            _hasStairDoorOverride = false;
        }
    }
}
