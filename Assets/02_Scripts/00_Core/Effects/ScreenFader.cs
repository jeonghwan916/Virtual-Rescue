using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualRescue.Effects
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Graphic _fadeGraphic;
        [SerializeField] private bool _useUnscaledTime = true;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_fadeGraphic == null)
            {
                _fadeGraphic = GetComponentInChildren<Graphic>(true);
            }

            SetAlpha(0f);
        }

        public IEnumerator FadeIn(float duration)
        {
            SetColor(Color.black);
            SetFadeObjectActive(true);
            yield return Fade(1f, 0f, duration);
            SetFadeObjectActive(false);
        }

        public IEnumerator FadeOut(float duration)
        {
            SetColor(Color.black);
            SetFadeObjectActive(true);
            yield return Fade(0f, 1f, duration);
        }

        public IEnumerator FlashWhite(
            float duration,
            int pulseCount,
            float peakAlpha)
        {
            SetColor(Color.white);
            SetFadeObjectActive(true);

            int validPulseCount = Mathf.Max(1, pulseCount);
            float halfPulseDuration = duration / (validPulseCount * 2f);
            float validPeakAlpha = Mathf.Clamp01(peakAlpha);

            for (int index = 0; index < validPulseCount; index++)
            {
                yield return Fade(0f, validPeakAlpha, halfPulseDuration);
                yield return Fade(validPeakAlpha, 0f, halfPulseDuration);
            }

            SetColor(Color.black);
            SetAlpha(0f);
        }

        public void ShowBlack()
        {
            SetFadeObjectActive(true);
            SetAlpha(1f);
        }

        public void Clear()
        {
            SetAlpha(0f);
            SetFadeObjectActive(false);
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

        private void SetColor(Color color)
        {
            if (_fadeGraphic != null)
            {
                _fadeGraphic.color = color;
            }
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
