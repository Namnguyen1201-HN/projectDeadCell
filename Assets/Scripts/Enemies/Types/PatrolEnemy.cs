using UnityEngine;

/// <summary>
/// Type 5: Patrol (Lính gác).
/// Hành vi: Chỉ đi tuần tra qua lại giữa 2 điểm cố định.
/// Sẽ tấn công nếu player ngáng đường (trong tầm đánh), nhưng không đổi mục tiêu rượt đuổi xa.
/// </summary>
public class PatrolEnemy : EnemyBase
{
    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    private bool movingToB = true;

    protected override void EnemyBehavior()
    {
        float dist = DistanceToPlayer();
        float currentSpeed = moveSpeed * currentSpeedMultiplier;

        // Ưu tiên tấn công nếu player vào tầm đánh (cản đường)
        if (dist <= attackRange && Time.time >= nextAttackTime)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
            FacePlayer();
            Attack();
            return;
        }

        // Không đuổi theo, chỉ tiếp tục đi tuần
        Patrol(currentSpeed);
    }

    private void Patrol(float currentSpeed)
    {
        if (pointA == null || pointB == null)
        {
            // Fallback đứng im nếu chưa gán điểm
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
            return;
        }

        Transform target = movingToB ? pointB : pointA;
        Vector2 dir = (target.position - transform.position).normalized;
        
        rb.velocity = new Vector2(dir.x * currentSpeed, rb.velocity.y);
        
        // Xoay mặt theo hướng đi
        if (dir.x > 0) transform.rotation = Quaternion.Euler(0, 180, 0);
        else if (dir.x < 0) transform.rotation = Quaternion.Euler(0, 0, 0);

        if (anim != null) anim.SetBool("isRunning", true);

        // Kiểm tra đến nơi để đảo chiều
        if (Vector2.Distance(transform.position, target.position) < 0.5f)
        {
            movingToB = !movingToB;
        }
    }
}
