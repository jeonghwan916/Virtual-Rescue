using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.Effects;
using VirtualRescue.Player;

namespace VirtualRescue.Lobby
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BedGameStartTrigger : MonoBehaviour
    {
        private const string LoopBaseSceneName = "LoopBase";

        [SerializeField, Min(0f)]
        private float _fadeOutDuration = 1f;

        private bool _isStarting;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isStarting)
            {
                return;
            }

            PersistentPlayerRoot enteredPlayer =
                other.GetComponentInParent<PersistentPlayerRoot>();

            if (enteredPlayer == null ||
                enteredPlayer != PersistentPlayerRoot.Instance)
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(LoopBaseSceneName))
            {
                Debug.LogError(
                    $"{nameof(BedGameStartTrigger)}: " +
                    $"{LoopBaseSceneName} 씬이 Build Settings에 없습니다.",
                    this);
                return;
            }

            StartCoroutine(LoadLoopBaseRoutine(enteredPlayer));
        }

        private IEnumerator LoadLoopBaseRoutine(
            PersistentPlayerRoot playerRoot)
        {
            _isStarting = true;

            ScreenFader screenFader =
                playerRoot.GetComponentInChildren<ScreenFader>(true);

            if (screenFader != null)
            {
                yield return screenFader.FadeOut(_fadeOutDuration);
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(
                LoopBaseSceneName,
                LoadSceneMode.Single);

            if (operation != null)
            {
                // 씬 로드가 시작되면 로비 오브젝트는 제거되고,
                // 이후 초기화는 LoopBase가 담당한다.
                yield break;
            }

            Debug.LogError(
                $"{nameof(BedGameStartTrigger)}: " +
                $"{LoopBaseSceneName} 씬 로드를 시작하지 못했습니다.",
                this);

            if (screenFader != null)
            {
                yield return screenFader.FadeIn(_fadeOutDuration);
            }

            _isStarting = false;
        }
    }
}