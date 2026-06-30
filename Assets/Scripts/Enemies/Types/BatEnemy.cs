using UnityEngine;

/// <summary>
/// Type 3: Bat (Bay lượn).
/// Hành vi: Bỏ qua địa hình (Gravity = 0), bay theo đường sin tiếp cận player.
/// </summary>
public class BatEnemy : EnemyBase
{
    [Header("Bat Settings")]
    public float sinAmplitude = 1.5f;    // Biên độ sóng sin
    public float sinFrequency = 2f;      // Tần số sóng sin
    
    private float sinTimer = 0f;
    private float baseY;

    protected override void Awake()
    {
        base.Awake();
        if (rb != null) 
        {
            rb.gravityScale = 0f; // Bỏ qua trọng lực
        }
    }

    protected override void Start()
    {
        base.Start();
        baseY = transform.position.y;
    }

    protected override void EnemyBehavior()
    {
        float dist = DistanceToPlayer();
        float currentSpeed = moveSpeed * currentSpeedMultiplier;

        if (dist <= aggroRange)
        {
            sinTimer += Time.deltaTime;
            
            // Bay về phía player theo trục X, dao động theo trục Y
            Vector2 dir = (player.position - transform.position).normalized;
            float sinOffset = Mathf.Sin(sinTimer * sinFrequency) * sinAmplitude;
            
            rb.velocity = new Vector2(dir.x * currentSpeed, sinOffset);
            FacePlayer();
            
            if (anim != null) anim.SetBool("isFlying", true);

            // Tấn công khi đủ gần
            if (dist <= attackRange && Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
        else
        {
            // Hover nhẹ tại chỗ khi không có mục tiêu
            sinTimer += Time.deltaTime;
            float hover = Mathf.Sin(sinTimer * sinFrequency * 0.5f) * (sinAmplitude * 0.3f);
            rb.velocity = new Vector2(0, hover);
            
            if (anim != null) anim.SetBool("isFlying", true);
        }
    }
}
