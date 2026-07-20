using System;
using UnityEngine;

public class HandkerChiefEnterTrigger : MonoBehaviour
{
    // 인스펙터 창에서 감지하고 싶은 레이어를 선택하세요.
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private VignetteController _vignetteController;

    private void OnTriggerEnter(Collider other)
    {
        // 비트 연산을 통해 충돌한 오브젝트의 레이어가 targetLayer에 포함되어 있는지 확인합니다.
        if (((1 << other.gameObject.layer) & _targetLayer) != 0)
        {
            Debug.Log($"지정한 레이어와 충돌했습니다! : {other.gameObject.name}");

            if (other.GetComponent<HandkerChiefWet>().IsCompletelyWet)
            {
                Debug.Log("다 젖은 손수건을 입에 갖다 댐");
                _vignetteController.WipeOut();
            }
        }
    }
}