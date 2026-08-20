using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class PhoneCallService : MonoBehaviour
    {
        private const string WrongCallGroupId = "Wrong_Call";

        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private SituationSceneLoader _situationSceneLoader;
        [SerializeField] private SituationDiscoveryTracker _discoveryTracker;

        private CellPhoneEndpoint _endpoint;
        private bool _isCallInProgress;
        private bool _shouldRequestExitAfterDialogue;
        private string _pendingGroupId = string.Empty;
        private string _beforeResolveCalledSituationId = string.Empty;
        private SituationController _heldSituationController;

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

            UnbindHeldSituation();
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
                UnbindHeldSituation();
                UnbindEndpoint(_endpoint);
            }

            _endpoint = endpoint;
            if (_endpoint.Screen != null)
            {
                _endpoint.Screen.ScreenOpened += HandleScreenOpened;
                _endpoint.Screen.ScreenClosed += HandleScreenClosed;
                _endpoint.Screen.CallRequested += HandleCallRequested;

                if (_endpoint.Screen.IsHeld)
                {
                    HandleScreenOpened();
                }

                return;
            }

            if (_endpoint.NumPad == null)
            {
                Debug.LogWarning(
                    $"{endpoint.name}: CellPhoneScreen and NumPad are not assigned.",
                    endpoint);
                return;
            }

            _endpoint.NumPad.OnCorrectNumber += HandleCallRequested;
        }

        private void HandleEndpointUnregistered(CellPhoneEndpoint endpoint)
        {
            if (endpoint == null || endpoint != _endpoint)
            {
                return;
            }

            UnbindHeldSituation();
            UnbindEndpoint(endpoint);
            _endpoint = null;
            ResetCallState();
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

            if (endpoint.NumPad != null)
            {
                endpoint.NumPad.OnCorrectNumber -= HandleCallRequested;
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

            PhoneCallAction action = EvaluatePhoneCallAction();
            if (action.RequestExitImmediately)
            {
                RequestExit();
                return;
            }

            if (string.IsNullOrWhiteSpace(action.DialogueGroupId))
            {
                CompletePhoneCallAction(action.RequestExitAfterDialogue);
                return;
            }

            PlayDialogueThenComplete(action.DialogueGroupId, action.RequestExitAfterDialogue);
        }

        private void HandleScreenOpened()
        {
            BindHeldSituation(
                _situationSceneLoader != null
                    ? _situationSceneLoader.CurrentController
                    : null);
            RefreshScreenContact();
        }

        private void HandleScreenClosed()
        {
            UnbindHeldSituation();
        }

        private void HandleSituationLoaded(
            SituationController controller,
            SituationDefinition _)
        {
            if (_endpoint?.Screen == null || !_endpoint.Screen.IsHeld)
            {
                return;
            }

            BindHeldSituation(controller);
            RefreshScreenContact();
        }

        private void HandleSituationUnloaded()
        {
            UnbindHeldSituation();
            ResetCallState();
            RefreshScreenContact();
        }

        private void HandleHeldSituationResolved()
        {
            RefreshScreenContact();
        }

        private void BindHeldSituation(SituationController controller)
        {
            if (_heldSituationController == controller)
            {
                return;
            }

            UnbindHeldSituation();
            _heldSituationController = controller;

            if (_heldSituationController != null)
            {
                _heldSituationController.Resolved += HandleHeldSituationResolved;
            }
        }

        private void UnbindHeldSituation()
        {
            if (_heldSituationController == null)
            {
                return;
            }

            _heldSituationController.Resolved -= HandleHeldSituationResolved;
            _heldSituationController = null;
        }

        private void RefreshScreenContact()
        {
            CellPhoneScreen screen = _endpoint?.Screen;
            if (screen == null || !screen.IsHeld)
            {
                return;
            }

            screen.ShowContact(EvaluateScreenContact());
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
            if (!HasDiscoveredCurrentSituation())
            {
                return PlayThenExit(WrongCallGroupId);
            }

            if (controller.IsResolved)
            {
                if (WasBeforeResolveCallPlayed(definition))
                {
                    return ExitImmediately();
                }

                return PlayThenExit(definition.AfterResolveCallingDialogueGroupId);
            }

            _beforeResolveCalledSituationId = definition.Id;
            return PlayOnly(definition.BeforeResolveCallingDialogueGroupId);
        }

        private PhoneCallAction EvaluateLevel2(SituationDefinition definition)
        {
            if (!HasDiscoveredCurrentSituation())
            {
                return PlayThenExit(WrongCallGroupId);
            }

            return PlayThenExit(definition.Level2CallingDialogueGroupId);
        }

        private bool HasDiscoveredCurrentSituation()
        {
            return _discoveryTracker != null &&
                   _discoveryTracker.HasDiscoveredCurrentSituation;
        }

        private bool WasBeforeResolveCallPlayed(SituationDefinition definition)
        {
            return definition != null &&
                   !string.IsNullOrWhiteSpace(definition.Id) &&
                   string.Equals(
                       _beforeResolveCalledSituationId,
                       definition.Id,
                       System.StringComparison.Ordinal);
        }

        private void PlayDialogueThenComplete(
            string dialogueGroupId,
            bool requestExitAfterDialogue)
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
            }
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
            _beforeResolveCalledSituationId = string.Empty;
        }

        private void ResetDialogueState()
        {
            _isCallInProgress = false;
            _shouldRequestExitAfterDialogue = false;
            _pendingGroupId = string.Empty;
        }

        private static PhoneCallAction PlayThenExit(string dialogueGroupId) =>
            new(dialogueGroupId, false, true);

        private static PhoneCallAction PlayOnly(string dialogueGroupId) =>
            new(dialogueGroupId, false, false);

        private static PhoneCallAction ExitImmediately() =>
            new(string.Empty, true, false);

        private readonly struct PhoneCallAction
        {
            public PhoneCallAction(
                string dialogueGroupId,
                bool requestExitImmediately,
                bool requestExitAfterDialogue)
            {
                DialogueGroupId = dialogueGroupId;
                RequestExitImmediately = requestExitImmediately;
                RequestExitAfterDialogue = requestExitAfterDialogue;
            }

            public string DialogueGroupId { get; }
            public bool RequestExitImmediately { get; }
            public bool RequestExitAfterDialogue { get; }
        }
    }
}
