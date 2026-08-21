using System.Collections;
using UnityEngine;
using VirtualRescue.GameFlow;

public class ElevatorTrigger : MonoBehaviour
{ 
    private static readonly int ButtonPressedHash = Animator.StringToHash("ButtonPressed");
    private static readonly int EleavatorAnimHash = Animator.StringToHash("EleavatorAnim");
    
    [SerializeField] private ExitController _exitController;
    [SerializeField] private LayerMask handLayerMask;
    private bool _hasTriggered;
    [SerializeField] private Animator _animator;
    [SerializeField] private AudioSource _elevatorAudioSource;
    [SerializeField] private AudioClip _elevatorButtonClickAudioClip;
    [SerializeField] private AudioClip _elevatorDingDongAudioClip;
    [SerializeField] private AudioClip _elevatorOpenAudioClip;

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
        if (_animator == null)
        {
            Debug.LogError("ElevatorTrigger requires an Animator.", this);
            _hasTriggered = false;
            return;
        }

        if (_elevatorAudioSource != null && _elevatorButtonClickAudioClip != null)
        {
            _elevatorAudioSource.PlayOneShot(_elevatorButtonClickAudioClip);
        }

        if (ExitController.ShouldBlockExitAnimation(_exitController.Type))
        {
            _hasTriggered = false;
            return;
        }
        
        StartCoroutine(RequestExitAfterElevatorAnimation());
    }

    private IEnumerator RequestExitAfterElevatorAnimation()
    {
        _animator.SetTrigger(ButtonPressedHash);

        yield return null;

        while (_animator.IsInTransition(0) ||
               _animator.GetCurrentAnimatorStateInfo(0).shortNameHash != EleavatorAnimHash)
        {
            yield return null;
        }

        while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        _exitController.RequestExit();
    }

}
