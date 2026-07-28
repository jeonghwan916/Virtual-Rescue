using UnityEngine;

public sealed class HoseNozzleTargetFollower : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private bool _followPosition = true;
    [SerializeField] private bool _followRotation = true;

    private void Update()
    {
        FollowTarget();
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (_target == null)
        {
            return;
        }

        if (_followPosition)
        {
            transform.position = _target.position;
        }

        if (_followRotation)
        {
            transform.rotation = _target.rotation;
        }
    }
}
