using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VirtualRescue.Effects;

public class PlayerReferenceHub : MonoBehaviour
{
    public static PlayerReferenceHub Instance { get; private set; }
    public event Action SceneReady;

    [Header("Player References")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private VignetteController _vignetteController;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private NearFarInteractor _leftNearFarInteractor;
    [SerializeField] private NearFarInteractor _rightNearFarInteractor;
    [SerializeField] private GameObject _leftLineVisual;
    [SerializeField] private GameObject _rightLineVisual;
    
    public AudioSource XrAudioSource => _audioSource;
    public VignetteController VignetteController => _vignetteController;
    public Transform PlayerTransform => _playerTransform;
    public NearFarInteractor LeftNearFarInteractor => _leftNearFarInteractor;
    public NearFarInteractor RightNearFarInteractor => _rightNearFarInteractor;
    public GameObject LeftLineVisual => _leftLineVisual;
    public GameObject RightLineVisual => _rightLineVisual;

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

        if (_leftNearFarInteractor == null || _rightNearFarInteractor == null)
        {
            Debug.LogWarning($"{nameof(PlayerReferenceHub)}: Near-Far Interactor references are missing.", this);
        }

        if (_leftLineVisual == null || _rightLineVisual == null)
        {
            Debug.LogWarning($"{nameof(PlayerReferenceHub)}: LineVisual references are missing.", this);
        }
    }
}
