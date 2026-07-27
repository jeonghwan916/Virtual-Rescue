using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;

namespace VirtualRescue.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class ProximityFireAudioZone : MonoBehaviour
    {
        [SerializeField] private AudioClip _fireClip;
        [SerializeField, Min(0.01f)] private float _minimumDistance = 0.5f;
        [SerializeField, Min(0.01f)] private float _maximumDistance = 3f;

        private readonly HashSet<Collider> _playerColliders = new();
        private AudioSource _audioSource;

        private void Awake()
        {
            Collider zoneCollider = GetComponent<Collider>();
            if (!zoneCollider.isTrigger)
            {
                Debug.LogWarning(
                    "화재 근접 오디오 영역의 Collider에서 Is Trigger를 활성화하세요.",
                    this);
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.clip = _fireClip;
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 1f;
            _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _audioSource.minDistance = _minimumDistance;
            _audioSource.maxDistance = _maximumDistance;
            _audioSource.dopplerLevel = 0f;
            _audioSource.Stop();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<XROrigin>() == null)
            {
                return;
            }

            _playerColliders.Add(other);

            if (_audioSource.clip != null &&
                !_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_playerColliders.Remove(other) ||
                _playerColliders.Count > 0)
            {
                return;
            }

            _audioSource.Stop();
        }

        private void OnDisable()
        {
            _playerColliders.Clear();

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        private void OnValidate()
        {
            _minimumDistance = Mathf.Max(0.01f, _minimumDistance);
            _maximumDistance = Mathf.Max(
                _minimumDistance,
                _maximumDistance);
        }
    }
}
