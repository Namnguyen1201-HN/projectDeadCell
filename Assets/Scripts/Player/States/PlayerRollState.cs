using UnityEngine;

public class PlayerRollState : PlayerState
{
    private float stateTimer;
    private bool isStopping;

    public PlayerRollState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = player.rollDuration;
        isStopping = false;

        // Thu nhỏ collider
        if (player.coll != null)
        {
            player.coll.size = player.rollColliderSize;
            player.coll.offset = player.rollColliderOffset;
        }
    }

    public override void Update()
    {
        base.Update();

        stateTimer -= Time.deltaTime;

        if (isStopping)
        {
            if (stateTimer <= 0f)
            {
                
                if (player.isCeilingHit)
                {
                    return; 
                }

                if (Mathf.Abs(player.horizontalInput) > 0.1f)
                    stateMachine.ChangeState(player.MoveState);
                else
                    stateMachine.ChangeState(player.IdleState);
            }
        }
        else
        {
            if (stateTimer <= 0f)
            {
                isStopping = true;
                stateTimer = player.rollStopDuration;
                
                if (player.coll != null && !player.isCeilingHit)
                {
                    player.coll.size = player.normalColliderSize;
                    player.coll.offset = player.normalColliderOffset;
                }
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isStopping)
        {
            player.rb.velocity = new Vector2(0, player.rb.velocity.y);
        }
        else
        {
            float rollDirection = player.isFacingRight ? 1f : -1f;
            player.rb.velocity = new Vector2(rollDirection * player.rollSpeed, player.rb.velocity.y);
        }
    }

    public override void Exit()
    {
        base.Exit();
        
        if (player.coll != null)
        {
            player.coll.size = player.normalColliderSize;
            player.coll.offset = player.normalColliderOffset;
        }
    }
}
