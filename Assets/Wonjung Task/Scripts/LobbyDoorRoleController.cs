using System.Collections;
using UnityEngine;
using VirtualRescue.Missions09;

namespace VirtualRescue.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyDoorRoleController : MonoBehaviour
    {
        [Header("Lobby Door Roles")]
        [SerializeField]
        private FireExitDoorController _blockedDoor;

        [SerializeField]
        private FireExitDoorController _exitDoor;

        [Header("Quit")]
        [SerializeField, Min(0f)]
        private float _quitDelay = 0.75f;

        private bool _isQuitting;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            // 로비에서만 각 문 인스턴스의 역할을 지정한다.
            _blockedDoor.SetLocked(true);
            _exitDoor.SetLocked(false);
        }

        private void OnEnable()
        {
            if (_exitDoor != null)
            {
                _exitDoor.Opened += HandleExitDoorOpened;
            }
        }

        private void OnDisable()
        {
            if (_exitDoor != null)
            {
                _exitDoor.Opened -= HandleExitDoorOpened;
            }
        }

        private void HandleExitDoorOpened()
        {
            if (_isQuitting)
            {
                return;
            }

            _isQuitting = true;
            StartCoroutine(QuitRoutine());
        }

        private IEnumerator QuitRoutine()
        {
            if (_quitDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_quitDelay);
            }

#if UNITY_EDITOR
            // 에디터 테스트에서는 Play Mode를 종료한다.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 실제 빌드에서는 애플리케이션을 종료한다.
            Application.Quit();
#endif
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            if (_blockedDoor == null)
            {
                Debug.LogError(
                    $"{nameof(LobbyDoorRoleController)}: " +
                    "Blocked Door가 연결되지 않았습니다.",
                    this);

                isValid = false;
            }

            if (_exitDoor == null)
            {
                Debug.LogError(
                    $"{nameof(LobbyDoorRoleController)}: " +
                    "Exit Door가 연결되지 않았습니다.",
                    this);

                isValid = false;
            }

            if (_blockedDoor != null &&
                _blockedDoor == _exitDoor)
            {
                Debug.LogError(
                    $"{nameof(LobbyDoorRoleController)}: " +
                    "서로 다른 문을 연결해야 합니다.",
                    this);

                isValid = false;
            }

            return isValid;
        }

        private void OnValidate()
        {
            _quitDelay = Mathf.Max(0f, _quitDelay);
        }
    }
}
