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
        //Debug.Log("플레이어가 대기 상태에 진입");
    }

    public void UpdateState(PlayerController player)
    {
        
    }

    public void ExitState(PlayerController player)
    {
        //Debug.Log("Idle 상태에서 벗어남");
    }
}
public class MoveState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        //Debug.Log("플레이어가 이동 상태에 진입");
    }

    public void UpdateState(PlayerController player)
    {
        
    }

    public void ExitState(PlayerController player)
    {
        //Debug.Log("Move 상태에서 벗어남");
    }
}

public class ActionState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        //Debug.Log("플레이어가 액션 상태에 진입");
    }

    public void UpdateState(PlayerController player)
    {

    }

    public void ExitState(PlayerController player)
    {
        //Debug.Log("Action 상태에서 벗어남");
    }
}

public class UIState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        //Debug.Log("플레이어가 UI 상태에 진입");
    }

    public void UpdateState(PlayerController player)
    {

    }

    public void ExitState(PlayerController player)
    {
        //Debug.Log("UI 상태에서 벗어남");
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
        // 기본 상태를 Idle 상태로 설정
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
