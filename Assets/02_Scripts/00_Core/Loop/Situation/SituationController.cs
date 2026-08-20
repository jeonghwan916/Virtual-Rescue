using System;
using System.Collections;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    public enum SituationState
    {
        Inactive = 0,
        Active = 1,
        Resolved = 2,
        Failed = 3
    }

    [DisallowMultipleComponent]
    public abstract class SituationController : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private SituationState _state = SituationState.Inactive;
        [SerializeField] private float _remainingTime;

        private Coroutine _countdownRoutine;

        public event Action Activated;
        public event Action Resolved;
        public event Action WarningRaised;
        public event Action Failed;
        public event Action ResetPerformed;

        public SituationDefinition Definition { get; private set; }
        public SituationState State => _state;
        public float RemainingTime => _remainingTime;
        public bool IsActive => _state == SituationState.Active;
        public bool IsResolved => _state == SituationState.Resolved;
        public bool IsFailed => _state == SituationState.Failed;

        public bool Activate(SituationDefinition definition)
        {
            if (_state != SituationState.Inactive)
            {
                return false;
            }

            if (definition == null)
            {
                Debug.LogError("Situation definition is not assigned.", this);
                return false;
            }

            if (definition.UsesTimeLimit && definition.TimeLimitSeconds <= 0f)
            {
                Debug.LogError(
                    $"Situation '{definition.Id}' requires a positive time limit.",
                    this);
                return false;
            }

            Definition = definition;
            _state = SituationState.Active;
            _remainingTime = definition.TimeLimitSeconds;

            OnActivated();
            Activated?.Invoke();

            if (_state == SituationState.Active && definition.UsesTimeLimit)
            {
                _countdownRoutine = StartCoroutine(CountdownRoutine());
            }

            return true;
        }

        public void ResetSituation()
        {
            StopCountdown();
            _state = SituationState.Inactive;
            _remainingTime = 0f;
            OnReset();
            ResetPerformed?.Invoke();
            Definition = null;
        }

        public bool TryResolveByExit(ExitType exitType)
        {
            if (_state != SituationState.Active ||
                Definition == null ||
                Definition.Level != SituationLevel.Level2 ||
                !Definition.IsExitAllowed(exitType))
            {
                return false;
            }

            return ResolveSituation();
        }

        protected bool ResolveSituation()
        {
            if (_state != SituationState.Active)
            {
                return false;
            }

            StopCountdown();
            _state = SituationState.Resolved;
            _remainingTime = 0f;
            OnResolved();
            Resolved?.Invoke();
            return true;
        }

        protected bool FailSituation()
        {
            if (_state != SituationState.Active)
            {
                return false;
            }

            StopCountdown();
            _state = SituationState.Failed;
            _remainingTime = 0f;
            OnFailed();
            Failed?.Invoke();
            return true;
        }

        protected bool RaiseWarning()
        {
            if (_state != SituationState.Active)
            {
                return false;
            }

            WarningRaised?.Invoke();
            return true;
        }

        // 해결 후 위험 상태가 재발하면, 퇴장 전까지 다시 안전 조치를 할 수 있게 한다.
        protected bool ReopenResolvedSituation()
        {
            if (_state != SituationState.Resolved)
            {
                return false;
            }

            _state = SituationState.Active;
            return true;
        }

        protected virtual void OnActivated()
        {
        }

        protected virtual void OnResolved()
        {
        }

        protected virtual void OnFailed()
        {
        }

        protected virtual void OnReset()
        {
        }

        private IEnumerator CountdownRoutine()
        {
            while (_state == SituationState.Active && _remainingTime > 0f)
            {
                _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
                yield return null;
            }

            _countdownRoutine = null;

            if (_state == SituationState.Active)
            {
                FailSituation();
            }
        }

        private void StopCountdown()
        {
            if (_countdownRoutine == null)
            {
                return;
            }

            StopCoroutine(_countdownRoutine);
            _countdownRoutine = null;
        }
    }
}
