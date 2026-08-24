using System;
using System.Collections;
using UnityEngine;

public class EndingSceneController : MonoBehaviour
{
    [Header("Eleavator")]
    [SerializeField] private Animator _eleavatorAnimator;
    [SerializeField] private AudioSource _eleavatorAudioSrc;
    [SerializeField] private AudioClip _eleavatorOpeningClip;
    
    private static readonly int ButtonPressedHash = Animator.StringToHash("ButtonPressed");
    private static readonly int EleavatorAnimHash = Animator.StringToHash("EleavatorAnim");
    private const float DoorOpeningNormalizedTime = 3f / 7f;


    private void Start()
    {
        OpeningEleavator();
    }

    private void OpeningEleavator()
    {
        StartCoroutine(RequestExitAfterElevatorAnimation());
    }
    
    private IEnumerator RequestExitAfterElevatorAnimation()
    {
        _eleavatorAnimator.SetTrigger(ButtonPressedHash);

        yield return null;

        while (_eleavatorAnimator.IsInTransition(0) ||
               _eleavatorAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash != EleavatorAnimHash)
        {
            yield return null;
        }

        while (_eleavatorAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < DoorOpeningNormalizedTime)
        {
            yield return null;
        }

        //_eleavatorAudioSrc.PlayOneShot(_eleavatorOpeningClip);

        while (_eleavatorAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
    }
}
