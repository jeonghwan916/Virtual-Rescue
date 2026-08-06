using System;
using UnityEngine;
using VirtualRescue.GameFlow;

public class Scenario_TestCubeSituationController : SituationController
{
    [SerializeField] private GameObject _testCube;

    private void Start()
    {
        Invoke("EnableCube", 3f);
    }

    private void EnableCube()
    {
        _testCube.SetActive(true);
    }
}
