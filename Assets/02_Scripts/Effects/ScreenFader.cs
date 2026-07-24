using System.Collections;
using UnityEngine;

namespace VirtualRescue.Effects
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private bool _useUnscaledTime = true;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            SetAlpha(0f);
        }

        public IEnumerator FadeIn(float duration)
        {
            SetFadeObjectActive(true);
            yield return Fade(1f, 0f, duration);
            SetFadeObjectActive(false);
        }

        public IEnumerator FadeOut(float duration)
        {
            SetFadeObjectActive(true);
            yield return Fade(0f, 1f, duration);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            SetAlpha(from);

            if (duration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            _canvasGroup.alpha = alpha;
            _canvasGroup.blocksRaycasts = alpha > 0.01f;
            _canvasGroup.interactable = false;
        }

        private void SetFadeObjectActive(bool isActive)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.gameObject.SetActive(isActive);
        }
    }
}
