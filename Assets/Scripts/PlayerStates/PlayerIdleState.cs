using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        // Khi đứng im, ép vận tốc X về 0 để không bị trôi
        player.rb.velocity = new Vector2(0, player.rb.velocity.y);
    }

    public override void Update()
    {
        base.Update();

        // Kiểm tra bấm chuột trái và thời gian hồi chiêu
        if (Input.GetMouseButtonDown(0) && player.CanAttack())
        {
            stateMachine.ChangeState(player.AttackState);
            return;
        }

        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            stateMachine.ChangeState(player.JumpState);
            return;
        }

        if (Mathf.Abs(player.horizontalInput) > 0.1f)
        {
            stateMachine.ChangeState(player.MoveState);
            return;
        }
    }
}
