using System;
using UnityEngine;
using VirtualRescue.GameFlow;

public class EntireHouseAlarmSituationController : SituationController
{
    [SerializeField] private AudioSource _alarmAudioSource;
    [SerializeField] private AudioSource _broadCastAudioSource;
    [SerializeField] private bool _isAlarmStarted = false;
    
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
}
