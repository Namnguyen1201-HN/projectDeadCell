using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("Targeting")]
    public string targetTag = "Player";
    private Transform player;
    private Health playerHealth;

    [Header("Boss Stats")]
    public float aggroRange = 15f;
    public float attackRange = 2f;
    public float moveSpeed = 2.5f;
    public int attackDamage = 20;
    public float attackCooldown = 2f;

    [Header("Effects")]
    public SpriteRenderer spriteRenderer;
    public Color hurtColor = Color.red;
    public float hurtDuration = 0.2f;
    private Color originalColor;
    
    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 1.5f;

    private float nextAttackTime = 0f;
    private bool isDead = false;

    private Rigidbody2D rb;
    public Animator anim;
    public Health health;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
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

    private void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            // Dừng di chuyển để tấn công
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isMoving", false);

            if (Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
        else if (distanceToPlayer <= aggroRange)
        {
            // Di chuyển về phía Player
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
            
            // Xoay mặt Boss
            if (direction.x > 0)
                transform.rotation = Quaternion.Euler(0, 180, 0);
            else if (direction.x < 0)
                transform.rotation = Quaternion.Euler(0, 0, 0);
            
            if (anim != null) anim.SetBool("isMoving", true);
        }
        else
        {
            // Ở trạng thái Idle
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isMoving", false);
        }
    }

    private void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;
        if (anim != null) anim.SetTrigger("isAttacking");
        
        if (playerHealth != null) 
        {
            playerHealth.changeHealth(-attackDamage);
        }
        else
        {
            Debug.LogWarning("Boss đang cố tấn công nhưng không tìm thấy component Health trên Player!");
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag(targetTag))
        {
            // Nếu chạm vào người chơi mà đã hồi xong đòn đánh thì đánh luôn
            if (Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
    }
    
    private void OnTriggerStay2D(Collider2D collider)
    {
        if (isDead) return;

        if (collider.CompareTag(targetTag))
        {
            if (Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
    }

    private void HandleDamaged()
    {
        if (isDead) return;

        if (spriteRenderer != null)
        {
            StopCoroutine("FlashHurtColor");
            StartCoroutine("FlashHurtColor");
        }
        
        if (anim != null) anim.SetTrigger("isHurt");
    }

    private IEnumerator FlashHurtColor()
    {
        spriteRenderer.color = hurtColor;
        yield return new WaitForSeconds(hurtDuration);
        spriteRenderer.color = originalColor;
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        rb.velocity = Vector2.zero;

        // Ẩn UI thanh máu Boss
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideBossHealth();
        }

        // Hồi đầy máu cho người chơi
        if (playerHealth != null)
        {
            playerHealth.changeHealth(playerHealth.maxHealth); // Sẽ tự động bị giới hạn ở maxHealth trong script Health
            Debug.Log("Player được hồi đầy máu sau khi đánh bại Boss!");
        }

        // Tăng sát thương cho người chơi
        if (player != null)
        {
            Player p = player.GetComponent<Player>();
            if (p != null)
            {
                p.UnlockSkill("Streng");
            }
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (anim != null) anim.SetTrigger("isDead");

        // Đợi animation chạy xong (bạn có thể chỉnh số giây này cho khớp với độ dài animation Die của bạn)
        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }
}
