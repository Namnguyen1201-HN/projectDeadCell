using UnityEngine;

/// <summary>
/// Type 1: Slime/Goblin (Cận chiến cơ bản).
/// Hành vi: Thấy người chơi là lao vào, đánh cận chiến.
/// </summary>
public class GoblinEnemy : EnemyBase
{
    protected override void EnemyBehavior()
    {
        float dist = DistanceToPlayer();
        float currentSpeed = moveSpeed * currentSpeedMultiplier;

        if (dist <= attackRange)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
            
            if (Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
        else if (dist <= aggroRange)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(dir.x * currentSpeed, rb.velocity.y);
            FacePlayer();
            
            if (anim != null) anim.SetBool("isRunning", true);
        }
        else
        {
            // Đứng im nếu ngoài tầm
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
        }
    }
}
