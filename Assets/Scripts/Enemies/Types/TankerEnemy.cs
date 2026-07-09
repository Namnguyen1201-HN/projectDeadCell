using UnityEngine;

/// <summary>
/// Type 4: Tanker (Giáp trâu).
/// Hành vi: Đi chậm, sát thương lớn. Khi lại gần sẽ lao nhanh vào (charge).
/// </summary>
public class TankerEnemy : EnemyBase
{
    [Header("Tanker Settings")]
    public float chargeSpeed = 6f;
    public float chargeDist  = 4f;

    protected override void EnemyBehavior()
    {
        float dist = DistanceToPlayer();
        float currentSpeed = moveSpeed * currentSpeedMultiplier;
        float currentCharge = chargeSpeed * currentSpeedMultiplier;

        if (dist <= attackRange)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
            
            if (Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
        else if (dist <= chargeDist)
        {
            // Lao vào nhanh khi đã ở cự ly gần
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(dir.x * currentCharge, rb.velocity.y);
            FacePlayer();
            
            if (anim != null) anim.SetBool("isRunning", true);
        }
        else if (dist <= aggroRange)
        {
            // Đi chậm rãi
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(dir.x * currentSpeed, rb.velocity.y);
            FacePlayer();
            
            if (anim != null) anim.SetBool("isRunning", true);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
        }
    }
}
