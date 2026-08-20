using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations.PartitionEscape
{
    [DisallowMultipleComponent]
    public sealed class LightweightPartitionEscapeSituationController :
        SituationController
    {
        [SerializeField] private SituationTrapDoorTrigger _trapDoorTrigger;
        [SerializeField] private AudioClip _deathAudioClip;

        private bool _hasPlayedDeathAudio;

        private void OnEnable()
        {
            if (_trapDoorTrigger == null)
            {
                Debug.LogError(
                    "SituationTrapDoorTrigger is not assigned.",
                    this);
                return;
            }

            _trapDoorTrigger.Triggered += HandleTrapDoorTriggered;
        }

        private void OnDisable()
        {
            if (_trapDoorTrigger != null)
            {
                _trapDoorTrigger.Triggered -= HandleTrapDoorTriggered;
            }
        }

        protected override void OnActivated()
        {
            _hasPlayedDeathAudio = false;
        }

        private void HandleTrapDoorTriggered()
        {
            if (!IsActive)
            {
                return;
            }

            if (FailSituation())
            {
                PlayDeathAudio();
            }
        }

        private void PlayDeathAudio()
        {
            if (_hasPlayedDeathAudio)
            {
                return;
            }

            _hasPlayedDeathAudio = true;

            if (_deathAudioClip == null)
            {
                Debug.LogWarning("Death audio clip is not assigned.", this);
                return;
            }

            PlayerReferenceHub playerReferenceHub = PlayerReferenceHub.Instance;
            AudioSource xrAudioSource = playerReferenceHub?.XrAudioSource;

            if (xrAudioSource == null)
            {
                Debug.LogWarning(
                    "HMD AudioSource was not found on PlayerReferenceHub.",
                    this);
                return;
            }

            xrAudioSource.PlayOneShot(_deathAudioClip);
        }
    }
}
