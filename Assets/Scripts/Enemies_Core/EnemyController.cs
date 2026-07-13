using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Targeting")]
    public string targetTag = "Player";
    private Transform player;
    private Health playerHealth;

    [Header("Stats")]
    public float aggroRange = 8f;
    public float attackRange = 1.5f;
    public float moveSpeed = 3f;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

    [Header("Stun Settings")]
    public float stunDuration = 0.8f;

    [Header("Effects")]
    public SpriteRenderer spriteRenderer;
    public Color hurtColor = Color.red;
    public float hurtDuration = 0.2f;
    private Color originalColor;

    [Header("DeathFX")]
    [SerializeField] private GameObject[] deathParts;
    [SerializeField] private float spawnForce;
    [SerializeField] private float torque;
    [SerializeField] private float lifeTime;

    [Header("Drop Items")]
    public GameObject keyPrefab;
    public GameObject healItemPrefab;
    [Range(0f, 1f)]
    public float healItemDropChance;

    private float nextAttackTime;
    private bool isStunned = false;

    private Rigidbody2D rb;
    public Animator anim;
    public Health health;

    private Collider2D coll;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();

        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (health == null) health = GetComponent<Health>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(targetTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<Health>();
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.onDamaged += HandleDamaged;
            health.onDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onDamaged -= HandleDamaged;
            health.onDeath -= HandleDeath;
        }
    }

    private bool CanMoveForward(float dirX)
    {
        if (coll == null) return true;

        Vector2 origin = coll.bounds.center;
        float xExtents = coll.bounds.extents.x;
        float yExtents = coll.bounds.extents.y;
        float checkDirection = dirX > 0 ? 1f : -1f;

        // Front bottom edge
        Vector2 frontEdge = new Vector2(origin.x + xExtents * checkDirection, origin.y - yExtents);
        LayerMask groundMask = LayerMask.GetMask("Ground");

        // 1. Check Ledge (is there ground below the front edge?)
        RaycastHit2D groundHit = Physics2D.Raycast(frontEdge, Vector2.down, 1f, groundMask);
        if (groundHit.collider == null)
            return false; // Ledge ahead!

        // 2. Check Wall (is there a wall in front?)
        Vector2 centerFront = new Vector2(origin.x + xExtents * checkDirection, origin.y);
        RaycastHit2D wallHit = Physics2D.Raycast(centerFront, new Vector2(checkDirection, 0), 0.2f, groundMask);
        if (wallHit.collider != null && !wallHit.collider.isTrigger)
            return false; // Wall ahead!

        return true;
    }

    private void Update()
    {
        if (health != null && health.health <= 0) return;
        if (isStunned) return;
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);

            if (Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
        else if (distanceToPlayer <= aggroRange)
        {
            Vector2 direction = (player.position - transform.position).normalized;

            // Check if we can move in that direction without falling or hitting a wall
            if (CanMoveForward(direction.x))
            {
                rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

                if (direction.x > 0)
                    transform.rotation = Quaternion.Euler(0, 180, 0);
                else if (direction.x < 0)
                    transform.rotation = Quaternion.Euler(0, 0, 0);

                if (anim != null) anim.SetBool("isRunning", true);
            }
            else
            {
                // Stop at edge/wall, face the player but don't move forward
                rb.velocity = new Vector2(0, rb.velocity.y);
                if (direction.x > 0)
                    transform.rotation = Quaternion.Euler(0, 180, 0);
                else if (direction.x < 0)
                    transform.rotation = Quaternion.Euler(0, 0, 0);

                if (anim != null) anim.SetBool("isRunning", false);
            }
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
        }
    }

    private void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;
        if (anim != null) anim.SetTrigger("isAttacking");
        if (playerHealth != null) playerHealth.changeHealth(-attackDamage);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (health != null && health.health <= 0) return;
        if (isStunned) return;

        if (collision.gameObject.CompareTag(targetTag) && Time.time >= nextAttackTime)
        {
            Attack();
        }
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (health != null && health.health <= 0) return;
        if (isStunned) return;

        if (collider.CompareTag(targetTag) && Time.time >= nextAttackTime)
        {
            Attack();
        }
    }

    private void HandleDamaged()
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

    private IEnumerator FlashHurtColor()
    {
        spriteRenderer.color = hurtColor;
        yield return new WaitForSeconds(hurtDuration);
        spriteRenderer.color = originalColor;
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
    }

    private void HandleDeath()
    {
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (anim != null) anim.SetTrigger("isDead");

        yield return new WaitForSeconds(0.6f);

        if (keyPrefab != null) {
            Instantiate(keyPrefab, transform.position, Quaternion.identity);
        }

        if (healItemPrefab != null && Random.value <= healItemDropChance) {
            Instantiate(healItemPrefab, transform.position, Quaternion.identity);
        }

        foreach (GameObject prefab in deathParts) {
            Quaternion rotation = Quaternion.Euler(0,0,Random.Range(0.5f,1)).normalized;
            GameObject part = Instantiate(prefab,transform.position,rotation);

            Rigidbody2D partRb = part.GetComponent<Rigidbody2D>();
            if (partRb != null) {
                Vector2 randomDirection = new Vector2(Random.Range(-1f,1f), Random.Range(0f,1f)).normalized;
                partRb.velocity = randomDirection * spawnForce;
                partRb.AddTorque(Random.Range(-torque,torque), ForceMode2D.Impulse);
            }

            Destroy(part, lifeTime);
        }
        Destroy(gameObject);
    }
}
