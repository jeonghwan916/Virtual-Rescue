using System;
using System.Collections;
using UnityEngine;

namespace VirtualRescue.Situations.ExitFailure
{
    [DisallowMultipleComponent]
    public sealed class ExitFailureSequencePlayer : MonoBehaviour
    {
        [Header("Effect")]
        [SerializeField] private GameObject _effectObject;

        [Header("Audio")]
        [SerializeField] private AudioClip _audioClip;
        [SerializeField, Range(0f, 3f)] private float _audioVolumeScale = 1f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _failureDelay = 1f;

        private Coroutine _routine;
        private ParticleSystem[] _effectParticles = Array.Empty<ParticleSystem>();

        public event Action Completed;

        public bool IsPlaying => _routine != null;

        private void Awake()
        {
            if (_effectObject != null)
            {
                _effectParticles =
                    _effectObject.GetComponentsInChildren<ParticleSystem>(true);
            }

            StopEffect();
        }

        public bool TryPlay()
        {
            if (_routine != null)
            {
                return false;
            }

            _routine = StartCoroutine(PlayRoutine());
            return true;
        }

        public void ResetSequence()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            StopEffect();
        }

        private IEnumerator PlayRoutine()
        {
            PlayEffect();
            PlayAudio();

            yield return new WaitForSecondsRealtime(_failureDelay);

            _routine = null;
            Completed?.Invoke();
        }

        private void PlayEffect()
        {
            if (_effectObject == null)
            {
                Debug.LogWarning("Exit failure effect object is not assigned.", this);
                return;
            }

            _effectObject.SetActive(true);

            foreach (ParticleSystem particle in _effectParticles)
            {
                particle.Play();
            }
        }

        private void PlayAudio()
        {
            if (_audioClip == null)
            {
                return;
            }

            AudioSource xrAudioSource = PlayerReferenceHub.Instance?.XrAudioSource;
            if (xrAudioSource == null)
            {
                Debug.LogWarning(
                    "HMD AudioSource was not found on PlayerReferenceHub.",
                    this);
                return;
            }

            xrAudioSource.PlayOneShot(_audioClip, _audioVolumeScale);
        }

        private void StopEffect()
        {
            if (_effectObject == null)
            {
                return;
            }

            foreach (ParticleSystem particle in _effectParticles)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            _effectObject.SetActive(false);
        }
    }
}
