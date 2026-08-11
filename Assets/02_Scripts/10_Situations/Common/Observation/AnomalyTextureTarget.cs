using UnityEngine;

namespace VirtualRescue.Situations.AnomalyObservation
{
    [DisallowMultipleComponent]
    public class AnomalyTextureTarget : MonoBehaviour
    {
        private const string DefaultTexturePropertyName = "_BaseMap";

        [Header("Renderer")]
        [SerializeField] private Renderer _targetRenderer;
        [Min(0)]
        [SerializeField] private int _materialIndex;
        [SerializeField] private string _texturePropertyName = DefaultTexturePropertyName;

        [Header("Textures")]
        [SerializeField] private Texture _anomalyTexture;
        [SerializeField] private Texture _normalTexture;

        private MaterialPropertyBlock _propertyBlock;

        public Renderer TargetRenderer => _targetRenderer;

        private void Awake()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<Renderer>();
            }

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnValidate()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<Renderer>();
            }

            _materialIndex = Mathf.Max(0, _materialIndex);

            if (string.IsNullOrWhiteSpace(_texturePropertyName))
            {
                _texturePropertyName = DefaultTexturePropertyName;
            }
        }

        public virtual bool TryApplyAnomalyTexture()
        {
            return TryApplyTexture(_anomalyTexture, "anomaly");
        }

        public virtual bool TryApplyNormalTexture()
        {
            return TryApplyTexture(_normalTexture, "normal");
        }

        public bool IsTargetCollider(Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return false;
            }

            AnomalyTextureTarget target =
                targetCollider.GetComponentInParent<AnomalyTextureTarget>();

            return target == this;
        }

        private bool TryApplyTexture(Texture texture, string textureRole)
        {
            if (!TryValidateSettings(texture, textureRole, out int propertyId))
            {
                return false;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _targetRenderer.GetPropertyBlock(_propertyBlock, _materialIndex);
            _propertyBlock.SetTexture(propertyId, texture);
            _targetRenderer.SetPropertyBlock(_propertyBlock, _materialIndex);
            return true;
        }

        private bool TryValidateSettings(
            Texture texture,
            string textureRole,
            out int propertyId)
        {
            propertyId = Shader.PropertyToID(_texturePropertyName);

            if (_targetRenderer == null)
            {
                Debug.LogError("Anomaly texture target requires a Renderer.", this);
                return false;
            }

            Material[] materials = _targetRenderer.sharedMaterials;

            if (_materialIndex < 0 || _materialIndex >= materials.Length)
            {
                Debug.LogError(
                    $"Material index {_materialIndex} is outside the Renderer material range.",
                    this);
                return false;
            }

            Material material = materials[_materialIndex];

            if (material == null || !material.HasProperty(propertyId))
            {
                Debug.LogError(
                    $"Material does not contain texture property '{_texturePropertyName}'.",
                    this);
                return false;
            }

            if (texture == null)
            {
                Debug.LogError($"The {textureRole} texture is not assigned.", this);
                return false;
            }

            return true;
        }
    }
}
