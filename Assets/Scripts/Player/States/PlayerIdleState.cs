using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        player.rb.velocity = new Vector2(0, player.rb.velocity.y);
        player.jumpsRemaining = player.hasDoubleJump ? 2 : 1;
    }

    public override void Update()
    {
        base.Update();

        if (Input.GetMouseButtonDown(0) && player.CanAttack())
        {
            stateMachine.ChangeState(player.AttackState);
            return;
        }

        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            if (Input.GetAxisRaw("Vertical") < -0.1f) return;

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
