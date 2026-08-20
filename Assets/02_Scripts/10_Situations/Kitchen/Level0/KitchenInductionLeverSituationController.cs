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
            if (_leverHeat == null)
            {
                return;
            }

            if (IsActive && !_leverHeat.IsHeatOn)
            {
                StageClear();
                return;
            }

            if (IsResolved && _leverHeat.IsHeatOn)
            {
                ReturnToActive();
            }
        }

        private void StageClear()
        {
            ResolveSituation();
        }

        private void ReturnToActive()
        {
            SituationDefinition definition = Definition;

            if (definition == null)
            {
                return;
            }

            ResetSituation();
            Activate(definition);
        }
    }
}