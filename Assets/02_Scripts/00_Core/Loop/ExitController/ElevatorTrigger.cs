using UnityEngine;
using VirtualRescue.GameFlow;

public class ElevatorTrigger : MonoBehaviour
{ 
    [SerializeField] private ExitController _exitController;
    [SerializeField] private LayerMask handLayerMask;
    private bool _hasTriggered;
    //[SerializeField] private Animator _animator;
    [SerializeField] private AudioSource _elevatorAudioSource;
    [SerializeField] private AudioClip _elevatorButtonClickAudioClip;
    [SerializeField] private AudioClip _elevatorDingDongAudioClip;
    //[SerializeField] private bool _isTrap;
    //[SerializeField] private ParticleSystem _fireParticle;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;

        if (handLayerMask.value == 0)
        {
            Debug.LogWarning("ElevatorTrigger hand layer mask is empty.", this);
            return;
        }
        
        if ((handLayerMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (_exitController == null)
        {
            Debug.LogError("ElevatorTrigger requires an ExitController.", this);
            return;
        }

        _hasTriggered = true;
        OnElevatorButtonPressed();
    }

    private void OnElevatorButtonPressed()
    {

        // 여기서 원하는 동작 실행
        _elevatorAudioSource.PlayOneShot(_elevatorButtonClickAudioClip);
        _elevatorAudioSource.PlayOneShot(_elevatorDingDongAudioClip);
        _exitController.RequestExit();
    }

}
