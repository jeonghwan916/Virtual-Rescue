using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations.DryerTowelHazard
{
    [DisallowMultipleComponent]
    public sealed class DryerTowelHazardSituationController : SituationController
    {
        [Header("References")]
        [SerializeField] private XRSocketInteractor _powerSocket;
        [SerializeField] private AudioSource _operatingAudioSource;

        private Coroutine _evaluationRoutine;
        private bool _hasObservedConnection;

        protected override void OnActivated()
        {
            _hasObservedConnection = false;

            if (_powerSocket == null)
            {
                Debug.LogError("A dryer power socket must be assigned.", this);
                return;
            }

            ConfigureOperatingAudio();
            SubscribeSocketEvents();
            ScheduleEvaluation();
        }

        protected override void OnResolved()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
            StopOperatingAudio();
        }

        protected override void OnFailed()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
            StopOperatingAudio();
        }

        protected override void OnReset()
        {
            StopEvaluation();
            UnsubscribeSocketEvents();
            StopOperatingAudio();
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
            EvaluatePowerState();
        }

        private void EvaluatePowerState()
        {
            if (!IsActive || _powerSocket == null)
            {
                return;
            }

            if (_powerSocket.hasSelection)
            {
                _hasObservedConnection = true;
                PlayOperatingAudio();
                return;
            }

            StopOperatingAudio();

            // 시작 플러그 연결이 누락된 설정 오류를 성공으로 처리하지 않는다.
            if (!_hasObservedConnection)
            {
                return;
            }

            if (!ResolveSituation())
            {
                Debug.LogError(
                    "The dryer towel hazard situation could not be resolved.",
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

        private void SubscribeSocketEvents()
        {
            _powerSocket.selectEntered.RemoveListener(HandleSocketSelectionChanged);
            _powerSocket.selectExited.RemoveListener(HandleSocketSelectionChanged);
            _powerSocket.selectEntered.AddListener(HandleSocketSelectionChanged);
            _powerSocket.selectExited.AddListener(HandleSocketSelectionChanged);
        }

        private void UnsubscribeSocketEvents()
        {
            if (_powerSocket == null)
            {
                return;
            }

            _powerSocket.selectEntered.RemoveListener(HandleSocketSelectionChanged);
            _powerSocket.selectExited.RemoveListener(HandleSocketSelectionChanged);
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
