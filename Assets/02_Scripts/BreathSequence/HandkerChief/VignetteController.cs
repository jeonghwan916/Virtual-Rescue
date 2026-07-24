using System;
using UnityEngine;
using System.Collections;

public class VignetteController : MonoBehaviour
{
    static readonly int ApertureSizeId = Shader.PropertyToID("_ApertureSize");

    [SerializeField] MeshRenderer vignetteRenderer;
    [SerializeField, Range(0f, 1f)] float startApertureSize = 0.5f;
    [SerializeField, Min(0f)] float wipeOutDuration = 0.3f;

    MaterialPropertyBlock propertyBlock;
    Coroutine wipeOutRoutine;
    float currentApertureSize;

    void Awake()
    {
        if (vignetteRenderer == null)
            vignetteRenderer = GetComponent<MeshRenderer>();

        propertyBlock = new MaterialPropertyBlock();
        //SetApertureSize(startApertureSize);
    }

    public void WipeOut()
    {
        if (wipeOutRoutine != null)
            StopCoroutine(wipeOutRoutine);

        wipeOutRoutine = StartCoroutine(WipeOutRoutine());
    }

    IEnumerator WipeOutRoutine()
    {
        var startSize = currentApertureSize;

        if (wipeOutDuration <= 0f)
        {
            SetApertureSize(1f);
            wipeOutRoutine = null;
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < wipeOutDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / wipeOutDuration);
            var easedT = 1f - (1f - t) * (1f - t);

            SetApertureSize(Mathf.Lerp(startSize, 1f, easedT));
            yield return null;
        }

        SetApertureSize(1f);
        wipeOutRoutine = null;
    }

    public void SetApertureSize(float apertureSize)
    {
        currentApertureSize = Mathf.Clamp01(apertureSize);

        if (vignetteRenderer == null)
            return;

        vignetteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(ApertureSizeId, currentApertureSize);
        vignetteRenderer.SetPropertyBlock(propertyBlock);
    }
}
