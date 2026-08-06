using System;
using System.Threading.Tasks;
using UnityEngine;
using VirtualRescue.GameFlow;

public class DaySceneCoordinator : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private DayFlowController _dayFlowController;

    [Header("Home")]
    [SerializeField] private HomeModuleLoader _homeModuleLoader;
    [SerializeField] private HomeLayoutDefinition _homeLayout;

    [Header("Situation")]
    [SerializeField] private SituationSelector _situationSelector;
    [SerializeField] private SituationSceneLoader _situationSceneLoader;

    private bool _isProcessing;

    private void OnEnable()
    {
        if (_dayFlowController == null)
        {
            return;
        }

        _dayFlowController.DayStarted += HandleDayStarted;
        _dayFlowController.TransitionRequested += HandleTransitionRequested;
        _dayFlowController.GameCleared += HandleGameCleared;
    }

    private void OnDisable()
    {
        if (_dayFlowController == null)
        {
            return;
        }

        _dayFlowController.DayStarted -= HandleDayStarted;
        _dayFlowController.TransitionRequested -= HandleTransitionRequested;
        _dayFlowController.GameCleared -= HandleGameCleared;
    }

    private async void HandleDayStarted(int currentDay)
    {
        if (_isProcessing)
        {
            return;
        }

        _isProcessing = true;

        try
        {
            bool homeLoaded = await _homeModuleLoader.LoadAsync(_homeLayout);
            if (!homeLoaded)
            {
                ReportError(_homeModuleLoader.LastError);
                return;
            }

            bool selectionSucceeded = _situationSelector.TrySelect(
                currentDay,
                _dayFlowController.SeenSituationIds,
                out SituationDefinition selectedSituation);

            if (!selectionSucceeded)
            {
                ReportError(_situationSelector.LastError);
                return;
            }

            if (selectedSituation != null)
            {
                bool situationLoaded =
                    await _situationSceneLoader.LoadAsync(selectedSituation);

                if (!situationLoaded)
                {
                    ReportError(_situationSceneLoader.LastError);
                    return;
                }

                bool registered =
                    _dayFlowController.TryRegisterSituation(selectedSituation);

                if (!registered)
                {
                    ReportError(
                        $"상황 ID를 등록하지 못했습니다: {selectedSituation.Id}");
                    return;
                }
            }
            else
            {
                Debug.Log($"{currentDay}일차는 무상황입니다.", this);
            }

            if (!_dayFlowController.NotifyHomeLoaded())
            {
                ReportError("하루를 Playing 상태로 전환하지 못했습니다.");
            }
        }
        catch (Exception exception)
        {
            ReportError(exception.Message);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async void HandleTransitionRequested(
        DayTransitionReason reason,
        int targetDay)
    {
        if (_isProcessing)
        {
            return;
        }

        _isProcessing = true;
        bool unloaded = false;

        try
        {
            unloaded = await UnloadCurrentDayAsync();

            if (unloaded)
            {
                Debug.Log(
                    $"{reason}: {targetDay}일차로 전환합니다.",
                    this);
            }
        }
        catch (Exception exception)
        {
            ReportError(exception.Message);
        }

        // NotifyTransitionCompleted()가 즉시 다음 DayStarted를 발생시키므로
        // 먼저 처리 잠금을 해제한다.
        _isProcessing = false;

        if (unloaded &&
            !_dayFlowController.NotifyTransitionCompleted())
        {
            ReportError("다음 날을 시작하지 못했습니다.");
        }
    }

    private async void HandleGameCleared()
    {
        if (_isProcessing)
        {
            return;
        }

        _isProcessing = true;

        try
        {
            await UnloadCurrentDayAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception.Message);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task<bool> UnloadCurrentDayAsync()
    {
        bool situationUnloaded =
            await _situationSceneLoader.UnloadAsync();

        if (!situationUnloaded)
        {
            ReportError(_situationSceneLoader.LastError);
            return false;
        }

        await _homeModuleLoader.UnloadAllAsync();

        if (_homeModuleLoader.LoadedModuleSceneNames.Count > 0)
        {
            ReportError(_homeModuleLoader.LastError);
            return false;
        }

        return true;
    }

    private void ReportError(string message)
    {
        Debug.LogError($"DaySceneCoordinator: {message}", this);
    }

}
