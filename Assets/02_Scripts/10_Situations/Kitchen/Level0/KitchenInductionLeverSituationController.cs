using UnityEngine;
using VirtualRescue.GameFlow;
using VirtualRescue.Interaction;

namespace VirtualRescue.Situations
{
    public sealed class KitchenInductionLeverSituationController
        : SituationController
    {
        [Header("References")]
        [SerializeField]
        private InductionLeverHeat _leverHeat;

        [SerializeField]
        private AudioSource _operatingAudioSource;

        private void Awake()
        {
            if (_leverHeat == null)
            {
                Debug.LogError(
                    $"[{name}] InductionLeverHeat is not assigned.",
                    this);
                return;
            }

            if (_operatingAudioSource == null)
            {
                Debug.LogWarning(
                    $"[{name}] Induction AudioSource is not assigned.",
                    this);
            }
        }

        private void Update()
        {
            if (_leverHeat == null)
            {
                return;
            }

            UpdateOperatingSound();

            if (IsActive && !_leverHeat.IsHeatOn)
            {
                StageClear();
                return;
            }

            if (IsResolved && _leverHeat.IsHeatOn)
            {
                ReturnToActive();
            }
        }

        private void OnDisable()
        {
            if (_operatingAudioSource != null)
            {
                _operatingAudioSource.Stop();
            }
        }

        private void UpdateOperatingSound()
        {
            if (_operatingAudioSource == null)
            {
                return;
            }

            if (_leverHeat.IsHeatOn)
            {
                if (!_operatingAudioSource.isPlaying)
                {
                    _operatingAudioSource.Play();
                }

                return;
            }

            if (_operatingAudioSource.isPlaying)
            {
                _operatingAudioSource.Stop();
            }
        }

        private void StageClear()
        {
            ResolveSituation();
        }

        private void ReturnToActive()
        {
            SituationDefinition definition = Definition;

            if (definition == null)
            {
                return;
            }

            ResetSituation();
            Activate(definition);
        }
    }
}
