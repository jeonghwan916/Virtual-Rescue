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
        [Header("Base Power Strip")]
        [SerializeField] private XRSocketInteractor _adapterASupplySocket;
        [SerializeField] private XRSocketInteractor _adapterBSupplySocket;

        [Header("T-Shaped Power Strip Chain")]
        [SerializeField] private XRSocketInteractor _adapterAToAdapterCSocket;

        [Header("Appliance Cord Sockets")]
        [SerializeField] private List<XRSocketInteractor> _adapterAApplianceSockets = new();
        [SerializeField] private List<XRSocketInteractor> _adapterBApplianceSockets = new();
        [SerializeField] private List<XRSocketInteractor> _adapterCApplianceSockets = new();

        private Coroutine _evaluationRoutine;
        private bool _hasObservedPoweredAppliance;

        public int PoweredAppliancePathCount => CountPoweredAppliancePaths();

        protected override void OnActivated()
        {
            _hasObservedPoweredAppliance = false;

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
            _hasObservedPoweredAppliance = false;
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
            if (!IsActive && !IsResolved)
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
            if (!IsActive && !IsResolved)
            {
                return;
            }

            int poweredAppliancePathCount = CountPoweredAppliancePaths();

            if (IsResolved)
            {
                if (poweredAppliancePathCount > 0 && !ReopenResolvedSituation())
                {
                    Debug.LogError(
                        "The resolved power strip unplug situation could not be reopened.",
                        this);
                }
            }

            if (poweredAppliancePathCount > 0)
            {
                _hasObservedPoweredAppliance = true;
                return;
            }

            if (!_hasObservedPoweredAppliance)
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

        private int CountPoweredAppliancePaths()
        {
            int poweredAppliancePathCount = 0;

            if (_adapterASupplySocket != null &&
                _adapterASupplySocket.hasSelection)
            {
                poweredAppliancePathCount += CountSelectedSockets(
                    _adapterAApplianceSockets);

                if (_adapterAToAdapterCSocket != null &&
                    _adapterAToAdapterCSocket.hasSelection)
                {
                    poweredAppliancePathCount += CountSelectedSockets(
                        _adapterCApplianceSockets);
                }
            }

            if (_adapterBSupplySocket != null &&
                _adapterBSupplySocket.hasSelection)
            {
                poweredAppliancePathCount += CountSelectedSockets(
                    _adapterBApplianceSockets);
            }

            return poweredAppliancePathCount;
        }

        private static int CountSelectedSockets(
            IReadOnlyList<XRSocketInteractor> sockets)
        {
            int selectedSocketCount = 0;

            foreach (XRSocketInteractor socket in sockets)
            {
                if (socket.hasSelection)
                {
                    selectedSocketCount++;
                }
            }

            return selectedSocketCount;
        }

        private bool TryValidateSockets()
        {
            List<XRSocketInteractor> allSockets = new()
            {
                _adapterASupplySocket,
                _adapterBSupplySocket,
                _adapterAToAdapterCSocket
            };

            AddSockets(allSockets, _adapterAApplianceSockets);
            AddSockets(allSockets, _adapterBApplianceSockets);
            AddSockets(allSockets, _adapterCApplianceSockets);

            if (allSockets.Count != 9 || allSockets.Contains(null) ||
                new HashSet<XRSocketInteractor>(allSockets).Count != allSockets.Count)
            {
                Debug.LogError(
                    "The power strip unplug situation requires nine unique socket references.",
                    this);
                return false;
            }

            return true;
        }

        private static void AddSockets(
            List<XRSocketInteractor> target,
            List<XRSocketInteractor> sockets)
        {
            if (sockets == null || sockets.Count != 2)
            {
                return;
            }

            target.AddRange(sockets);
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
            yield return _adapterASupplySocket;
            yield return _adapterBSupplySocket;
            yield return _adapterAToAdapterCSocket;

            if (_adapterAApplianceSockets != null)
            {
                foreach (XRSocketInteractor socket in _adapterAApplianceSockets)
                {
                    yield return socket;
                }
            }

            if (_adapterBApplianceSockets != null)
            {
                foreach (XRSocketInteractor socket in _adapterBApplianceSockets)
                {
                    yield return socket;
                }
            }

            if (_adapterCApplianceSockets != null)
            {
                foreach (XRSocketInteractor socket in _adapterCApplianceSockets)
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
