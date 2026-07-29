using System;
using UnityEngine;

public class PlayerReferenceHub : MonoBehaviour
{
    public static PlayerReferenceHub Instance { get; private set; }

    [Header("Player References")]
    [SerializeField] private VignetteController _vignetteController;
    
    public VignetteController VignetteController => _vignetteController;
    
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
        if (_vignetteController == null)
        {
            Debug.LogWarning($"{nameof(PlayerReferenceHub)}: VignetteController is not assigned.", this);
        }
    }
}
