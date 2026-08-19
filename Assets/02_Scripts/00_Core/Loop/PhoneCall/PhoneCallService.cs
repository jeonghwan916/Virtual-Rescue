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
                _situationSceneLoader.SituationUnloaded += ResetCallState;
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
                _situationSceneLoader.SituationUnloaded -= ResetCallState;
            }

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
            if (_endpoint.NumPad == null)
            {
                Debug.LogWarning(
                    $"{endpoint.name}: NumPad is not assigned.",
                    endpoint);
                return;
            }

            _endpoint.NumPad.OnCorrectNumber += HandleCorrectNumber;
        }

        private void HandleEndpointUnregistered(CellPhoneEndpoint endpoint)
        {
            if (endpoint == null || endpoint != _endpoint)
            {
                return;
            }

            UnbindEndpoint(endpoint);
            _endpoint = null;
            ResetCallState();
        }

        private void UnbindEndpoint(CellPhoneEndpoint endpoint)
        {
            if (endpoint == null || endpoint.NumPad == null)
            {
                return;
            }

            endpoint.NumPad.OnCorrectNumber -= HandleCorrectNumber;
        }

        private void HandleCorrectNumber()
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

            return ExitImmediately();
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
