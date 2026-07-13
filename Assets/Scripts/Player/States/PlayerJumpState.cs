using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.jumpsRemaining--;
        Jump();
    }

    public override void Update()
    {
        base.Update();

        // up/top/down depends on yVelocity (Blend Tree)
        if (player.anim != null) player.anim.SetFloat("yVelocity", player.rb.velocity.y);

        if (Input.GetButtonDown("Jump") && player.hasDoubleJump && player.jumpsRemaining > 0)
        {
            player.jumpsRemaining--;
            Jump();
        }

        //landing
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

        float finalSpeed = player.moveSpeed;
        if (player.stanceManager != null)
            finalSpeed *= player.stanceManager.GetSpeedMultiplier();

        player.rb.velocity = new Vector2(player.horizontalInput * finalSpeed, player.rb.velocity.y);
    }

    private void Jump()
    {
        float finalJumpForce = player.jumpForce;
        if (player.stanceManager != null)
            finalJumpForce *= player.stanceManager.GetJumpMultiplier();

        player.rb.velocity = new Vector2(player.rb.velocity.x, finalJumpForce);
    }
}
