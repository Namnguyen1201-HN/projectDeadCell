using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.rb.velocity = new Vector2(player.rb.velocity.x, player.jumpForce);
    }

    public override void Update()
    {
        base.Update();

        // Cập nhật giá trị yVelocity cho Animator
        player.anim.SetFloat("yVelocity", player.rb.velocity.y);

        // Chạm đất
        if (player.rb.velocity.y <= 0.1f && player.isGrounded)
        {
            if (Mathf.Abs(player.horizontalInput) > 0.1f)
                stateMachine.ChangeState(player.MoveState);
            else
                stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        // Cho phép di chuyển trên không
        player.rb.velocity = new Vector2(player.horizontalInput * player.moveSpeed, player.rb.velocity.y);
    }
}
