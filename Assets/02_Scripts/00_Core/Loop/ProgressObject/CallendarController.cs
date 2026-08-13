using System;
using UnityEngine;
using VirtualRescue.GameFlow;

public class CallendarController : MonoBehaviour
{
    [Header("Day Flow Controller")]
    [SerializeField] private DayFlowController _dayFlowController;

    [Header("Callendars")]
    [SerializeField] private GameObject[] _callendars;
    
    private void OnEnable()
    {
        _dayFlowController.DayStarted += ChangeCallendar;
        //_dayFlowController.StateChanged += ChangeCallendar;
    }

    private void OnDisable()
    {
        _dayFlowController.DayStarted -= ChangeCallendar;
        //_dayFlowController.StateChanged -= ChangeCallendar;
    }
    
    private void ChangeCallendar(int currentDay)
    {
        // 모든 집 모듈 + 상황 씬 로딩 + 페이드인까지 끝난 뒤 실행
        for (int i = 0; i < _callendars.Length; i++)
        {
            if (_callendars[i].activeSelf) _callendars[i].SetActive(false);
        }
            
        _callendars[currentDay - 1].SetActive(true);
        /*
        if (currentDay == 1) _callendars[0].SetActive(true);
        else if (currentDay == 2) _callendars[1].SetActive(true);
        else if (currentDay == 3) _callendars[2].SetActive(true);
        else if (currentDay == 4) _callendars[3].SetActive(true);
        else if (currentDay == 5) _callendars[4].SetActive(true);
        else if (currentDay == 6) _callendars[5].SetActive(true);
        else if (currentDay == 7) _callendars[6].SetActive(true);
        else if (currentDay == 8) _callendars[7].SetActive(true);
        */
    }
}
