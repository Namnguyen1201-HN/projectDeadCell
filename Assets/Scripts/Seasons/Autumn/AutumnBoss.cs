using UnityEngine;
using System.Collections;

/// <summary>
/// Boss Mùa Thu - Guardian of Autumn Ruins.
/// Đánh gần, bắn lá (tầm xa), và triệu hồi bù nhìn/dơi ở phase 2.
/// </summary>
public class AutumnBoss : BossBase
{
    [Header("Autumn Boss - Phase 1")]
    public GameObject leafProjectilePrefab;
    public Transform[] firePoints;
    public float leafBarrageInterval = 0.3f;
    public int leafCount = 5;
    
    [Header("Autumn Boss - Phase 2")]
    public GameObject summonPrefab; 
    public int maxSummons = 3;
    
    private float nextSpecialTime = 0f;
    private bool isUsingSpecial = false;

    protected override void Phase1Behavior()
    {
        if (isUsingSpecial) return;

        float dist = DistanceToPlayer();
        FacePlayer();
        
        // 1. Tấn công cận chiến
        if (dist <= attackRange)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
            if (Time.time >= nextAttackTime) Attack();
        }
        // 2. Tấn công tầm xa (bắn lá)
        else if (dist <= aggroRange && Time.time >= nextSpecialTime)
        {
            StartCoroutine(LeafBarrageRoutine());
            nextSpecialTime = Time.time + 4f; // Hồi chiêu 4s
        }
        // 3. Đuổi theo player
        else if (dist <= aggroRange)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(dir.x * moveSpeed, rb.velocity.y);
            if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", true);
        }
    }

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
        // 2. Kỹ năng đặc biệt (random giữa bắn lá và triệu hồi)
        else if (Time.time >= nextSpecialTime)
        {
            if (Random.value > 0.4f)
                StartCoroutine(LeafBarrageRoutine());
            else
                StartCoroutine(SummonMinionsRoutine());
                
            nextSpecialTime = Time.time + 3f; // Phase 2 hồi chiêu nhanh hơn (3s)
        }
        // 3. Đuổi theo
        else if (dist <= aggroRange)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(dir.x * moveSpeed, rb.velocity.y);
            if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", true);
        }
    }

    protected override void OnPhaseTransition()
    {
        base.OnPhaseTransition();
        leafCount += 3; // Phase 2 bắn nhiều lá hơn
    }

    private IEnumerator LeafBarrageRoutine()
    {
        isUsingSpecial = true;
        rb.velocity = new Vector2(0, rb.velocity.y);
        if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
        if (anim != null && HasAnimatorParameter("isAttacking")) anim.SetTrigger("isAttacking");

        for (int i = 0; i < leafCount; i++)
        {
            if (firePoints != null && firePoints.Length > 0 && leafProjectilePrefab != null)
            {
                Transform fp = firePoints[Random.Range(0, firePoints.Length)];
                GameObject leaf = Instantiate(leafProjectilePrefab, fp.position, Quaternion.identity);
                
                Vector2 dir = (player.position - fp.position).normalized;
                // Thêm độ phân tán ngẫu nhiên
                dir += new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.1f, 0.1f));
                
                Projectile proj = leaf.GetComponent<Projectile>();
                if (proj != null) proj.Initialize(dir, 7f, attackDamage / 2); // Damage nhẹ hơn cận chiến
            }
            yield return new WaitForSeconds(leafBarrageInterval);
        }

        isUsingSpecial = false;
    }

    private IEnumerator SummonMinionsRoutine()
    {
        isUsingSpecial = true;
        rb.velocity = new Vector2(0, rb.velocity.y);
        if (anim != null && HasAnimatorParameter("isRunning")) anim.SetBool("isRunning", false);
        if (anim != null && HasAnimatorParameter("isAttacking")) anim.SetTrigger("isAttacking"); // Có thể thay bằng "isSummoning"

        int count = Random.Range(1, maxSummons + 1);
        for (int i = 0; i < count; i++)
        {
            if (summonPrefab != null)
            {
                Vector3 spawnPos = transform.position + new Vector3(Random.Range(-3f, 3f), Random.Range(1f, 3f), 0);
                Instantiate(summonPrefab, spawnPos, Quaternion.identity);
            }
        }

        yield return new WaitForSeconds(0.5f);
        isUsingSpecial = false;
    }
}
