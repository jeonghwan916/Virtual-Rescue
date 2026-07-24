using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VirtualRescue.Interaction
{
    public sealed class DoorHandleHeatVisual : MonoBehaviour
    {
        [SerializeField]
        private DoorHandleTemperature _handleTemperature;

        [SerializeField]
        private XRSimpleInteractable _handleInteractable;

        [SerializeField]
        private Renderer _handleRenderer;

        [SerializeField]
        private Color _dangerBaseColor =
            new Color(1f, 0.18f, 0.02f, 1f);

        [SerializeField, ColorUsage(true, true)]
        private Color _dangerEmissionColor =
            new Color(4f, 0.6f, 0f, 1f);

        [SerializeField, Min(0.01f)]
        private float _fadeDuration = 1.5f;

        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _propertyBlock;
        private Color _normalBaseColor;

        private float _currentAmount;
        private int _hoverCount;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();

            if (_handleRenderer != null &&
                _handleRenderer.sharedMaterial != null &&
                _handleRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                _normalBaseColor =
                    _handleRenderer.sharedMaterial.GetColor(BaseColorId);
            }
            else
            {
                _normalBaseColor = Color.gray;
            }

            _currentAmount = 0f;
            ApplyColor();
        }

        private void OnEnable()
        {
            if (_handleInteractable == null)
            {
                return;
            }

            _handleInteractable.hoverEntered.AddListener(
                OnHoverEntered);

            _handleInteractable.hoverExited.AddListener(
                OnHoverExited);
        }

        private void OnDisable()
        {
            if (_handleInteractable != null)
            {
                _handleInteractable.hoverEntered.RemoveListener(
                    OnHoverEntered);

                _handleInteractable.hoverExited.RemoveListener(
                    OnHoverExited);
            }

            _hoverCount = 0;
            _currentAmount = 0f;

            ApplyColor();
        }

        private void Update()
        {
            bool shouldBeOrange =
                _handleTemperature != null &&
                _handleTemperature.IsDangerous &&
                _hoverCount > 0;

            float targetAmount =
                shouldBeOrange ? 1f : 0f;

            float fadeSpeed =
                1f / _fadeDuration;

            _currentAmount = Mathf.MoveTowards(
                _currentAmount,
                targetAmount,
                fadeSpeed * Time.deltaTime);

            ApplyColor();
        }

        private void OnHoverEntered(
            HoverEnterEventArgs args)
        {
            _hoverCount++;

            Debug.Log(
                $"[{name}] 손잡이 Hover 시작 | " +
                $"위험: {_handleTemperature != null && _handleTemperature.IsDangerous}",
                this);
        }

        private void OnHoverExited(
            HoverExitEventArgs args)
        {
            _hoverCount =
                Mathf.Max(0, _hoverCount - 1);
        }

        private void ApplyColor()
        {
            if (_handleRenderer == null ||
                _propertyBlock == null)
            {
                return;
            }

            Color baseColor = Color.Lerp(
                _normalBaseColor,
                _dangerBaseColor,
                _currentAmount);

            Color emissionColor = Color.Lerp(
                Color.black,
                _dangerEmissionColor,
                _currentAmount);

            _handleRenderer.GetPropertyBlock(
                _propertyBlock);

            _propertyBlock.SetColor(
                BaseColorId,
                baseColor);

            _propertyBlock.SetColor(
                EmissionColorId,
                emissionColor);

            _handleRenderer.SetPropertyBlock(
                _propertyBlock);
        }
    }
}
