using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : PlayerState
{
    public PlayerAttackState(Player player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        if (player.anim != null) player.anim.SetBool("isAttacking", true);
        player.rb.velocity = new Vector2(0,player.rb.velocity.y);

        //Reset attack
        player.isAttackFinished = false;

        //Updata time next attack
        if (player.combat != null)
        {
            player.combat.nextAttackTime = Time.time + player.combat.GetCurrentAttackCooldown();
        }
    }

    public override void Update()
    {
        base.Update();

        //Wait animation event
        if (player.isAttackFinished)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        if (player.anim != null) player.anim.SetBool("isAttacking", false);
    }

}
