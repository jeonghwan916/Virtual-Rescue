using System;
using UnityEngine;

public class EndingTrigger : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private float _holdSeconds = 2f;
    
    // 페이드아웃할 플레이어 눈 앞의 캔버스
    
    // 돌아갈 로비 씬
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered");
            
            // 효과음 재생 후
            
            // 몇초 대기 : _holdSeconds
            
            // 화면 페이드
            
            // 몇초 대기 : _holdSeconds
            
            // 씬 이동 : LobbyScene
        }
    }
}
