using System;
using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.QuestGuide
{
    public class QuestGuideManager : MonoBehaviour
    {
        [SerializeField] private QuestGuideController _questGuideController;
        [SerializeField] private GuideSequence[] _guideSequences;

        private void Start()
        {
            _questGuideController.Show(_guideSequences[0]);
        }
    }
}
