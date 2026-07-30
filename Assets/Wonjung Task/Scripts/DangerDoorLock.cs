using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VirtualRescue.Missions09;

namespace VirtualRescue.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DangerDoorLock : MonoBehaviour
    {
        [SerializeField]
        private DoorHandleTemperature _temperature;

        [SerializeField]
        private FireExitDoorHandle _handleMotion;

        [SerializeField]
        private XRSimpleInteractable _doorInteractable;

        [SerializeField]
        private FireExitDoorController _doorController;

        private FireExitDoorHandle[] _handleMotions;
        private bool _previousDangerState;

        private void Awake()
        {
            _handleMotions =
                GetComponentsInChildren<FireExitDoorHandle>(true);

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            // 첫 실행에서 반드시 상태가 적용되도록 반대 값으로 초기화
            _previousDangerState =
                !_temperature.IsDangerous;

            ApplyLockState();
        }

        private void Update()
        {
            bool isDangerous =
                _temperature.IsDangerous;

            if (_previousDangerState ==
                isDangerous)
            {
                return;
            }

            ApplyLockState();
        }

        private void ApplyLockState()
        {
            bool isDangerous =
                _temperature.IsDangerous;

            bool canOperate =
                !isDangerous;

            // 손잡이 피봇 회전 차단
            foreach (FireExitDoorHandle handleMotion in _handleMotions)
            {
                handleMotion.enabled = canOperate;
            }

            // 문 패널 직접 조작 차단
            _doorInteractable.enabled =
                canOperate;

            // 실제 문 회전 로직 차단
            _doorController.enabled =
                canOperate;

            _previousDangerState =
                isDangerous;

            Debug.Log(
                $"[{name}] 위험 상태: {isDangerous} | " +
                $"손잡이 및 문 동작 가능: {canOperate}",
                this);
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            if (_temperature == null)
            {
                Debug.LogError(
                    $"[{name}] DoorHandleTemperature가 연결되지 않았습니다.",
                    this);

                isValid = false;
            }

            if (_handleMotion == null)
            {
                Debug.LogError(
                    $"[{name}] FireExitDoorHandle이 연결되지 않았습니다.",
                    this);

                isValid = false;
            }

            if (_doorInteractable == null)
            {
                Debug.LogError(
                    $"[{name}] 문의 XRSimpleInteractable이 연결되지 않았습니다.",
                    this);

                isValid = false;
            }

            if (_doorController == null)
            {
                Debug.LogError(
                    $"[{name}] FireExitDoorController가 연결되지 않았습니다.",
                    this);

                isValid = false;
            }

            return isValid;
        }
    }
}
