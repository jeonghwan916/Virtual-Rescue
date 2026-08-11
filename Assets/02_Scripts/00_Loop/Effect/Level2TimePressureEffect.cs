using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Effects
{
    [DisallowMultipleComponent]
    public sealed class Level2TimePressureEffect : MonoBehaviour
    {
        [Header("Situation")]
        [SerializeField] private DayFlowController _dayFlowController;
        [SerializeField] private SituationSceneLoader _situationSceneLoader;

        [Header("Vignette")]
        [SerializeField, Range(0f, 1f)] private float _minimumApertureSize = 0.4f;

        [Header("Cough Audio")]
        [SerializeField] private AudioSource _coughAudioSource;
        [SerializeField] private AudioClip _coughAudioClip;

        private SituationController _boundController;
        private SituationDefinition _boundDefinition;
        private VignetteController _vignetteController;
        private bool _coughStarted;

        private void OnEnable()
        {
            if (_dayFlowController == null)
            {
                return;
            }

            _dayFlowController.StateChanged += HandleDayFlowStateChanged;

            if (_dayFlowController.CurrentState == DayFlowState.Playing)
            {
                BindCurrentSituation();
            }
        }

        private void OnDisable()
        {
            if (_dayFlowController != null)
            {
                _dayFlowController.StateChanged -= HandleDayFlowStateChanged;
            }

            ResetEffect();
        }

        private void Update()
        {
            if (_boundController == null ||
                _boundDefinition == null ||
                !_boundController.IsActive)
            {
                return;
            }

            float progress = 1f - Mathf.Clamp01(
                _boundController.RemainingTime /
                _boundDefinition.TimeLimitSeconds);

            float apertureSize = Mathf.Lerp(
                1f,
                _minimumApertureSize,
                progress);

            _vignetteController?.SetTimePressureApertureSize(apertureSize);

            if (progress >= 0.5f && !_coughStarted)
            {
                StartCoughAudio();
            }
        }

        private void HandleDayFlowStateChanged(DayFlowState state)
        {
            if (state == DayFlowState.Playing)
            {
                BindCurrentSituation();
                return;
            }

            ResetEffect();
        }

        private void BindCurrentSituation()
        {
            ResetEffect();

            if (_situationSceneLoader == null)
            {
                Debug.LogWarning(
                    $"{nameof(Level2TimePressureEffect)}: " +
                    "SituationSceneLoader is not assigned.",
                    this);
                return;
            }

            SituationDefinition definition =
                _situationSceneLoader.CurrentDefinition;
            SituationController controller =
                _situationSceneLoader.CurrentController;

            if (definition == null ||
                controller == null ||
                definition.Level != SituationLevel.Level2 ||
                !definition.UsesTimeLimit)
            {
                return;
            }

            PlayerReferenceHub playerReferenceHub = PlayerReferenceHub.Instance;
            if (playerReferenceHub == null ||
                playerReferenceHub.VignetteController == null)
            {
                Debug.LogWarning(
                    $"{nameof(Level2TimePressureEffect)}: " +
                    "Player vignette is not available.",
                    this);
                return;
            }

            _boundDefinition = definition;
            _boundController = controller;
            _vignetteController = playerReferenceHub.VignetteController;

            _boundController.Resolved += HandleSituationEnded;
            _boundController.Failed += HandleSituationEnded;
            _boundController.ResetPerformed += HandleSituationEnded;
        }

        private void HandleSituationEnded()
        {
            ResetEffect();
        }

        private void StartCoughAudio()
        {
            _coughStarted = true;

            if (_coughAudioSource == null || _coughAudioClip == null)
            {
                return;
            }

            _coughAudioSource.Stop();
            _coughAudioSource.clip = _coughAudioClip;
            _coughAudioSource.loop = true;
            _coughAudioSource.Play();
        }

        private void ResetEffect()
        {
            if (_boundController != null)
            {
                _boundController.Resolved -= HandleSituationEnded;
                _boundController.Failed -= HandleSituationEnded;
                _boundController.ResetPerformed -= HandleSituationEnded;
            }

            _boundController = null;
            _boundDefinition = null;

            if (_vignetteController != null)
            {
                _vignetteController.ResetTimePressureAperture();
                _vignetteController = null;
            }

            if (_coughAudioSource != null)
            {
                _coughAudioSource.Stop();
            }

            _coughStarted = false;
        }

        private void OnValidate()
        {
            _minimumApertureSize = Mathf.Clamp01(_minimumApertureSize);
        }
    }
}
