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

        protected override void PrepareActiveFireObjects(
            List<FireObject> activeFireObjects)
        {
            if (_oilFire == null)
            {
                Debug.LogError("A kitchen oil fire object must be assigned.", this);
                return;
            }

            activeFireObjects.Add(_oilFire);
        }
    }
}
