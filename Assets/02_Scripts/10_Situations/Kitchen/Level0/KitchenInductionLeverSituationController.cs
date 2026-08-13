using UnityEngine;
using VirtualRescue.GameFlow;
using VirtualRescue.Interaction;

namespace VirtualRescue.Situations
{
    public sealed class KitchenInductionLeverSituationController
        : SituationController
    {
        [SerializeField]
        private InductionLeverHeat _leverHeat;

        private void Update()
        {
            if (!IsActive || _leverHeat == null)
            {
                return;
            }

            bool isEmissionOff = !_leverHeat.IsHeatOn;

            if (isEmissionOff)
            {
                ResolveSituation();
            }
        }
    }
}
