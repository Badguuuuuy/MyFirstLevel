using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public interface IPlayerState
{
    void EnterState(PlayerController player);
    void UpdateState(PlayerController player);
    void ExitState(PlayerController player);
}
public class IdleState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        //Debug.Log("�÷��̾ ��� ���¿� ����");
    }

    public void UpdateState(PlayerController player)
    {
        
    }

    public void ExitState(PlayerController player)
    {
        //Debug.Log("Idle ���¿��� ���");
    }
}
public class MoveState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        //Debug.Log("�÷��̾ �̵� ���¿� ����");
    }

    public void UpdateState(PlayerController player)
    {
        
    }

    public void ExitState(PlayerController player)
    {
        //Debug.Log("Move ���¿��� ���");
    }
}

public class ActionState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        //Debug.Log("�÷��̾ �׼� ���¿� ����");
    }

    public void UpdateState(PlayerController player)
    {

    }

    public void ExitState(PlayerController player)
    {
        //Debug.Log("Action ���¿��� ���");
    }
}

public class UIState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        //Debug.Log("�÷��̾ UI ���¿� ����");
    }

    public void UpdateState(PlayerController player)
    {

    }

    public void ExitState(PlayerController player)
    {
        //Debug.Log("UI ���¿��� ���");
    }
}

public class PlayerController : MonoBehaviour
{
    public IPlayerState CurrentState { get; private set; }

    public readonly IdleState idleState = new IdleState();
    public readonly MoveState moveState = new MoveState();
    public readonly ActionState actionState = new ActionState();
    public readonly UIState uiState = new UIState();
    //public readonly AttackState attackState = new AttackState();

    private void Start()
    {
        // �⺻ ���¸� Idle ���·� ����
        SwitchState(idleState);
    }

    private void Update()
    {
        CurrentState?.UpdateState(this);
    }

    public void SwitchState(IPlayerState newState)
    {
        if (newState == CurrentState) return;

        if (CurrentState != null)
        {
            CurrentState.ExitState(this);
        }

        CurrentState = newState;
        CurrentState.EnterState(this);
    }
}
