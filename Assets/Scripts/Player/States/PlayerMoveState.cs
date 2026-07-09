using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
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
            stateMachine.ChangeState(player.JumpState);
            return;
        }

        if (Input.GetAxisRaw("Vertical") < -0.1f)
        {
            stateMachine.ChangeState(player.RollState);
            return;
        }

        if (Mathf.Abs(player.horizontalInput) <= 0.1f)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        float finalSpeed = player.moveSpeed;
        if (player.stanceManager != null)
            finalSpeed *= player.stanceManager.GetSpeedMultiplier();

        player.rb.velocity = new Vector2(player.horizontalInput * finalSpeed, player.rb.velocity.y);
    }
}
