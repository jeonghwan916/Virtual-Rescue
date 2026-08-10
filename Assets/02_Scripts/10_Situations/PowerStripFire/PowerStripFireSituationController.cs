using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.Situations.FireSuppression;

namespace VirtualRescue.Situations.PowerStripFire
{
    [DisallowMultipleComponent]
    public sealed class PowerStripFireSituationController :
        FireSuppressionSituationController
    {
        [Header("References")]
        [SerializeField] private FireObject _fireObject;

        protected override void PrepareActiveFireObjects(
            List<FireObject> activeFireObjects)
        {
            if (_fireObject == null)
            {
                Debug.LogError("A power strip fire object must be assigned.", this);
                return;
            }

            activeFireObjects.Add(_fireObject);
        }
    }
}
