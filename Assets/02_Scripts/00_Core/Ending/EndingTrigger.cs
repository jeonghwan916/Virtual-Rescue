using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EndingTrigger : MonoBehaviour
{
    [SerializeField] private EndingSceneController _endingSceneController;
    [SerializeField] private Collider _triggerCollider;

    private bool _hasTriggered;

    private void Awake()
    {
        if (_triggerCollider == null)
        {
            _triggerCollider = GetComponent<Collider>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered || !other.CompareTag("Player"))
        {
            return;
        }

        if (_endingSceneController == null)
        {
            Debug.LogError(
                $"{nameof(EndingTrigger)}: EndingSceneController is not assigned.",
                this);
            return;
        }

        if (!_endingSceneController.RequestEnding())
        {
            return;
        }

        _hasTriggered = true;

        if (_triggerCollider != null)
        {
            _triggerCollider.enabled = false;
        }
    }
}
