using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class SituationDialogueDirector : MonoBehaviour
    {
        [SerializeField] private SituationSceneLoader _situationSceneLoader;
        [SerializeField] private DialogueManager _dialogueManager;

        private SituationController _boundController;

        private void OnEnable()
        {
            if (_situationSceneLoader != null)
            {
                _situationSceneLoader.SituationLoaded += HandleSituationLoaded;
                _situationSceneLoader.SituationUnloaded += HandleSituationUnloaded;
            }

            BindCurrentSituation();
        }

        private void OnDisable()
        {
            if (_situationSceneLoader != null)
            {
                _situationSceneLoader.SituationLoaded -= HandleSituationLoaded;
                _situationSceneLoader.SituationUnloaded -= HandleSituationUnloaded;
            }

            UnbindCurrentSituation();
        }

        private void HandleSituationLoaded(
            SituationController controller,
            SituationDefinition definition)
        {
            BindSituation(controller);
        }

        private void HandleSituationUnloaded()
        {
            UnbindCurrentSituation();
        }

        private void BindCurrentSituation()
        {
            if (_situationSceneLoader == null)
            {
                Debug.LogWarning(
                    $"{name}: SituationSceneLoader is not assigned.",
                    this);
                return;
            }

            BindSituation(_situationSceneLoader.CurrentController);
        }

        private void BindSituation(SituationController controller)
        {
            UnbindCurrentSituation();

            _boundController = controller;
            if (_boundController == null)
            {
                return;
            }

            _boundController.Resolved += HandleSituationResolved;
            _boundController.WarningRaised += HandleSituationWarningRaised;
            _boundController.Failed += HandleSituationFailed;
        }

        private void UnbindCurrentSituation()
        {
            if (_boundController == null)
            {
                return;
            }

            _boundController.Resolved -= HandleSituationResolved;
            _boundController.WarningRaised -= HandleSituationWarningRaised;
            _boundController.Failed -= HandleSituationFailed;
            _boundController = null;
        }

        private void HandleSituationResolved()
        {
            PlayDialogue(_situationSceneLoader.CurrentDefinition?.ResolvedDialogueId);
        }

        private void HandleSituationWarningRaised()
        {
            PlayDialogue(_situationSceneLoader.CurrentDefinition?.WarningDialogueId);
        }

        private void HandleSituationFailed()
        {
            PlayDialogue(_situationSceneLoader.CurrentDefinition?.FailedDialogueId);
        }

        private void PlayDialogue(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                return;
            }

            if (_dialogueManager == null)
            {
                Debug.LogWarning(
                    $"{name}: DialogueManager is not assigned.",
                    this);
                return;
            }

            _dialogueManager.Play(dialogueId);
        }
    }
}
