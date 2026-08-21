using System;
using System.Collections;
using UnityEngine;

namespace VirtualRescue.Situations.ExitFailure
{
    [DisallowMultipleComponent]
    public sealed class ExitFailureSequencePlayer : MonoBehaviour
    {
        [Header("Effect")]
        [SerializeField] private GameObject _effectPrefab;
        [SerializeField] private Vector3 _effectOffset = new(0f, 0f, 0.6f);
        [SerializeField] private Vector3 _effectEulerAngles;
        [SerializeField, Min(0.01f)] private float _effectScale = 0.8f;

        [Header("Audio")]
        [SerializeField] private AudioClip _audioClip;
        [SerializeField, Range(0f, 3f)] private float _audioVolumeScale = 1f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _failureDelay = 1f;

        private Coroutine _routine;
        private GameObject _effectInstance;

        public event Action Completed;

        public bool IsPlaying => _routine != null;

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

            DestroyEffect();
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
            if (_effectPrefab == null)
            {
                Debug.LogWarning("Exit failure effect prefab is not assigned.", this);
                return;
            }

            Camera hmdCamera = Camera.main;
            if (hmdCamera == null)
            {
                Debug.LogWarning(
                    "The HMD camera was not found for the exit failure effect.",
                    this);
                return;
            }

            DestroyEffect();
            _effectInstance = Instantiate(_effectPrefab, hmdCamera.transform);
            _effectInstance.transform.localPosition = _effectOffset;
            _effectInstance.transform.localRotation =
                Quaternion.Euler(_effectEulerAngles);
            _effectInstance.transform.localScale = Vector3.one * _effectScale;
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

        private void DestroyEffect()
        {
            if (_effectInstance == null)
            {
                return;
            }

            Destroy(_effectInstance);
            _effectInstance = null;
        }

        private void OnDestroy()
        {
            DestroyEffect();
        }
    }
}
