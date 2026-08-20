using System;
using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations
{
    public sealed class BalconyFishBowlSituationController : SituationController
    {
        [SerializeField] private GameObject _bowl;
        [SerializeField] private bool _isBowlExited = false;
        
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject == _bowl)
            {
                if (!_isBowlExited)
                {
                    _isBowlExited = true;
                    ResolveSituation();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == _bowl &&
                _isBowlExited &&
                ReopenResolvedSituation())
            {
                _isBowlExited = false;
                RaiseWarning();
            }
        }
    }
}
