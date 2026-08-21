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
            if (_elevatorFailureSequence != null)
            {
                _elevatorFailureSequence.Completed += HandleFailureSequenceCompleted;
            }
        }

        private void OnDisable()
        {
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

            if (_elevatorFailureSequence == null)
            {
                Debug.LogError(
                    "Elevator failure sequence is not assigned.",
                    this);
                return false;
            }

            if (!_elevatorFailureSequence.IsPlaying)
            {
                StopCountdown();
                _elevatorFailureSequence.TryPlay();
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
    }
}
