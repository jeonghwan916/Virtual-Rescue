using UnityEngine;

public class HandkerChiefWet : MonoBehaviour
{
    [SerializeField] private Material _handkerChiefMaterial;
    [SerializeField] private Renderer _handkerChiefRenderer;

    private Material _runtimeMaterial;
    private bool _isCompletelyWet;

    public bool IsCompletelyWet => _isCompletelyWet;

    private void Awake()
    {
        if (_handkerChiefRenderer == null)
            _handkerChiefRenderer = GetComponent<Renderer>();

        if (_handkerChiefMaterial != null)
        {
            _runtimeMaterial = new Material(_handkerChiefMaterial);

            if (_handkerChiefRenderer != null)
                _handkerChiefRenderer.material = _runtimeMaterial;
        }
        else if (_handkerChiefRenderer != null)
        {
            _runtimeMaterial = _handkerChiefRenderer.material;
        }
    }

    public void ApplyWet(float blueAmount)
    {
        if (_isCompletelyWet || _runtimeMaterial == null)
            return;

        Color color = GetMaterialColor();
        color.b = Mathf.MoveTowards(color.b, 1f, Mathf.Max(0f, blueAmount));
        SetMaterialColor(color);

        if (Mathf.Approximately(color.b, 1f))
        {
            _isCompletelyWet = true;
            CompletlyWet();
        }
    }

    public void CompletlyWet()
    {
    }

    private Color GetMaterialColor()
    {
        if (_runtimeMaterial.HasProperty("_BaseColor"))
            return _runtimeMaterial.GetColor("_BaseColor");

        if (_runtimeMaterial.HasProperty("_Color"))
            return _runtimeMaterial.GetColor("_Color");

        return Color.white;
    }

    private void SetMaterialColor(Color color)
    {
        if (_runtimeMaterial.HasProperty("_BaseColor"))
            _runtimeMaterial.SetColor("_BaseColor", color);
        else if (_runtimeMaterial.HasProperty("_Color"))
            _runtimeMaterial.SetColor("_Color", color);
    }
}
