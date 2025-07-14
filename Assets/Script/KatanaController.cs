using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class KatanaController : MonoBehaviour
{
    public Transform handPosition; // 손 위치
    public Transform handPosition_Reverse;
    public Transform sheathedPosition; // 칼집 위치
    private ParentConstraint parentConstraint; // ParentConstraint 컴포넌트

    private ConstraintSource handSource; // 손 위치를 위한 ConstraintSource
    private ConstraintSource handSource_Reverse;
    private ConstraintSource sheathedSource; // 칼집 위치를 위한 ConstraintSource

    void Start()
    {
        // 칼 오브젝트에 ParentConstraint가 없다면, 추가합니다
        parentConstraint = GetComponent<ParentConstraint>();
        if (parentConstraint == null)
        {
            Debug.LogError("ParentConstraint Missing");
        }

        // 미리 ConstraintSource들을 캐싱해둡니다
        handSource = new ConstraintSource();
        handSource.sourceTransform = handPosition;
        handSource.weight = 1.0f;

        handSource_Reverse = new ConstraintSource();
        handSource_Reverse.sourceTransform = handPosition_Reverse;
        handSource_Reverse.weight = 1.0f;

        sheathedSource = new ConstraintSource();
        sheathedSource.sourceTransform = sheathedPosition;
        sheathedSource.weight = 1.0f;
    }

    // 무기를 손에 장착하는 함수
    public void EquipToHand()
    {
        // 무기의 부모를 손 위치로 설정
        transform.SetParent(handPosition);
        transform.localPosition = Vector3.zero; // 위치 초기화
        transform.localRotation = Quaternion.identity; // 회전 초기화

        // ParentConstraint의 소스를 handPosition으로 설정
        List<ConstraintSource> sources = new List<ConstraintSource> { handSource };
        parentConstraint.SetSources(sources);

        // ParentConstraint 활성화 (필요한 경우)
        parentConstraint.constraintActive = true;
    }

    public void EquipToHand_Reverse()
    {
        // 무기의 부모를 손 위치로 설정
        transform.SetParent(handPosition_Reverse);
        transform.localPosition = Vector3.zero; // 위치 초기화
        transform.localRotation = Quaternion.identity; // 회전 초기화

        // ParentConstraint의 소스를 handPosition으로 설정
        List<ConstraintSource> sources = new List<ConstraintSource> { handSource_Reverse };
        parentConstraint.SetSources(sources);

        // ParentConstraint 활성화 (필요한 경우)
        parentConstraint.constraintActive = true;
    }

    // 무기를 칼집에 넣는 함수
    public void Sheathe()
    {
        // 무기의 부모를 칼집 위치로 설정
        transform.SetParent(sheathedPosition);
        transform.localPosition = Vector3.zero; // 위치 초기화
        transform.localRotation = Quaternion.identity; // 회전 초기화

        // ParentConstraint의 소스를 sheathedPosition으로 설정
        List<ConstraintSource> sources = new List<ConstraintSource> { sheathedSource };
        parentConstraint.SetSources(sources);

        // ParentConstraint 활성화 (필요한 경우)
        parentConstraint.constraintActive = true;
    }
}
