using System;
using UnityEngine;
using System.Collections;
using VirtualRescue.SmokeStairs;
using VirtualRescue.Missions02;

public class VignetteController : MonoBehaviour
{
    static readonly int ApertureSizeId = Shader.PropertyToID("_ApertureSize");
    static readonly int FeatheringEffectId = Shader.PropertyToID("_FeatheringEffect");

    [SerializeField] MeshRenderer vignetteRenderer;
    [SerializeField, Range(0f, 1f)] float startApertureSize = 0.5f;
    [SerializeField, Range(0f, 1f)] float featheringEffect = 0.3f;
    [SerializeField, Min(0f)] float wipeOutDuration = 0.3f;

    MaterialPropertyBlock propertyBlock;
    Coroutine wipeOutRoutine;
    float currentApertureSize;

    void Awake()
    {
        if (vignetteRenderer == null)
            vignetteRenderer = GetComponent<MeshRenderer>();

        propertyBlock = new MaterialPropertyBlock();
    }
    

    public void WipeOut()
    {
        if (wipeOutRoutine != null)
            StopCoroutine(wipeOutRoutine);

        wipeOutRoutine = StartCoroutine(WipeOutRoutine());

        SmokeStairsQuestManager smokeStairsQuestManager = Mission02References.SmokeStairsQuestManager;
        smokeStairsQuestManager.TryAdvance(SmokeStairsQuestStep.Exit);
    }

    public void WipeIn()
    {
        if (wipeOutRoutine != null)
            StopCoroutine(wipeOutRoutine);

        wipeOutRoutine = StartCoroutine(WipeInRoutine());
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

    IEnumerator WipeInRoutine()
    {
        var startSize = currentApertureSize;

        if (wipeOutDuration <= 0f)
        {
            SetApertureSize(startApertureSize);
            wipeOutRoutine = null;
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < wipeOutDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / wipeOutDuration);
            var easedT = 1f - (1f - t) * (1f - t);

            SetApertureSize(Mathf.Lerp(startSize, startApertureSize, easedT));
            yield return null;
        }

        SetApertureSize(startApertureSize);
        wipeOutRoutine = null;
    }

    public void SetApertureSize(float apertureSize)
    {
        currentApertureSize = Mathf.Clamp01(apertureSize);

        if (vignetteRenderer == null)
            return;

        vignetteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(ApertureSizeId, currentApertureSize);
        propertyBlock.SetFloat(FeatheringEffectId, featheringEffect);
        vignetteRenderer.SetPropertyBlock(propertyBlock);
    }
}
