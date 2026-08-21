using UnityEngine;
using VirtualRescue.GameFlow;
using VirtualRescue.Situations.ExitFailure;

namespace VirtualRescue.Situations
{
    [DisallowMultipleComponent]
    public sealed class EntireHouseSmokeSituationController : SituationController
    {
        [SerializeField] private ExitFailureSequencePlayer _elevatorFailureSequence;

        private void OnEnable()
        {
            ElevatorTrigger.DoorOpeningStarted += HandleElevatorDoorOpeningStarted;

            if (_elevatorFailureSequence != null)
            {
                _elevatorFailureSequence.Completed += HandleFailureSequenceCompleted;
            }
        }

        private void OnDisable()
        {
            ElevatorTrigger.DoorOpeningStarted -= HandleElevatorDoorOpeningStarted;

            if (_elevatorFailureSequence != null)
            {
                _elevatorFailureSequence.Completed -= HandleFailureSequenceCompleted;
            }
        }

        public override bool TryConsumeExitAttempt(ExitType exitType)
        {
            if (!IsActive || exitType != ExitType.Elevator)
            {
                return false;
            }

            return true;
        }

        protected override void OnReset()
        {
            _elevatorFailureSequence?.ResetSequence();
        }

        private void HandleFailureSequenceCompleted()
        {
            FailSituation();
        }

        private void HandleElevatorDoorOpeningStarted()
        {
            if (!IsActive)
            {
                return;
            }

            if (_elevatorFailureSequence == null)
            {
                Debug.LogError(
                    "Elevator failure sequence is not assigned.",
                    this);
                return;
            }

            if (!_elevatorFailureSequence.IsPlaying)
            {
                StopCountdown();
                _elevatorFailureSequence.TryPlay();
            }
        }
    }
}
