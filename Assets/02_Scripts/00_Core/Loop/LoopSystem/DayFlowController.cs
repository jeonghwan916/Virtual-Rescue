using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    public enum DayFlowState
    {
        Preparing = 0,
        LoadingHome = 1,
        Playing = 2,
        Transitioning = 3,
        Cleared = 4
    }

    public enum DayTransitionReason
    {
        DayCompleted = 0,
        RunFailed = 1
    }

    [DisallowMultipleComponent]
    public sealed class DayFlowController : MonoBehaviour
    {
        [SerializeField] private bool _startAutomatically = true;

        private DayRunState _runState;
        private DayFlowState _currentState = DayFlowState.Preparing;
        private DayResultContext _lastDayResult = DayResultContext.None;

        public event Action<DayFlowState> StateChanged;
        public event Action<int> DayStarted;
        public event Action<DayTransitionReason, int> TransitionRequested;
        public event Action GameCleared;

        public int CurrentDay => _runState?.CurrentDay ?? DayRunState.FirstDay;
        public DayFlowState CurrentState => _currentState;
        public DayResultContext LastDayResult => _lastDayResult;
        public bool IsEndingDay => _runState?.IsEndingDay ?? false;
        public bool IsGameCleared => _currentState == DayFlowState.Cleared;
        public IReadOnlyCollection<string> SeenSituationIds =>
            _runState?.SeenSituationIds ?? Array.Empty<string>();

        private void Awake()
        {
            _runState = new DayRunState();
        }

        private void Start()
        {
            if (_startAutomatically)
            {
                TryStartDay();
            }
        }

        public bool TryStartDay()
        {
            if (_currentState != DayFlowState.Preparing)
            {
                return false;
            }

            SetState(DayFlowState.LoadingHome);
            DayStarted?.Invoke(CurrentDay);
            return true;
        }

        public bool NotifyHomeLoaded()
        {
            if (_currentState != DayFlowState.LoadingHome)
            {
                return false;
            }

            SetState(DayFlowState.Playing);
            return true;
        }

        public bool HasSeenSituation(string situationId)
        {
            return _runState != null && _runState.HasSeenSituation(situationId);
        }

        public bool TryRegisterSituation(SituationDefinition definition)
        {
            if (_currentState != DayFlowState.LoadingHome || definition == null)
            {
                return false;
            }

            return _runState.TryRegisterSituation(definition.Id);
        }

        public bool CompleteDay()
        {
            return CompleteDay(DayResultContext.Completed(null));
        }

        public bool CompleteDay(DayResultContext resultContext)
        {
            if (_currentState != DayFlowState.Playing || IsEndingDay)
            {
                return false;
            }

            if (!_runState.AdvanceDay())
            {
                return false;
            }

            _lastDayResult = resultContext;
            SetState(DayFlowState.Transitioning);
            TransitionRequested?.Invoke(DayTransitionReason.DayCompleted, CurrentDay);
            return true;
        }

        public bool CompleteGame(DayResultContext resultContext)
        {
            if (_currentState != DayFlowState.Playing || !IsEndingDay)
            {
                return false;
            }

            _lastDayResult = resultContext;
            SetState(DayFlowState.Cleared);
            GameCleared?.Invoke();
            return true;
        }

        public bool TransitionToDayForDebug(int targetDay)
        {
            if (_currentState != DayFlowState.Playing ||
                targetDay == CurrentDay ||
                !_runState.TrySetCurrentDay(targetDay))
            {
                return false;
            }

            _lastDayResult = DayResultContext.None;
            SetState(DayFlowState.Transitioning);
            TransitionRequested?.Invoke(DayTransitionReason.DayCompleted, CurrentDay);
            return true;
        }

        public bool FailDay()
        {
            return FailDay(DayResultContext.Failed(null));
        }

        public bool FailDay(DayResultContext resultContext)
        {
            if (_currentState != DayFlowState.Playing)
            {
                return false;
            }

            _lastDayResult = resultContext;
            SetState(DayFlowState.Transitioning);
            _runState.ResetRun();
            TransitionRequested?.Invoke(DayTransitionReason.RunFailed, CurrentDay);
            return true;
        }

        public bool NotifyTransitionCompleted()
        {
            if (_currentState != DayFlowState.Transitioning)
            {
                return false;
            }

            SetState(DayFlowState.Preparing);
            return TryStartDay();
        }

        private void SetState(DayFlowState nextState)
        {
            if (_currentState == nextState)
            {
                return;
            }

            _currentState = nextState;
            StateChanged?.Invoke(_currentState);
        }
    }
}
