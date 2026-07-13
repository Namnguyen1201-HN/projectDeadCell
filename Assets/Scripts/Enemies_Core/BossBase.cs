using UnityEngine;
using System.Collections;

/// <summary>
/// Hệ thống Boss cốt lõi dùng chung (Core AI), theo GDD.
/// Quản lý Phase chuyển đổi và phần thưởng (Stance Unlock).
/// </summary>
public abstract class BossBase : EnemyBase
{
    [Header("Boss Settings")]
    public string bossName = "Guardian";
    public int phase = 1;
    public int maxPhases = 2;
    [Range(0f, 1f)] public float phaseTransitionHP = 0.5f;

    [Header("Boss Rewards")]
    public StanceManager.StanceType stanceDrop = StanceManager.StanceType.None;

    [Header("Boss UI")]
    public GameObject bossHealthBarUI;

    private bool hasTransitioned = false;

    protected override void Start()
    {
        base.Start();
        if (bossHealthBarUI != null) bossHealthBarUI.SetActive(true);
    }

    protected override void Update()
    {
        if (health != null && health.health <= 0) return;
        if (isStunned) return;
        if (player == null) return;

        // Check chuyển phase
        if (!hasTransitioned && health != null)
        {
            float hpPercent = (float)health.health / health.maxHealth;
            if (hpPercent <= phaseTransitionHP)
            {
                hasTransitioned = true;
                phase = 2;
                OnPhaseTransition();
            }
        }

        if (phase == 1) Phase1Behavior();
        else            Phase2Behavior();
    }

    // Tắt EnemyBehavior gốc
    protected override void EnemyBehavior() { }

    protected abstract void Phase1Behavior();
    protected abstract void Phase2Behavior();

    protected virtual void OnPhaseTransition()
    {
        Debug.Log($"[{bossName}] Transitiioning to Phase 2!");
        if (anim != null && HasAnimatorParameter("phaseTransition")) anim.SetTrigger("phaseTransition");

        // Buff stat cơ bản ở phase 2
        moveSpeed *= 1.3f;
        attackDamage = Mathf.RoundToInt(attackDamage * 1.5f);
    }

    protected override IEnumerator DeathRoutine()
    {
        Debug.Log($"[{bossName}] Defeated!");
        if (anim != null && HasAnimatorParameter("isDead")) anim.SetTrigger("isDead");

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        GetComponent<Collider2D>().enabled = false;

        // Ẩn thanh máu UI
        if (bossHealthBarUI != null) bossHealthBarUI.SetActive(false);

        // Chờ chết animation
        yield return new WaitForSeconds(2f);

        // Mở khoá Stance cho player
        if (player != null && stanceDrop != StanceManager.StanceType.None)
        {
            StanceManager sm = player.GetComponent<StanceManager>();
            if (sm != null) sm.UnlockStance(stanceDrop);
        }

        // Rớt chìa khoá / vật phẩm (gọi từ Base)
        if (keyPrefab != null) Instantiate(keyPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
