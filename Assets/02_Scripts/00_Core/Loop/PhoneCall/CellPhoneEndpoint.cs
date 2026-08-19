using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class CellPhoneEndpoint : MonoBehaviour
    {
        [SerializeField] private NumPad _numPad;
        [SerializeField] private ExitController _exitController;

        public NumPad NumPad => _numPad;

        private void OnEnable()
        {
            CellPhoneEndpointRegistry.Register(this);
        }

        private void OnDisable()
        {
            CellPhoneEndpointRegistry.Unregister(this);
        }

        public void RequestExit()
        {
            if (_exitController == null)
            {
                Debug.LogWarning(
                    $"{name}: CellPhone ExitController is not assigned.",
                    this);
                return;
            }

            _exitController.RequestExit();
        }

        private void Reset()
        {
            _numPad = GetComponentInChildren<NumPad>(true);
            _exitController = GetComponent<ExitController>();
        }

        private void OnValidate()
        {
            if (_numPad == null)
            {
                _numPad = GetComponentInChildren<NumPad>(true);
            }

            if (_exitController == null)
            {
                _exitController = GetComponent<ExitController>();
            }
        }
    }
}
