using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class VRHandAnimator : MonoBehaviour
{
    public bool IsLeftHand = true;

    [Header("Grip")]
    [SerializeField]
    private XRInputValueReader<float> m_GripInput = new XRInputValueReader<float>("Grip");

    [Header("Trigger")]
    [SerializeField]
    private XRInputValueReader<float> m_TriggerInput = new XRInputValueReader<float>("Trigger");

    private Animator _animator;


    private void Awake()
    {
        _animator = GetComponent<Animator>();

    }

    void OnEnable()
    {
        //m_StickInput?.EnableDirectActionIfModeUsed();
        m_TriggerInput?.EnableDirectActionIfModeUsed();
        m_GripInput?.EnableDirectActionIfModeUsed();
    }

    void OnDisable()
    {
        //m_StickInput?.DisableDirectActionIfModeUsed();
        m_TriggerInput?.DisableDirectActionIfModeUsed();
        m_GripInput?.DisableDirectActionIfModeUsed();
    }

    void LateUpdate()
    {
        // 0.0f ~ 1.0f
        var gripVal = m_GripInput.ReadValue();
        var triggerVal = m_TriggerInput.ReadValue();

        if (IsLeftHand)
        {
            _animator.SetFloat("Left_Grip", gripVal);
            _animator.SetFloat("Left_Trigger", triggerVal);
        }
        else
        {
            _animator.SetFloat("Right_Grip", gripVal);
            _animator.SetFloat("Right_Trigger", triggerVal);
        }



    }
}
