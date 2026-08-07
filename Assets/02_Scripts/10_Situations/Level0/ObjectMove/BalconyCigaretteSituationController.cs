using UnityEngine;
using VirtualRescue.GameFlow;

public class BalconyCigaretteSituationController : SituationController
{
    public void OnCigaretteEnteredAshtray()
    {
        ResolveSituation();
    }

    public void OnCigaretteExitedAshtray()
    {
        FailSituation();
    }
}
