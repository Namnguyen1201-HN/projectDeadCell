using UnityEngine;

/// <summary>
/// Type 2: Archer (Đánh xa).
/// Hành vi: Đứng yên bắn khi player vào tầm. Nếu player quá gần sẽ lùi lại.
/// </summary>
public class ArcherEnemy : EnemyBase
{
    [Header("Archer Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 8f;
    public float retreatDistance = 3f;
    public float retreatSpeed = 2f;

    protected override void EnemyBehavior()
    {
        float dist = DistanceToPlayer();
        FacePlayer();
        float currentSpeed = moveSpeed * currentSpeedMultiplier;
        float currentRetreat = retreatSpeed * currentSpeedMultiplier;

        // 1. Lùi lại nếu player đến quá gần (Kite)
        if (dist < retreatDistance)
        {
            Vector2 awayDir = (transform.position - player.position).normalized;
            rb.velocity = new Vector2(awayDir.x * currentRetreat, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", true);
        }
        // 2. Đứng lại và bắn nếu trong tầm aggro
        else if (dist <= aggroRange)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
            
            if (Time.time >= nextAttackTime)
            {
                ShootProjectile();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        // 3. Đứng im ngoài tầm
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        
        if (anim != null) anim.SetTrigger("isAttacking");
        
        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();
        
        if (proj != null)
        {
            Vector2 dir = (player.position - firePoint.position).normalized;
            proj.Initialize(dir, projectileSpeed, attackDamage);
        }
    }

    // Override Attack thường vì Archer bắn đạn
    protected override void Attack()
    {
        // Do nothing here, handled by ShootProjectile
    }
}
