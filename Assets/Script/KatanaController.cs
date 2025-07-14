using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class KatanaController : MonoBehaviour
{
    public Transform handPosition; // �� ��ġ
    public Transform handPosition_Reverse;
    public Transform sheathedPosition; // Į�� ��ġ
    private ParentConstraint parentConstraint; // ParentConstraint ������Ʈ

    private ConstraintSource handSource; // �� ��ġ�� ���� ConstraintSource
    private ConstraintSource handSource_Reverse;
    private ConstraintSource sheathedSource; // Į�� ��ġ�� ���� ConstraintSource

    void Start()
    {
        // Į ������Ʈ�� ParentConstraint�� ���ٸ�, �߰��մϴ�
        parentConstraint = GetComponent<ParentConstraint>();
        if (parentConstraint == null)
        {
            Debug.LogError("ParentConstraint Missing");
        }

        // �̸� ConstraintSource���� ĳ���صӴϴ�
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

    // ���⸦ �տ� �����ϴ� �Լ�
    public void EquipToHand()
    {
        // ������ �θ� �� ��ġ�� ����
        transform.SetParent(handPosition);
        transform.localPosition = Vector3.zero; // ��ġ �ʱ�ȭ
        transform.localRotation = Quaternion.identity; // ȸ�� �ʱ�ȭ

        // ParentConstraint�� �ҽ��� handPosition���� ����
        List<ConstraintSource> sources = new List<ConstraintSource> { handSource };
        parentConstraint.SetSources(sources);

        // ParentConstraint Ȱ��ȭ (�ʿ��� ���)
        parentConstraint.constraintActive = true;
    }

    public void EquipToHand_Reverse()
    {
        // ������ �θ� �� ��ġ�� ����
        transform.SetParent(handPosition_Reverse);
        transform.localPosition = Vector3.zero; // ��ġ �ʱ�ȭ
        transform.localRotation = Quaternion.identity; // ȸ�� �ʱ�ȭ

        // ParentConstraint�� �ҽ��� handPosition���� ����
        List<ConstraintSource> sources = new List<ConstraintSource> { handSource_Reverse };
        parentConstraint.SetSources(sources);

        // ParentConstraint Ȱ��ȭ (�ʿ��� ���)
        parentConstraint.constraintActive = true;
    }

    // ���⸦ Į���� �ִ� �Լ�
    public void Sheathe()
    {
        // ������ �θ� Į�� ��ġ�� ����
        transform.SetParent(sheathedPosition);
        transform.localPosition = Vector3.zero; // ��ġ �ʱ�ȭ
        transform.localRotation = Quaternion.identity; // ȸ�� �ʱ�ȭ

        // ParentConstraint�� �ҽ��� sheathedPosition���� ����
        List<ConstraintSource> sources = new List<ConstraintSource> { sheathedSource };
        parentConstraint.SetSources(sources);

        // ParentConstraint Ȱ��ȭ (�ʿ��� ���)
        parentConstraint.constraintActive = true;
    }
}
