using System.Collections;
using UnityEngine;

/// <summary>
/// Quái tàng hình (Invisible/Stealth Enemy).
/// - Trong tầm aggro: tàng hình hoàn toàn (alpha = 0), di chuyển đến gần player.
/// - Khi tấn công: hiện ra nháy sáng rồi đánh rồi tàng hình lại.
/// - Khi bị đánh: hiện nguyên hình trong 1 giây rồi tàng hình lại.
/// - Ngoài tầm aggro: tàng hình, đứng yên.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class StealthEnemy : MonoBehaviour
{
    [Header("Targeting")]
    public string targetTag = "Player";

    [Header("Stats")]
    public float aggroRange = 7f;
    public float attackRange = 1.2f;
    public float moveSpeed = 2.5f;
    public int attackDamage = 12;
    public float attackCooldown = 2f;

    [Header("Stealth Settings")]
    [Tooltip("Alpha khi đang tàng hình (0 = vô hình hoàn toàn)")]
    public float stealthAlpha = 0.05f;
    [Tooltip("Khoảng cách bắt đầu hiện dần ra")]
    public float revealRange = 3.5f;
    [Tooltip("Alpha khi hiện nguyên hình")]
    public float visibleAlpha = 1f;
    [Tooltip("Thời gian hiện ra trước khi tấn công (giây)")]
    public float revealBeforeAttack = 0.4f;
    [Tooltip("Thời gian hiện ra khi bị đánh (giây)")]
    public float revealOnHitDuration = 1f;

    [Header("Effects")]
    public SpriteRenderer spriteRenderer;
    public Color attackFlashColor = new Color(1f, 0.2f, 0.2f);

    private Transform _player;
    private Health _playerHealth;
    private Rigidbody2D _rb;
    private Health _health;

    private float _nextAttackTime;
    private bool _isAttacking;
    private bool _isRevealed;
    private Color _baseColor;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _health = GetComponent<Health>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            _baseColor = spriteRenderer.color;
            SetAlpha(stealthAlpha); // bắt đầu tàng hình
        }

        // Lắng nghe sự kiện bị đánh và chết từ Health
        if (_health != null)
        {
            _health.onDamaged += OnHurt;
            _health.onDeath += OnDeath;
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.onDamaged -= OnHurt;
            _health.onDeath -= OnDeath;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(targetTag);
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (_health != null && _health.health <= 0) return;
        if (_player == null || _isAttacking) return;

        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist <= attackRange && Time.time >= _nextAttackTime)
        {
            StartCoroutine(AttackRoutine());
        }
        else if (dist <= aggroRange)
        {
            // Di chuyển về phía player, tính toán độ tàng hình
            if (!_isRevealed)
            {
                if (dist <= revealRange)
                {
                    // Lại gần thì hiện rõ dần từ stealthAlpha đến visibleAlpha
                    float t = Mathf.InverseLerp(revealRange, attackRange, dist);
                    float currentAlpha = Mathf.Lerp(stealthAlpha, visibleAlpha, t);
                    SetAlpha(currentAlpha);
                }
                else
                {
                    SetAlpha(stealthAlpha);
                }
            }

            Vector2 dir = (_player.position - transform.position).normalized;
            _rb.velocity = new Vector2(dir.x * moveSpeed, _rb.velocity.y);

            // Flip sprite
            if (spriteRenderer != null)
                spriteRenderer.flipX = dir.x < 0;
        }
        else
        {
            // Ngoài tầm: đứng yên và tàng hình
            _rb.velocity = new Vector2(0, _rb.velocity.y);
            if (!_isRevealed)
                SetAlpha(stealthAlpha);
        }
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _nextAttackTime = Time.time + attackCooldown;

        // Hiện ra nháy sáng báo hiệu tấn công
        _isRevealed = true;
        SetAlpha(visibleAlpha);
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = attackFlashColor;
            yield return new WaitForSeconds(revealBeforeAttack * 0.5f);
            spriteRenderer.color = original;
            yield return new WaitForSeconds(revealBeforeAttack * 0.5f);
        }
        else
        {
            yield return new WaitForSeconds(revealBeforeAttack);
        }

        // Thực hiện đánh
        if (_playerHealth != null)
        {
            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist <= attackRange * 1.5f) // cho phép khoảng hở nhỏ
            {
                _playerHealth.changeHealth(-attackDamage);
            }
        }

        // Tàng hình lại sau khi đánh
        yield return new WaitForSeconds(0.3f);
        _isRevealed = false;
        SetAlpha(stealthAlpha);
        _isAttacking = false;
    }

    private void OnHurt()
    {
        // Khi bị đánh: hiện ra một lúc
        StopCoroutine(nameof(RevealOnHitRoutine));
        StartCoroutine(nameof(RevealOnHitRoutine));
    }

    private void OnDeath()
    {
        // Dừng mọi hành động và tự hủy khi máu = 0
        StopAllCoroutines();
        Destroy(gameObject);
    }

    private IEnumerator RevealOnHitRoutine()
    {
        _isRevealed = true;
        SetAlpha(visibleAlpha);
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer != null)
            spriteRenderer.color = _baseColor;
        yield return new WaitForSeconds(revealOnHitDuration - 0.1f);
        _isRevealed = false;
        SetAlpha(stealthAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null) return;
        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Màu vàng cho revealRange
        Gizmos.DrawWireSphere(transform.position, revealRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
