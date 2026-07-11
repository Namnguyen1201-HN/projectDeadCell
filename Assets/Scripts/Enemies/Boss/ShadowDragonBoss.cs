using UnityEngine;
using System.Collections;

/// <summary>
/// Boss cuối Màn Thu - Shadow Demon Dragon.
/// Kế thừa BossBase: 2 phase, bắn bóng tối, triệu hồi đệ.
/// Sprite: Shadow_Demon_Dragon_Asset_Pack.
/// </summary>
public class ShadowDragonBoss : BossBase
{
    [Header("Shadow Dragon - Ranged Attack")]
    public GameObject shadowBallPrefab;
    public Transform firePoint;
    public float projectileSpeed = 8f;
    public int projectileDamage = 8;

    [Header("Shadow Dragon - Phase 2: Shadow Breath")]
    public int breathCount = 5;
    public float breathInterval = 0.2f;
    public float breathSpreadAngle = 30f; // Tổng góc quạt (mỗi bên 15 độ)

    [Header("Shadow Dragon - Summon")]
    public GameObject summonPrefab; // Mushroom_Enemy prefab
    public int maxSummons = 2;

    [Header("Audio")]
    public AudioClip attackSFX;
    public AudioClip hitSFX;
    public AudioClip deathSFX;
    public AudioClip idleSFX;

