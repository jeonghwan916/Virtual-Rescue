using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class CellPhoneEndpoint : MonoBehaviour
    {
        [SerializeField] private CellPhoneScreen _screen;
        [SerializeField] private NumPad _numPad;
        [SerializeField] private ExitController _exitController;

        public CellPhoneScreen Screen => _screen;
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
            _screen = GetComponentInChildren<CellPhoneScreen>(true);
            _numPad = GetComponentInChildren<NumPad>(true);
            _exitController = GetComponent<ExitController>();
        }

        private void OnValidate()
        {
            if (_screen == null)
            {
                _screen = GetComponentInChildren<CellPhoneScreen>(true);
            }

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
