using System;
using UnityEngine;
using VirtualRescue.GameFlow;

public class EntireHouseAlarmSituationController : SituationController
{
    [SerializeField] private AudioSource _alarmAudioSource;
    [SerializeField] private AudioSource _broadCastAudioSource;
    [SerializeField] private bool _isAlarmStarted = false;
    [SerializeField] private SituationTrapDoorTrigger _trapDoorTrigger;
    [SerializeField] private AudioClip _deathAudioClip;

    private bool _hasPlayedDeathAudio;

    private void OnEnable()
    {
        if (_trapDoorTrigger == null)
        {
            Debug.LogError("SituationTrapDoorTrigger is not assigned.", this);
            return;
        }

        _trapDoorTrigger.Triggered += GameOver;
    }

    private void OnDisable()
    {
        if (_trapDoorTrigger != null)
        {
            _trapDoorTrigger.Triggered -= GameOver;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_isAlarmStarted == false)
            {
                _isAlarmStarted = true;
                PlayAlarm();
            }
        }
    }

    public void PlayAlarm()
    {
        _alarmAudioSource.loop = true;
        _alarmAudioSource.Play();
        _broadCastAudioSource.Play();
    }

    public void GameOver()
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

    protected override void OnActivated()
    {
        _hasPlayedDeathAudio = false;
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
