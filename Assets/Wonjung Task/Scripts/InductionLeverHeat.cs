using UnityEngine;

namespace VirtualRescue.Interaction
{
    public sealed class InductionLeverHeat : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");

        [Header("References")]
        [SerializeField] private Transform _lever;
        [SerializeField] private Renderer _heatCylinderRenderer;

        [Header("Lever Angles")]
        [SerializeField] private float _startOnAngle = 30f;
        [SerializeField] private float _offAngle = 5f;
        [SerializeField] private float _onAngle = 25f;

        [Header("Colors")]
        [SerializeField] private Color _offColor =
            new Color(0.15f, 0.15f, 0.15f, 1f);

        [SerializeField] private Color _onColor =
            Color.red;

        [ColorUsage(true, true)]
        [SerializeField] private Color _onEmissionColor =
            new Color(4f, 0.1f, 0.02f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private Material _material;
        private int _colorPropertyId;
        private bool _isHeatOn;

        public bool IsHeatOn => _isHeatOn;

        private void Awake()
        {
            if (_lever == null ||
                _heatCylinderRenderer == null)
            {
                Debug.LogError(
                    $"[{name}] Lever 또는 Heat Renderer가 연결되지 않았습니다.",
                    this);

                enabled = false;
                return;
            }

            _material =
                _heatCylinderRenderer.sharedMaterial;

            if (_material == null)
            {
                Debug.LogError(
                    $"[{name}] Heat Renderer에 Material이 없습니다.",
                    this);

                enabled = false;
                return;
            }

            _colorPropertyId =
                _material.HasProperty(BaseColorId)
                    ? BaseColorId
                    : ColorId;

            _propertyBlock =
                new MaterialPropertyBlock();

            // 오른쪽으로 돌아간 상태에서 시작
            Vector3 startRotation =
                _lever.localEulerAngles;

            startRotation.y = _startOnAngle;
            _lever.localEulerAngles = startRotation;

            // 가열된 상태로 시작
            SetHeatState(true);
        }

        private void Update()
        {
            float leverAngle =
                Mathf.DeltaAngle(
                    0f,
                    _lever.localEulerAngles.y);

            if (_isHeatOn &&
                leverAngle <= _offAngle)
            {
                SetHeatState(false);
            }
            else if (!_isHeatOn &&
                     leverAngle >= _onAngle)
            {
                SetHeatState(true);
            }
        }

        private void SetHeatState(bool isHeatOn)
        {
            _isHeatOn = isHeatOn;

            _heatCylinderRenderer.GetPropertyBlock(
                _propertyBlock);

            _propertyBlock.SetColor(
                _colorPropertyId,
                isHeatOn ? _onColor : _offColor);

            if (_material.HasProperty(EmissionColorId))
            {
                _propertyBlock.SetColor(
                    EmissionColorId,
                    isHeatOn
                        ? _onEmissionColor
                        : Color.black);
            }

            _heatCylinderRenderer.SetPropertyBlock(
                _propertyBlock);
        }
    }
}