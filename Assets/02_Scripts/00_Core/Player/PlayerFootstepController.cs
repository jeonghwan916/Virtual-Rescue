using UnityEngine;

namespace VirtualRescue.Player
{
    public sealed class PlayerFootstepController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _movementTarget;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private AudioSource _footstepAudioSource;
        [SerializeField] private AudioClip[] _footstepClips;

        [Header("Step Detection")]
        [SerializeField] private float _minMoveSpeed = 0.05f;
        [SerializeField] private float _walkStepDistance = 0.45f;
        [SerializeField] private float _minStepInterval = 0.22f;
        [SerializeField] private float _teleportResetDistance = 1.2f;
        [SerializeField] private bool _requireGrounded = true;

        [Header("Variation")]
        [SerializeField] private Vector2 _volumeRange = new Vector2(0.85f, 1f);
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.95f, 1.05f);

        private Vector3 _lastPosition;
        private float _distanceSinceLastStep;
        private float _lastStepTime = -999f;
        private int _lastClipIndex = -1;

        private void Awake()
        {
            if (_movementTarget == null)
            {
                _movementTarget = transform;
            }

            if (_footstepAudioSource == null)
            {
                _footstepAudioSource = GetComponent<AudioSource>();
            }

            if (_characterController == null)
            {
                _characterController = _movementTarget.GetComponent<CharacterController>();
            }

            ResetStepTracking();
        }

        private void Update()
        {
            if (_movementTarget == null ||
                _footstepAudioSource == null ||
                _footstepClips == null ||
                _footstepClips.Length == 0)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 currentPosition = _movementTarget.position;
            Vector3 horizontalDelta = currentPosition - _lastPosition;
            horizontalDelta.y = 0f;

            float horizontalDistance = horizontalDelta.magnitude;
            _lastPosition = currentPosition;

            if (horizontalDistance >= _teleportResetDistance)
            {
                _distanceSinceLastStep = 0f;
                return;
            }

            float horizontalSpeed = horizontalDistance / deltaTime;
            if (horizontalSpeed < _minMoveSpeed || !CanPlayWhileGrounded())
            {
                _distanceSinceLastStep = 0f;
                return;
            }

            _distanceSinceLastStep += horizontalDistance;
            if (_distanceSinceLastStep < _walkStepDistance ||
                Time.time - _lastStepTime < _minStepInterval)
            {
                return;
            }

            _distanceSinceLastStep = 0f;
            _lastStepTime = Time.time;
            PlayFootstep();
        }

        private bool CanPlayWhileGrounded()
        {
            return !_requireGrounded ||
                   _characterController == null ||
                   _characterController.isGrounded;
        }

        private void PlayFootstep()
        {
            int clipIndex = GetRandomClipIndex();
            if (clipIndex < 0)
            {
                return;
            }

            _lastClipIndex = clipIndex;
            _footstepAudioSource.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
            float volume = Random.Range(_volumeRange.x, _volumeRange.y);
            _footstepAudioSource.PlayOneShot(_footstepClips[clipIndex], volume);
        }

        private int GetRandomClipIndex()
        {
            if (_footstepClips == null || _footstepClips.Length == 0)
            {
                return -1;
            }

            if (_footstepClips.Length == 1)
            {
                return _footstepClips[0] == null ? -1 : 0;
            }

            int clipIndex;
            do
            {
                clipIndex = Random.Range(0, _footstepClips.Length);
            }
            while ((clipIndex == _lastClipIndex || _footstepClips[clipIndex] == null) &&
                   HasPlayableAlternative());

            return _footstepClips[clipIndex] == null ? -1 : clipIndex;
        }

        private bool HasPlayableAlternative()
        {
            for (int i = 0; i < _footstepClips.Length; i++)
            {
                if (i != _lastClipIndex && _footstepClips[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void ResetStepTracking()
        {
            _lastPosition = _movementTarget != null ? _movementTarget.position : transform.position;
            _distanceSinceLastStep = 0f;
        }

        private void OnValidate()
        {
            _minMoveSpeed = Mathf.Max(0f, _minMoveSpeed);
            _walkStepDistance = Mathf.Max(0.01f, _walkStepDistance);
            _minStepInterval = Mathf.Max(0f, _minStepInterval);
            _teleportResetDistance = Mathf.Max(_walkStepDistance, _teleportResetDistance);

            _volumeRange.x = Mathf.Clamp01(_volumeRange.x);
            _volumeRange.y = Mathf.Clamp01(_volumeRange.y);
            if (_volumeRange.x > _volumeRange.y)
            {
                float volumeMin = _volumeRange.y;
                _volumeRange.y = _volumeRange.x;
                _volumeRange.x = volumeMin;
            }

            _pitchRange.x = Mathf.Max(0.01f, _pitchRange.x);
            _pitchRange.y = Mathf.Max(0.01f, _pitchRange.y);
            if (_pitchRange.x > _pitchRange.y)
            {
                float pitchMin = _pitchRange.y;
                _pitchRange.y = _pitchRange.x;
                _pitchRange.x = pitchMin;
            }
        }
    }
}
