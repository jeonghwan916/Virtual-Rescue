using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations.PowerStripUnplug
{
    [DisallowMultipleComponent]
    public sealed class PowerStripUnplugSituationController : SituationController
    {
        [Header("References")]
        [SerializeField] private List<XRSocketInteractor> _sockets = new();

        private Coroutine _evaluationRoutine;
        private bool _hasObservedConnection;

        public int ConnectedSocketCount => CountConnectedSockets();

        protected override void OnActivated()
        {
            _hasObservedConnection = false;

            if (!TryValidateSockets())
            {
                return;
            }

            SubscribeSocketEvents();
            ScheduleEvaluation();
        }

        protected override void OnResolved()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
        }

        protected override void OnFailed()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
        }

        protected override void OnReset()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
            _hasObservedConnection = false;
        }

        private void OnDisable()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
        }

        private void HandleSocketSelectionChanged(SelectEnterEventArgs args)
        {
            ScheduleEvaluation();
        }

        private void HandleSocketSelectionChanged(SelectExitEventArgs args)
        {
            ScheduleEvaluation();
        }

        private void ScheduleEvaluation()
        {
            if (!IsActive)
            {
                return;
            }

            StopEvaluation();
            _evaluationRoutine = StartCoroutine(EvaluateNextFrameRoutine());
        }

        private IEnumerator EvaluateNextFrameRoutine()
        {
            yield return null;

            _evaluationRoutine = null;
            EvaluateSocketState();
        }

        private void EvaluateSocketState()
        {
            if (!IsActive)
            {
                return;
            }

            int connectedSocketCount = CountConnectedSockets();

            if (connectedSocketCount > 0)
            {
                _hasObservedConnection = true;
                return;
            }

            if (!_hasObservedConnection)
            {
                return;
            }

            if (!ResolveSituation())
            {
                Debug.LogError(
                    "The power strip unplug situation could not be resolved.",
                    this);
            }
        }

        private int CountConnectedSockets()
        {
            int connectedSocketCount = 0;

            foreach (XRSocketInteractor socket in _sockets)
            {
                if (socket != null && socket.hasSelection)
                {
                    connectedSocketCount++;
                }
            }

            return connectedSocketCount;
        }

        private bool TryValidateSockets()
        {
            if (_sockets == null || _sockets.Count == 0)
            {
                Debug.LogError(
                    "At least one power strip socket must be assigned.",
                    this);
                return false;
            }

            foreach (XRSocketInteractor socket in _sockets)
            {
                if (socket == null)
                {
                    Debug.LogError(
                        "Power strip socket references cannot contain missing entries.",
                        this);
                    return false;
                }
            }

            return true;
        }

        private void SubscribeSocketEvents()
        {
            foreach (XRSocketInteractor socket in _sockets)
            {
                socket.selectEntered.RemoveListener(HandleSocketSelectionChanged);
                socket.selectExited.RemoveListener(HandleSocketSelectionChanged);
                socket.selectEntered.AddListener(HandleSocketSelectionChanged);
                socket.selectExited.AddListener(HandleSocketSelectionChanged);
            }
        }

        private void UnsubscribeSocketEvents()
        {
            if (_sockets == null)
            {
                return;
            }

            foreach (XRSocketInteractor socket in _sockets)
            {
                if (socket == null)
                {
                    continue;
                }

                socket.selectEntered.RemoveListener(HandleSocketSelectionChanged);
                socket.selectExited.RemoveListener(HandleSocketSelectionChanged);
            }
        }

        private void StopEvaluation()
        {
            if (_evaluationRoutine == null)
            {
                return;
            }

            StopCoroutine(_evaluationRoutine);
            _evaluationRoutine = null;
        }
    }
}
