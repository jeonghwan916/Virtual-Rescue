using UnityEngine;

namespace VirtualRescue.SmokeStairs
{
    public class SmokeStairsQuestTrigger : MonoBehaviour
    {
        [SerializeField] private SmokeStairsQuestManager _questManager;
        [SerializeField] private SmokeStairsQuestStep _questStep;

        private bool _hasTriggered;

        private void Awake()
        {
            if (_questManager == null)
            {
                _questManager = GetComponentInParent<SmokeStairsQuestManager>();
            }

            Collider triggerCollider = GetComponent<Collider>();
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning(
                    "Alarm Stairs 트리거의 Collider에서 Is Trigger를 활성화하세요.",
                    this);
            }

            if (_questManager == null)
            {
                Debug.LogWarning(
                    "Alarm Stairs 트리거에서 퀘스트 매니저를 찾을 수 없습니다.",
                    this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered || _questManager == null)
            {
                return;
            }

            if (other.GetComponentInParent<CharacterController>() == null)
            {
                return;
            }

            if (_questManager.TryAdvance(_questStep))
            {
                _hasTriggered = true;
            }
        }
    }
}
