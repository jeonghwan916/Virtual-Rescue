using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.Situations.FireSuppression;

namespace VirtualRescue.Situations.KitchenOilFire
{
    [DisallowMultipleComponent]
    public sealed class KitchenOilFireSituationController :
        FireSuppressionSituationController
    {
        [Header("References")]
        [SerializeField] private FireObject _oilFire;

        private bool _warningRaised;

        protected override void PrepareActiveFireObjects(
            List<FireObject> activeFireObjects)
        {
            _warningRaised = false;

            if (_oilFire == null)
            {
                Debug.LogError("A kitchen oil fire object must be assigned.", this);
                return;
            }

            activeFireObjects.Add(_oilFire);
        }

        protected override void OnFireSuppressionActivated()
        {
            _oilFire.TemporarySuppressionLimitReached +=
                HandleTemporarySuppressionLimitReached;
        }

        protected override void OnFireSuppressionDeactivated()
        {
            if (_oilFire == null)
            {
                return;
            }

            _oilFire.TemporarySuppressionLimitReached -=
                HandleTemporarySuppressionLimitReached;
        }

        private void HandleTemporarySuppressionLimitReached(
            FireSuppressantType suppressantType)
        {
            if (!IsActive ||
                _warningRaised ||
                suppressantType != FireSuppressantType.GeneralPurpose)
            {
                return;
            }

            _warningRaised = true;
            RaiseWarning();
        }
    }
}
