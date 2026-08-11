using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class DayOutcomeController : MonoBehaviour
    {
        [SerializeField] private DayFlowController _dayFlowController = null;
        [SerializeField] private SituationSceneLoader _situationSceneLoader = null;

        private SituationController _boundSituationController;

        public string LastError { get; private set; } = string.Empty;

        private void OnEnable()
        {
            ExitController.ExitRequested += HandleExitRequested;

            if (_dayFlowController != null)
            {
                _dayFlowController.StateChanged += HandleDayFlowStateChanged;

                if (_dayFlowController.CurrentState == DayFlowState.Playing)
                {
                    BindCurrentSituation();
                }
            }
        }

        private void OnDisable()
        {
            ExitController.ExitRequested -= HandleExitRequested;

            if (_dayFlowController != null)
            {
                _dayFlowController.StateChanged -= HandleDayFlowStateChanged;
            }

            UnbindCurrentSituation();
        }

        public bool TryEvaluateExit(ExitType exitType)
        {
            LastError = string.Empty;

            if (!TryValidateReferences())
            {
                return false;
            }

            if (_dayFlowController.CurrentState != DayFlowState.Playing)
            {
                return Fail(
                    $"Exit request is not allowed while day flow state is " +
                    $"{_dayFlowController.CurrentState}.");
            }

            SituationDefinition definition = _situationSceneLoader.CurrentDefinition;
            SituationController controller = _situationSceneLoader.CurrentController;

            if (_dayFlowController.IsEndingDay)
            {
                return TryCompleteEnding(exitType, definition, controller);
            }

            if (definition == null && controller == null)
            {
                return exitType == ExitType.Elevator
                    ? CompleteDay(definition)
                    : FailDay(definition);
            }

            if (definition == null || controller == null)
            {
                return Fail(
                    "Situation definition and controller must either both exist or both be absent.");
            }

            if (controller.IsFailed)
            {
                return FailDay(definition);
            }

            if (definition.Level == SituationLevel.Level2)
            {
                if (!definition.IsExitAllowed(exitType))
                {
                    return FailDay(definition);
                }

                if (controller.IsActive && !controller.TryResolveByExit(exitType))
                {
                    return FailDay(definition);
                }

                return controller.IsResolved
                    ? CompleteDay(definition)
                    : FailDay(definition);
            }

            if (!controller.IsResolved)
            {
                return FailDay(definition);
            }

            return definition.IsExitAllowed(exitType)
                ? CompleteDay(definition)
                : FailDay(definition);
        }

        private void HandleExitRequested(ExitType exitType)
        {
            TryEvaluateExit(exitType);
        }

        private void HandleDayFlowStateChanged(DayFlowState state)
        {
            if (state == DayFlowState.Playing)
            {
                BindCurrentSituation();
                return;
            }

            UnbindCurrentSituation();
        }

        private void HandleSituationFailed()
        {
            if (_dayFlowController == null ||
                _dayFlowController.CurrentState != DayFlowState.Playing)
            {
                return;
            }

            SituationDefinition definition =
                _situationSceneLoader != null
                    ? _situationSceneLoader.CurrentDefinition
                    : null;

            if (!_dayFlowController.FailDay(DayResultContext.Failed(definition)))
            {
                Fail("Day flow rejected a situation failure result.");
            }
        }

        private void BindCurrentSituation()
        {
            UnbindCurrentSituation();

            if (_situationSceneLoader == null)
            {
                return;
            }

            _boundSituationController = _situationSceneLoader.CurrentController;

            if (_boundSituationController != null)
            {
                _boundSituationController.Failed += HandleSituationFailed;
            }
        }

        private void UnbindCurrentSituation()
        {
            if (_boundSituationController == null)
            {
                return;
            }

            _boundSituationController.Failed -= HandleSituationFailed;
            _boundSituationController = null;
        }

        private bool CompleteDay(SituationDefinition definition)
        {
            if (_dayFlowController.CompleteDay(
                    DayResultContext.Completed(definition)))
            {
                return true;
            }

            return Fail("Day flow rejected a successful day result.");
        }

        private bool TryCompleteEnding(
            ExitType exitType,
            SituationDefinition definition,
            SituationController controller)
        {
            if (definition == null || controller == null)
            {
                return Fail("Ending situation must be loaded before requesting an exit.");
            }

            if (exitType != ExitType.Elevator)
            {
                return Fail("Only the elevator can complete the ending day.");
            }

            if (_dayFlowController.CompleteGame(
                    DayResultContext.Completed(definition)))
            {
                return true;
            }

            return Fail("Day flow rejected the ending result.");
        }

        private bool FailDay(SituationDefinition definition)
        {
            if (_dayFlowController.FailDay(DayResultContext.Failed(definition)))
            {
                return true;
            }

            return Fail("Day flow rejected a failed day result.");
        }

        private bool TryValidateReferences()
        {
            if (_dayFlowController == null)
            {
                return Fail("DayFlowController is not assigned.");
            }

            if (_situationSceneLoader == null)
            {
                return Fail("SituationSceneLoader is not assigned.");
            }

            return true;
        }

        private bool Fail(string message)
        {
            LastError = message;
            Debug.LogError($"DayOutcomeController: {message}", this);
            return false;
        }
    }
}
