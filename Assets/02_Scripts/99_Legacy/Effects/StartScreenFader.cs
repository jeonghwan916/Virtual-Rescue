using System.Collections;
using UnityEngine;

namespace VirtualRescue.Effects
{
    public sealed class StartScreenFader : MonoBehaviour
    {
        [SerializeField] private ScreenFader _screenFader;
        [SerializeField] private float _fadeDuration = 1f;

        private IEnumerator Start()
        {
            if (_screenFader == null)
            {
                _screenFader = GetComponentInChildren<ScreenFader>();
            }

            if (_screenFader == null)
            {
                yield break;
            }

            yield return _screenFader.FadeIn(_fadeDuration);
        }
    }
}
