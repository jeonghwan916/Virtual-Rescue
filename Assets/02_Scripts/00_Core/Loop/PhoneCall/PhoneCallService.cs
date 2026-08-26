using UnityEngine;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Locations;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class PhoneCallService : MonoBehaviour
    {
        private const string WrongCallGroupId = "Wrong_Call";

        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private SituationSceneLoader _situationSceneLoader;
        [SerializeField] private SituationDiscoveryTracker _discoveryTracker;
        [SerializeField] private RoomSituationController _roomSituationController;

        private CellPhoneEndpoint _endpoint;
        private bool _isCallInProgress;
        private bool _shouldRequestExitAfterDialogue;
        private bool _isCallDisplayLocked;
        private bool _hasCalledBeforeResolve;
        private string _pendingGroupId = string.Empty;
        private CellPhoneContact _callingContact = CellPhoneContact.Emergency119;
        private SituationController _boundSituationController;

        private void OnEnable()
        {
            CellPhoneEndpointRegistry.EndpointRegistered += HandleEndpointRegistered;
            CellPhoneEndpointRegistry.EndpointUnregistered += HandleEndpointUnregistered;

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted += HandleDialogueGroupCompleted;
            }

            if (_situationSceneLoader != null)
            {
                _situationSceneLoader.SituationLoaded += HandleSituationLoaded;
                _situationSceneLoader.SituationUnloaded += HandleSituationUnloaded;
            }
        }

        private void OnDisable()
        {
            CellPhoneEndpointRegistry.EndpointRegistered -= HandleEndpointRegistered;
            CellPhoneEndpointRegistry.EndpointUnregistered -= HandleEndpointUnregistered;

            if (_dialogueManager != null)
            {
                _dialogueManager.GroupCompleted -= HandleDialogueGroupCompleted;
            }

            if (_situationSceneLoader != null)
            {
                _situationSceneLoader.SituationLoaded -= HandleSituationLoaded;
                _situationSceneLoader.SituationUnloaded -= HandleSituationUnloaded;
            }

            UnbindSituation();
            UnbindEndpoint(_endpoint);
            ResetCallState();
        }

        private void HandleEndpointRegistered(CellPhoneEndpoint endpoint)
        {
            if (endpoint == null)
            {
                return;
            }

            if (_endpoint == endpoint)
            {
                return;
            }

            if (_endpoint != null && _endpoint != endpoint)
            {
                UnbindEndpoint(_endpoint);
            }

            _endpoint = endpoint;
            if (_endpoint.Screen == null)
            {
                Debug.LogWarning(
                    $"{endpoint.name}: CellPhoneScreen is not assigned.",
                    endpoint);
                return;
            }

            _endpoint.Screen.ScreenOpened += HandleScreenOpened;
            _endpoint.Screen.ScreenClosed += HandleScreenClosed;
            _endpoint.Screen.CallRequested += HandleCallRequested;
            BindSituation(_situationSceneLoader?.CurrentController);
            RefreshScreenDisplay();
        }

        private void HandleEndpointUnregistered(CellPhoneEndpoint endpoint)
        {
            if (endpoint == null || endpoint != _endpoint)
            {
                return;
            }

            UnbindEndpoint(endpoint);
            _endpoint = null;
        }

        private void UnbindEndpoint(CellPhoneEndpoint endpoint)
        {
            if (endpoint == null)
            {
                return;
            }

            if (endpoint.Screen != null)
            {
                endpoint.Screen.ScreenOpened -= HandleScreenOpened;
                endpoint.Screen.ScreenClosed -= HandleScreenClosed;
                endpoint.Screen.CallRequested -= HandleCallRequested;
            }

        }

        private void HandleCallRequested()
        {
            if (_isCallInProgress)
            {
                return;
            }

            if (!CanHandlePhoneCall())
            {
                return;
            }

            _roomSituationController?.SuppressEntryDialogues();

            PhoneCallAction action = EvaluatePhoneCallAction();
            if (action.RequestExitImmediately)
            {
                LockCallDisplay();
                RequestExit();
                return;
            }

            if (string.IsNullOrWhiteSpace(action.DialogueGroupId))
            {
                CompletePhoneCallAction(action.RequestExitAfterDialogue);
                return;
            }

            PlayDialogueThenComplete(
                action.DialogueGroupId,
                action.RequestExitAfterDialogue,
                action.MarkBeforeResolveCall);
        }

        private void HandleScreenOpened()
        {
            BindSituation(_situationSceneLoader?.CurrentController);
            RefreshScreenDisplay();
        }

        private void HandleScreenClosed()
        {
            RefreshScreenDisplay();
        }

        private void HandleSituationLoaded(
            SituationController controller,
            SituationDefinition _)
        {
            BindSituation(controller);
            RefreshScreenDisplay();
        }

        private void HandleSituationUnloaded()
        {
            UnbindSituation();
            ResetCallState();
            RefreshScreenDisplay();
        }

        private void HandleSituationResolved()
        {
            if (ShouldAutoExitAfterResolvedLevel1Call())
            {
                if (_isCallInProgress)
                {
                    _shouldRequestExitAfterDialogue = true;
                    return;
                }

                RequestExit();
                return;
            }

            RefreshScreenDisplay();
        }

        private void BindSituation(SituationController controller)
        {
            if (_boundSituationController == controller)
            {
                return;
            }

            UnbindSituation();
            _boundSituationController = controller;

            if (_boundSituationController != null)
            {
                _boundSituationController.Resolved += HandleSituationResolved;
            }
        }

        private void UnbindSituation()
        {
            if (_boundSituationController == null)
            {
                return;
            }

            _boundSituationController.Resolved -= HandleSituationResolved;
            _boundSituationController = null;
        }

        private void RefreshScreenDisplay()
        {
            CellPhoneScreen screen = _endpoint?.Screen;
            if (screen == null)
            {
                return;
            }

            if (!screen.isActiveAndEnabled)
            {
                screen.SetDisplay(CellPhoneDisplayState.Hidden);
                return;
            }

            if (_isCallDisplayLocked)
            {
                screen.SetDisplay(GetDisplayState(_callingContact, true));
                return;
            }

            if (!screen.IsHeld)
            {
                screen.SetDisplay(CellPhoneDisplayState.Hidden);
                return;
            }

            screen.SetDisplay(GetDisplayState(EvaluateScreenContact(), false));
        }

        private static CellPhoneDisplayState GetDisplayState(
            CellPhoneContact contact,
            bool isCalling)
        {
            if (contact == CellPhoneContact.Management)
            {
                return isCalling
                    ? CellPhoneDisplayState.ManagementCalling
                    : CellPhoneDisplayState.Management;
            }

            return isCalling
                ? CellPhoneDisplayState.Emergency119Calling
                : CellPhoneDisplayState.Emergency119;
        }

        private bool ShouldAutoExitAfterResolvedLevel1Call()
        {
            SituationDefinition definition = _situationSceneLoader?.CurrentDefinition;
            SituationController controller = _situationSceneLoader?.CurrentController;

            return definition != null &&
                   controller != null &&
                   definition.Level == SituationLevel.Level1 &&
                   controller.IsResolved &&
                   _hasCalledBeforeResolve;
        }

        private CellPhoneContact EvaluateScreenContact()
        {
            SituationDefinition definition =
                _situationSceneLoader != null
                    ? _situationSceneLoader.CurrentDefinition
                    : null;
            SituationController controller =
                _situationSceneLoader != null
                    ? _situationSceneLoader.CurrentController
                    : null;

            bool shouldCallManagement =
                definition != null &&
                controller != null &&
                definition.Level == SituationLevel.Level0 &&
                controller.IsResolved &&
                definition.IsExitAllowed(ExitType.CellPhone);

            return shouldCallManagement
                ? CellPhoneContact.Management
                : CellPhoneContact.Emergency119;
        }

        private bool CanHandlePhoneCall()
        {
            if (_endpoint == null)
            {
                Debug.LogWarning($"{name}: CellPhoneEndpoint is not registered.", this);
                return false;
            }

            if (_situationSceneLoader == null)
            {
                Debug.LogWarning($"{name}: SituationSceneLoader is not assigned.", this);
                return false;
            }

            return true;
        }

        private PhoneCallAction EvaluatePhoneCallAction()
        {
            SituationDefinition definition = _situationSceneLoader.CurrentDefinition;
            SituationController controller = _situationSceneLoader.CurrentController;

            if (definition == null && controller == null)
            {
                return PlayThenExit(WrongCallGroupId);
            }

            if (definition == null || controller == null)
            {
                return PlayThenExit(WrongCallGroupId);
            }

            return definition.Level switch
            {
                SituationLevel.Level0 => EvaluateLevel0(definition, controller),
                SituationLevel.Level1 => EvaluateLevel1(definition, controller),
                SituationLevel.Level2 => EvaluateLevel2(definition),
                _ => PlayThenExit(WrongCallGroupId)
            };
        }

        private PhoneCallAction EvaluateLevel0(
            SituationDefinition definition,
            SituationController controller)
        {
            if (!definition.IsExitAllowed(ExitType.CellPhone) ||
                !controller.IsResolved)
            {
                return PlayThenExit(WrongCallGroupId);
            }

            return PlayThenExit(definition.AfterResolveCallingDialogueGroupId);
        }

        private PhoneCallAction EvaluateLevel1(
            SituationDefinition definition,
            SituationController controller)
        {
            if (controller.IsResolved && _hasCalledBeforeResolve)
            {
                return ExitImmediately();
            }

            if (!HasDiscoveredCurrentSituation())
            {
                return PlayThenExit(WrongCallGroupId);
            }

            if (controller.IsResolved)
            {
                return PlayThenExit(definition.AfterResolveCallingDialogueGroupId);
            }

            return PlayOnly(definition.BeforeResolveCallingDialogueGroupId, true);
        }

        private PhoneCallAction EvaluateLevel2(SituationDefinition definition)
        {
            if (!HasDiscoveredCurrentSituation())
            {
                return PlayThenExit(WrongCallGroupId);
            }

            return PlayOnly(definition.Level2CallingDialogueGroupId, false);
        }

        private bool HasDiscoveredCurrentSituation()
        {
            return _discoveryTracker != null &&
                   _discoveryTracker.HasDiscoveredCurrentSituation;
        }

        private void PlayDialogueThenComplete(
            string dialogueGroupId,
            bool requestExitAfterDialogue,
            bool markBeforeResolveCall)
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning($"{name}: DialogueManager is not assigned.", this);
                CompletePhoneCallAction(requestExitAfterDialogue);
                return;
            }

            if (!_dialogueManager.TryPlayGroup(dialogueGroupId))
            {
                CompletePhoneCallAction(requestExitAfterDialogue);
                return;
            }

            _isCallInProgress = true;
            _pendingGroupId = dialogueGroupId;
            _shouldRequestExitAfterDialogue = requestExitAfterDialogue;

            if (markBeforeResolveCall)
            {
                _hasCalledBeforeResolve = true;
            }

            LockCallDisplay();
        }

        private void HandleDialogueGroupCompleted(string groupId)
        {
            if (!_isCallInProgress ||
                !string.Equals(groupId, _pendingGroupId, System.StringComparison.Ordinal))
            {
                return;
            }

            bool shouldRequestExit = _shouldRequestExitAfterDialogue;
            ResetDialogueState();
            CompletePhoneCallAction(shouldRequestExit);
        }

        private void CompletePhoneCallAction(bool requestExit)
        {
            ResetDialogueState();

            if (requestExit)
            {
                RequestExit();
                return;
            }

            RefreshScreenDisplay();
        }

        private void LockCallDisplay()
        {
            _callingContact = EvaluateScreenContact();
            _isCallDisplayLocked = true;
            RefreshScreenDisplay();
        }

        private void RequestExit()
        {
            if (_endpoint == null)
            {
                Debug.LogWarning($"{name}: CellPhoneEndpoint is not registered.", this);
                return;
            }

            _endpoint.RequestExit();
        }

        private void ResetCallState()
        {
            ResetDialogueState();
            _hasCalledBeforeResolve = false;
            _isCallDisplayLocked = false;
            _callingContact = CellPhoneContact.Emergency119;
        }

        private void ResetDialogueState()
        {
            _isCallInProgress = false;
            _shouldRequestExitAfterDialogue = false;
            _pendingGroupId = string.Empty;
        }

        private static PhoneCallAction PlayThenExit(string dialogueGroupId) =>
            new(dialogueGroupId, false, true, false);

        private static PhoneCallAction PlayOnly(
            string dialogueGroupId,
            bool markBeforeResolveCall) =>
            new(dialogueGroupId, false, false, markBeforeResolveCall);

        private static PhoneCallAction ExitImmediately() =>
            new(string.Empty, true, false, false);

        private readonly struct PhoneCallAction
        {
            public PhoneCallAction(
                string dialogueGroupId,
                bool requestExitImmediately,
                bool requestExitAfterDialogue,
                bool markBeforeResolveCall)
            {
                DialogueGroupId = dialogueGroupId;
                RequestExitImmediately = requestExitImmediately;
                RequestExitAfterDialogue = requestExitAfterDialogue;
                MarkBeforeResolveCall = markBeforeResolveCall;
            }

            public string DialogueGroupId { get; }
            public bool RequestExitImmediately { get; }
            public bool RequestExitAfterDialogue { get; }
            public bool MarkBeforeResolveCall { get; }
        }
    }
}
