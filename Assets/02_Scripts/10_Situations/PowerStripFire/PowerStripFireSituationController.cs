using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations.PowerStripFire
{
    [DisallowMultipleComponent]
    public sealed class PowerStripFireSituationController : SituationController
    {
        [Header("References")]
        [SerializeField] private FireObject _fireObject;

        protected override void OnActivated()
        {
            if (_fireObject == null)
            {
                Debug.LogError("A power strip fire object must be assigned.", this);
                return;
            }

            _fireObject.OnExtinguished -= HandleFireExtinguished;
            _fireObject.OnExtinguished += HandleFireExtinguished;
        }

        protected override void OnResolved()
        {
            UnsubscribeFireEvent();
        }

        protected override void OnFailed()
        {
            UnsubscribeFireEvent();
        }

        protected override void OnReset()
        {
            UnsubscribeFireEvent();
        }

        private void OnDisable()
        {
            UnsubscribeFireEvent();
        }

        private void HandleFireExtinguished()
        {
            if (!ResolveSituation())
            {
                Debug.LogError(
                    "The power strip fire situation could not be resolved.",
                    this);
            }
        }

        private void UnsubscribeFireEvent()
        {
            if (_fireObject != null)
            {
                _fireObject.OnExtinguished -= HandleFireExtinguished;
            }
        }
    }
}