    private AudioSource audioSource;
    private float nextSpecialTime = 0f;
    private bool isUsingSpecial = false;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    // ─────────────────────────────────────────────────────────────────
    //  PHASE 1: Đuổi + Cận chiến + Shadow Ball đơn
    // ─────────────────────────────────────────────────────────────────
    protected override void Phase1Behavior()
    {
        if (isUsingSpecial) return;

        float dist = DistanceToPlayer();
        FacePlayer();

        // 1. Cận chiến
        if (dist <= attackRange)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
            if (Time.time >= nextAttackTime) Attack();
        }
        // 2. Bắn Shadow Ball (tầm xa)
        else if (dist <= aggroRange && Time.time >= nextSpecialTime)
        {
            StartCoroutine(ShadowBallRoutine());
            nextSpecialTime = Time.time + 3f;
        }
        // 3. Đuổi theo
        else if (dist <= aggroRange)
        {
            ChasePlayer();
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  PHASE 2: Nhanh hơn, Shadow Breath hình quạt + triệu hồi đệ
    // ─────────────────────────────────────────────────────────────────
    protected override void Phase2Behavior()
    {
        if (isUsingSpecial) return;

        float dist = DistanceToPlayer();
        FacePlayer();

        // 1. Cận chiến
        if (dist <= attackRange)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
            if (Time.time >= nextAttackTime) Attack();
        }
        // 2. Kỹ năng đặc biệt (random)
        else if (Time.time >= nextSpecialTime)
        {
            float roll = Random.value;
            if (roll > 0.5f)
                StartCoroutine(ShadowBreathRoutine()); // Bắn quạt
            else if (roll > 0.2f)
                StartCoroutine(ShadowBallRoutine());   // Bắn đơn
            else
                StartCoroutine(SummonMinionsRoutine()); // Triệu hồi

            nextSpecialTime = Time.time + 2f; // Phase 2 hồi chiêu nhanh hơn
        }
        // 3. Đuổi theo
        else if (dist <= aggroRange)
        {
            ChasePlayer();
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  CHUYỂN PHASE
    // ─────────────────────────────────────────────────────────────────
    protected override void OnPhaseTransition()
    {
        base.OnPhaseTransition();
        breathCount += 2; // Phase 2 bắn nhiều hơn
        Debug.Log($"[{bossName}] enters Phase 2 — Shadow Breath unlocked!");
    }

    // ─────────────────────────────────────────────────────────────────
    //  ATTACK OVERRIDE (thêm SFX)
    // ─────────────────────────────────────────────────────────────────
    protected override void Attack()
    {
        base.Attack();
        PlaySFX(attackSFX);
    }

    protected override void HandleDamaged()
    {
        base.HandleDamaged();
        PlaySFX(hitSFX);
    }

    // ─────────────────────────────────────────────────────────────────
    //  ROUTINES
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Bắn 1 quả Shadow Ball thẳng về phía Player.</summary>
    private IEnumerator ShadowBallRoutine()
    {
        isUsingSpecial = true;
        rb.velocity = new Vector2(0, rb.velocity.y);
        if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
        if (anim != null && HasAnimatorParameter("isAttacking")) anim.SetTrigger("isAttacking");
        PlaySFX(attackSFX);

        yield return new WaitForSeconds(0.3f); // Chờ animation wind-up

        FireProjectile(Vector2.zero); // Bắn thẳng

        yield return new WaitForSeconds(0.3f);
        isUsingSpecial = false;
    }

    /// <summary>Phase 2: Bắn nhiều quả theo hình quạt (Shadow Breath).</summary>
    private IEnumerator ShadowBreathRoutine()
    {
        isUsingSpecial = true;
        rb.velocity = new Vector2(0, rb.velocity.y);
        if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
        if (anim != null && HasAnimatorParameter("isAttacking")) anim.SetTrigger("isAttacking");
        PlaySFX(attackSFX);

        yield return new WaitForSeconds(0.3f);

        float halfSpread = breathSpreadAngle / 2f;
        float step = breathCount > 1 ? breathSpreadAngle / (breathCount - 1) : 0f;

        for (int i = 0; i < breathCount; i++)
        {
            float angleOffset = breathCount > 1 ? -halfSpread + step * i : 0f;
            Vector2 baseDir = (player.position - GetFirePosition()).normalized;
            Vector2 rotatedDir = RotateVector(baseDir, angleOffset);

            FireProjectile(rotatedDir, true);
            yield return new WaitForSeconds(breathInterval);
        }

        yield return new WaitForSeconds(0.3f);
        isUsingSpecial = false;
    }

    /// <summary>Triệu hồi quái nhỏ quanh Boss.</summary>
    private IEnumerator SummonMinionsRoutine()
    {
        isUsingSpecial = true;
        rb.velocity = new Vector2(0, rb.velocity.y);
        if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
        if (anim != null && HasAnimatorParameter("isAttacking")) anim.SetTrigger("isAttacking");

        int count = Random.Range(1, maxSummons + 1);
        for (int i = 0; i < count; i++)
        {
            if (summonPrefab != null)
            {
                Vector3 spawnPos = transform.position + new Vector3(Random.Range(-4f, 4f), 0f, 0f);
                Instantiate(summonPrefab, spawnPos, Quaternion.identity);
            }
        }

        Debug.Log($"[{bossName}] summoned {count} minions!");
        yield return new WaitForSeconds(0.5f);
        isUsingSpecial = false;
    }

    // ─────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────

    private void ChasePlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        float horizontalDirection = Mathf.Sign(dir.x);
        bool canMove = CanMoveForward(horizontalDirection);
        rb.velocity = new Vector2(canMove ? horizontalDirection * moveSpeed * currentSpeedMultiplier : 0f, rb.velocity.y);
        if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", canMove);
    }

    private Vector3 GetFirePosition()
    {
        return firePoint != null ? firePoint.position : transform.position;
    }

    /// <summary>
    /// Bắn 1 viên đạn. Nếu useDirectDirection=false, tự tính hướng đến player.
    /// </summary>
    private void FireProjectile(Vector2 direction, bool useDirectDirection = false)
    {
        if (shadowBallPrefab == null) return;

        Vector3 spawnPos = GetFirePosition();

        if (!useDirectDirection)
            direction = ((Vector2)(player.position - spawnPos)).normalized;

        GameObject ball = Instantiate(shadowBallPrefab, spawnPos, Quaternion.identity);
        Projectile proj = ball.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(direction, projectileSpeed, projectileDamage);
        }
        else
        {
            // Fallback: di chuyển bằng Rigidbody nếu không có script Projectile
            Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
            if (ballRb != null)
            {
                ballRb.velocity = direction * projectileSpeed;
            }
            Destroy(ball, 5f);
        }
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // ─────────────────────────────────────────────────────────────────
    //  DEATH OVERRIDE (thêm SFX)
    // ─────────────────────────────────────────────────────────────────
    protected override IEnumerator DeathRoutine()
    {
        PlaySFX(deathSFX);
        yield return StartCoroutine(base.DeathRoutine());
    }
}
