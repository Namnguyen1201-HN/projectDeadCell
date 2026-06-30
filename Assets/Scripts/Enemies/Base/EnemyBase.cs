using UnityEngine;
using System.Collections;

/// <summary>
/// Lớp cơ sở (Base Class) cho tất cả quái vật.
/// Quản lý chung Máu, Sát thương, Hiệu ứng (Hurt/Die), Drop Items.
/// Kế thừa và override EnemyBehavior() để định nghĩa AI.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Targeting")]
    public string targetTag = "Player";
    protected Transform player;
    protected Health playerHealth;

    [Header("Stats")]
    public float aggroRange   = 8f;
    public float attackRange  = 1.5f;
    public float moveSpeed    = 3f;
    public int   attackDamage = 10;
    public float attackCooldown = 1.5f;

    [Header("Stun & Slow")]
    public float stunDuration = 0.8f;
    protected bool  isStunned = false;
    protected float currentSpeedMultiplier = 1f;

    [Header("Effects")]
    public SpriteRenderer spriteRenderer;
    public Color hurtColor = Color.red;
    public float hurtDuration = 0.2f;
    protected Color originalColor;

    [Header("Death FX")]
    [SerializeField] protected GameObject[] deathParts;
    [SerializeField] protected float spawnForce = 5f;
    [SerializeField] protected float torque     = 5f;
    [SerializeField] protected float lifeTime   = 2f;

    [Header("Drop Items")]
    public GameObject keyPrefab;
    public GameObject healItemPrefab;
    [Range(0f, 1f)] public float healItemDropChance = 0.2f;
    public GameObject[] weaponDropPrefabs;
    [Range(0f, 1f)] public float weaponDropChance = 0.1f;
    public GameObject[] buffDropPrefabs;
    [Range(0f, 1f)] public float buffDropChance = 0.15f;

    protected float nextAttackTime = 0f;
    protected Rigidbody2D rb;
    public Animator anim;
    public Health health;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (health == null) health = GetComponent<Health>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    protected virtual void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag(targetTag);
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }
    }

    protected virtual void OnEnable()
    {
        if (health != null)
        {
            health.onDamaged += HandleDamaged;
            health.onDeath   += HandleDeath;
        }
    }

    protected virtual void OnDisable()
    {
        if (health != null)
        {
            health.onDamaged -= HandleDamaged;
            health.onDeath   -= HandleDeath;
        }
    }

    protected virtual void Update()
    {
        if (health != null && health.health <= 0) return;
        if (isStunned) return;
        if (player == null) return;

        EnemyBehavior();
    }

    /// <summary>Override hàm này ở class con để định nghĩa AI (Goblin, Archer, v.v.).</summary>
    protected abstract void EnemyBehavior();

    protected virtual void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;
        if (anim != null) anim.SetTrigger("isAttacking");
        
        // Cận chiến cơ bản: deal damage trực tiếp (Archer sẽ override bắn đạn)
        if (playerHealth != null)
        {
            playerHealth.changeHealth(-attackDamage);
            Debug.Log($"[{gameObject.name}] Attacked player for {attackDamage} dmg");
        }
    }

    protected void FacePlayer()
    {
        if (player == null) return;
        float dir = player.position.x - transform.position.x;
        if (dir > 0) transform.rotation = Quaternion.Euler(0, 180, 0);
        else if (dir < 0) transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    protected float DistanceToPlayer()
    {
        return player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
    }

    // ─────────────────────────────────────────────────────────────────
    //  DAMAGE, STUN, & EFFECTS
    // ─────────────────────────────────────────────────────────────────

    protected virtual void HandleDamaged()
    {
        StopCoroutine("StunRoutine");
        StartCoroutine("StunRoutine");
        
        if (spriteRenderer != null)
        {
            StopCoroutine("FlashHurtColor");
            StartCoroutine("FlashHurtColor");
        }
        if (anim != null) anim.SetTrigger("isDamaged");
    }

    protected IEnumerator FlashHurtColor()
    {
        spriteRenderer.color = hurtColor;
        yield return new WaitForSeconds(hurtDuration);
        spriteRenderer.color = originalColor;
    }

    protected IEnumerator StunRoutine()
    {
        isStunned = true;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
    }

    public void ApplySlow(float multiplier, float duration)
    {
        StartCoroutine(SlowRoutine(multiplier, duration));
    }

    private IEnumerator SlowRoutine(float multiplier, float duration)
    {
        currentSpeedMultiplier = multiplier;
        spriteRenderer.color = new Color(0.5f, 0.8f, 1f); // Màu xanh dương nhạt (bị làm chậm)
        yield return new WaitForSeconds(duration);
        currentSpeedMultiplier = 1f;
        spriteRenderer.color = originalColor;
    }

    // ─────────────────────────────────────────────────────────────────
    //  DEATH & DROPS
    // ─────────────────────────────────────────────────────────────────

    protected virtual void HandleDeath()
    {
        StartCoroutine(DeathRoutine());
    }

    protected virtual IEnumerator DeathRoutine()
    {
        if (anim != null) anim.SetTrigger("isDead");
        rb.velocity = Vector2.zero;
        rb.isKinematic = true; // Ngừng vật lý
        GetComponent<Collider2D>().enabled = false;

        yield return new WaitForSeconds(0.6f);

        DropItems();
        SpawnDeathParts();

        Destroy(gameObject);
    }

    private void DropItems()
    {
        // 1. Key (Luôn rớt nếu có gán)
        if (keyPrefab != null)
            Instantiate(keyPrefab, transform.position, Quaternion.identity);
            
        // 2. Heal Potion
        if (healItemPrefab != null && Random.value <= healItemDropChance)
            Instantiate(healItemPrefab, transform.position, Quaternion.identity);
            
        // 3. Weapon
        if (weaponDropPrefabs != null && weaponDropPrefabs.Length > 0 && Random.value <= weaponDropChance)
        {
            GameObject drop = weaponDropPrefabs[Random.Range(0, weaponDropPrefabs.Length)];
            Instantiate(drop, transform.position, Quaternion.identity);
        }

        // 4. Buffs (x2 Dmg / Invincible)
        if (buffDropPrefabs != null && buffDropPrefabs.Length > 0 && Random.value <= buffDropChance)
        {
            GameObject drop = buffDropPrefabs[Random.Range(0, buffDropPrefabs.Length)];
            Instantiate(drop, transform.position, Quaternion.identity);
        }
    }

    private void SpawnDeathParts()
    {
        if (deathParts == null || deathParts.Length == 0) return;
        
        foreach (GameObject prefab in deathParts)
        {
            Quaternion rot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            GameObject part = Instantiate(prefab, transform.position, rot);
            
            Rigidbody2D partRb = part.GetComponent<Rigidbody2D>();
            if (partRb != null)
            {
                Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(0f, 1f)).normalized;
                partRb.velocity = randomDir * spawnForce;
                partRb.AddTorque(Random.Range(-torque, torque), ForceMode2D.Impulse);
            }
            Destroy(part, lifeTime);
        }
    }
}
