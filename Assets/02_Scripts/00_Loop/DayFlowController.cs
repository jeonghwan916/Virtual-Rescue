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

        public event Action<DayFlowState> StateChanged;
        public event Action<int> DayStarted;
        public event Action<DayTransitionReason, int> TransitionRequested;
        public event Action GameCleared;

        public int CurrentDay => _runState?.CurrentDay ?? DayRunState.FirstDay;
        public DayFlowState CurrentState => _currentState;
        public bool IsGameCleared => _runState?.IsGameCleared ?? false;
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
            if (_currentState != DayFlowState.Preparing || IsGameCleared)
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
            if (_currentState != DayFlowState.Playing)
            {
                return false;
            }

            SetState(DayFlowState.Transitioning);
            _runState.AdvanceDay();

            if (_runState.IsGameCleared)
            {
                SetState(DayFlowState.Cleared);
                GameCleared?.Invoke();
                return true;
            }

            TransitionRequested?.Invoke(DayTransitionReason.DayCompleted, CurrentDay);
            return true;
        }

        public bool FailDay()
        {
            if (_currentState != DayFlowState.Playing)
            {
                return false;
            }

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
