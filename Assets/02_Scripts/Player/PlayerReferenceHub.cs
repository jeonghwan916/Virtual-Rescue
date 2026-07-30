using System;
using UnityEngine;
using VirtualRescue.Effects;

public class PlayerReferenceHub : MonoBehaviour
{
    public static PlayerReferenceHub Instance { get; private set; }
    public event Action SceneReady;

    [Header("Player References")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private VignetteController _vignetteController;
    
    public AudioSource XrAudioSource => _audioSource;
    public VignetteController VignetteController => _vignetteController;

    public void NotifySceneReady()
    {
        SceneReady?.Invoke();
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ValidateReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ValidateReferences()
    {
        if (_audioSource == null)
        {
            Debug.LogWarning($"{nameof(PlayerReferenceHub)}: AudioSource is not assigned.", this);
        }

        if (_vignetteController == null)
        {
            Debug.LogWarning($"{nameof(PlayerReferenceHub)}: VignetteController is not assigned.", this);
        }
    }
}
