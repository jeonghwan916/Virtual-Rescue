using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations.HairStraightenerHazard
{
    [DisallowMultipleComponent]
    public sealed class HairStraightenerHazardSituationController : SituationController
    {
        [Header("References")]
        [SerializeField] private List<XRSocketInteractor> _powerSockets = new();
        [SerializeField] private AudioSource _operatingAudioSource;

        [Header("Power Warning Visual")]
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Renderer _topRenderer;
        [SerializeField] private Color _poweredBaseColor =
            new Color(1f, 0.05f, 0.02f, 1f);
        [SerializeField, ColorUsage(true, true)]
        private Color _poweredEmissionColor =
            new Color(4f, 0.1f, 0f, 1f);

        private Coroutine _evaluationRoutine;
        private bool _hasObservedConnection;
        private MaterialPropertyBlock _propertyBlock;
        private Color _normalBodyColor;
        private Color _normalTopColor;

        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _normalBodyColor = GetBaseColor(_bodyRenderer);
            _normalTopColor = GetBaseColor(_topRenderer);
            ApplyPowerWarningVisual(false);
        }

        protected override void OnActivated()
        {
            _hasObservedConnection = false;

            if (!HasAssignedPowerSocket())
            {
                Debug.LogError(
                    "At least one hair straightener power socket must be assigned.",
                    this);
                return;
            }

            ConfigureOperatingAudio();
            SubscribeSocketEvents();
            ScheduleEvaluation();
        }

        protected override void OnResolved()
        {
            StopEvaluation();
            StopOperatingAudio();
            ApplyPowerWarningVisual(false);
        }

        protected override void OnFailed()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
            StopOperatingAudio();
            ApplyPowerWarningVisual(IsPowerConnected());
        }

        protected override void OnReset()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
            StopOperatingAudio();
            ApplyPowerWarningVisual(false);
            _hasObservedConnection = false;
        }

        private void OnDisable()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
            StopOperatingAudio();
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
            EvaluatePowerState();
        }

        private void EvaluatePowerState()
        {
            if ((!IsActive && !IsResolved) || !HasAssignedPowerSocket())
            {
                return;
            }

            if (IsPowerConnected())
            {
                ApplyPowerWarningVisual(true);

                if (IsResolved)
                {
                    if (!ReopenResolvedSituation())
                    {
                        Debug.LogError(
                            "The resolved hair straightener hazard situation could not be reopened.",
                            this);
                    }
                }

                _hasObservedConnection = true;
                PlayOperatingAudio();
                return;
            }

            StopOperatingAudio();
            ApplyPowerWarningVisual(false);

            // 시작 플러그 연결이 누락된 설정 오류를 성공으로 처리하지 않는다.
            if (!_hasObservedConnection)
            {
                return;
            }

            if (!ResolveSituation())
            {
                Debug.LogError(
                    "The hair straightener hazard situation could not be resolved.",
                    this);
            }
        }

        private void ConfigureOperatingAudio()
        {
            if (_operatingAudioSource == null)
            {
                return;
            }

            _operatingAudioSource.playOnAwake = false;
            _operatingAudioSource.loop = true;
            _operatingAudioSource.Stop();
        }

        private void PlayOperatingAudio()
        {
            if (_operatingAudioSource == null ||
                _operatingAudioSource.clip == null ||
                _operatingAudioSource.isPlaying)
            {
                return;
            }

            _operatingAudioSource.Play();
        }

        private void StopOperatingAudio()
        {
            if (_operatingAudioSource == null || !_operatingAudioSource.isPlaying)
            {
                return;
            }

            _operatingAudioSource.Stop();
        }

        private static Color GetBaseColor(Renderer renderer)
        {
            if (renderer == null ||
                renderer.sharedMaterial == null ||
                !renderer.sharedMaterial.HasProperty(BaseColorId))
            {
                return Color.white;
            }

            return renderer.sharedMaterial.GetColor(BaseColorId);
        }

        private void ApplyPowerWarningVisual(bool isPowered)
        {
            ApplyRendererColor(
                _bodyRenderer,
                _normalBodyColor,
                isPowered);
            ApplyRendererColor(
                _topRenderer,
                _normalTopColor,
                isPowered);
        }

        private void ApplyRendererColor(
            Renderer renderer,
            Color normalColor,
            bool isPowered)
        {
            if (renderer == null || _propertyBlock == null)
            {
                return;
            }

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(
                BaseColorId,
                isPowered ? _poweredBaseColor : normalColor);
            _propertyBlock.SetColor(
                EmissionColorId,
                isPowered ? _poweredEmissionColor : Color.black);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private void SubscribeSocketEvents()
        {
            foreach (XRSocketInteractor powerSocket in _powerSockets)
            {
                if (powerSocket == null)
                {
                    continue;
                }

                powerSocket.selectEntered.RemoveListener(HandleSocketSelectionChanged);
                powerSocket.selectExited.RemoveListener(HandleSocketSelectionChanged);
                powerSocket.selectEntered.AddListener(HandleSocketSelectionChanged);
                powerSocket.selectExited.AddListener(HandleSocketSelectionChanged);
            }
        }

        private void UnsubscribeSocketEvents()
        {
            if (_powerSockets == null)
            {
                return;
            }

            foreach (XRSocketInteractor powerSocket in _powerSockets)
            {
                if (powerSocket == null)
                {
                    continue;
                }

                powerSocket.selectEntered.RemoveListener(HandleSocketSelectionChanged);
                powerSocket.selectExited.RemoveListener(HandleSocketSelectionChanged);
            }
        }

        private bool HasAssignedPowerSocket()
        {
            if (_powerSockets == null)
            {
                return false;
            }

            foreach (XRSocketInteractor powerSocket in _powerSockets)
            {
                if (powerSocket != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPowerConnected()
        {
            if (_powerSockets == null)
            {
                return false;
            }

            foreach (XRSocketInteractor powerSocket in _powerSockets)
            {
                if (powerSocket != null && powerSocket.hasSelection)
                {
                    return true;
                }
            }

            return false;
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
