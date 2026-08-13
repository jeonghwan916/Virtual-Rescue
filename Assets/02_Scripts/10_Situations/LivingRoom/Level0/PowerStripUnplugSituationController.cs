using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations.PowerStripUnplug
{
    [DisallowMultipleComponent]
    public sealed class PowerStripUnplugSituationController : SituationController
    {
        [Header("Base Power Strip")]
        [SerializeField] private XRSocketInteractor _firstTStripBaseSocket;
        [SerializeField] private XRSocketInteractor _secondTStripBaseSocket;

        [Header("T-Shaped Power Strip Cord Sockets")]
        [SerializeField] private List<XRSocketInteractor> _firstTStripCordSockets = new();
        [SerializeField] private List<XRSocketInteractor> _secondTStripCordSockets = new();

        private Coroutine _evaluationRoutine;
        private bool _hasObservedPoweredConnection;

        public int ConnectedAppliancePathCount => CountConnectedAppliancePaths();

        protected override void OnActivated()
        {
            _hasObservedPoweredConnection = false;

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
            _hasObservedPoweredConnection = false;
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

            int connectedAppliancePathCount = CountConnectedAppliancePaths();

            if (connectedAppliancePathCount > 0)
            {
                _hasObservedPoweredConnection = true;
                return;
            }

            if (!_hasObservedPoweredConnection)
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

        private int CountConnectedAppliancePaths()
        {
            return CountConnectedAppliancePaths(
                _firstTStripBaseSocket != null &&
                    _firstTStripBaseSocket.hasSelection,
                CountSelectedSockets(_firstTStripCordSockets),
                _secondTStripBaseSocket != null &&
                    _secondTStripBaseSocket.hasSelection,
                CountSelectedSockets(_secondTStripCordSockets));
        }

        private static int CountConnectedAppliancePaths(
            bool isFirstTStripConnected,
            int firstCordCount,
            bool isSecondTStripConnected,
            int secondCordCount)
        {
            return (isFirstTStripConnected ? firstCordCount : 0) +
                (isSecondTStripConnected ? secondCordCount : 0);
        }

        private static int CountSelectedSockets(
            IReadOnlyList<XRSocketInteractor> sockets)
        {
            int selectedSocketCount = 0;

            foreach (XRSocketInteractor socket in sockets)
            {
                if (socket != null && socket.hasSelection)
                {
                    selectedSocketCount++;
                }
            }

            return selectedSocketCount;
        }

        private bool TryValidateSockets()
        {
            if (_firstTStripBaseSocket == null ||
                _secondTStripBaseSocket == null)
            {
                Debug.LogError(
                    "Both base power strip sockets must be assigned.",
                    this);
                return false;
            }

            if (!HasThreeValidSockets(_firstTStripCordSockets) ||
                !HasThreeValidSockets(_secondTStripCordSockets))
            {
                Debug.LogError(
                    "Each T-shaped power strip must have exactly three valid cord sockets.",
                    this);
                return false;
            }

            return true;
        }

        private static bool HasThreeValidSockets(
            IReadOnlyList<XRSocketInteractor> sockets)
        {
            return sockets != null &&
                sockets.Count == 3 &&
                sockets.All(socket => socket != null) &&
                sockets.Distinct().Count() == sockets.Count;
        }

        private void SubscribeSocketEvents()
        {
            foreach (XRSocketInteractor socket in GetAllObservedSockets())
            {
                socket.selectEntered.RemoveListener(HandleSocketSelectionChanged);
                socket.selectExited.RemoveListener(HandleSocketSelectionChanged);
                socket.selectEntered.AddListener(HandleSocketSelectionChanged);
                socket.selectExited.AddListener(HandleSocketSelectionChanged);
            }
        }

        private void UnsubscribeSocketEvents()
        {
            foreach (XRSocketInteractor socket in GetAllObservedSockets())
            {
                if (socket == null)
                {
                    continue;
                }

                socket.selectEntered.RemoveListener(HandleSocketSelectionChanged);
                socket.selectExited.RemoveListener(HandleSocketSelectionChanged);
            }
        }

        private IEnumerable<XRSocketInteractor> GetAllObservedSockets()
        {
            if (_firstTStripBaseSocket != null)
            {
                yield return _firstTStripBaseSocket;
            }

            if (_secondTStripBaseSocket != null)
            {
                yield return _secondTStripBaseSocket;
            }

            if (_firstTStripCordSockets != null)
            {
                foreach (XRSocketInteractor socket in _firstTStripCordSockets)
                {
                    yield return socket;
                }
            }

            if (_secondTStripCordSockets != null)
            {
                foreach (XRSocketInteractor socket in _secondTStripCordSockets)
                {
                    yield return socket;
                }
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
