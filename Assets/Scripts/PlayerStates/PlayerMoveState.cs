using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update();

        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            stateMachine.ChangeState(player.JumpState);
            return;
        }

        // Chuyển sang lăn nếu bấm xuống
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
        player.rb.velocity = new Vector2(player.horizontalInput * player.moveSpeed, player.rb.velocity.y);
    }
}
