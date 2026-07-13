using System.Collections;
using UnityEngine;

/// <summary>
/// Autumn enemy that becomes easier to see as the player approaches.
/// It briefly reveals itself while attacking and after taking damage.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class StealthEnemy : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private string targetTag = "Player";

    [Header("Stats")]
    [SerializeField] private float aggroRange = 7f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private int attackDamage = 12;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Ground Navigation")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundCheckDistance = 0.16f;
    [SerializeField] private float wallCheckDistance = 0.18f;
    [SerializeField] private float ledgeCheckDistance = 0.55f;
    [SerializeField] private float minimumGravityScale = 3f;

    [Header("Stealth")]
    [SerializeField, Range(0.05f, 1f)] private float stealthAlpha = 0.28f;
    [SerializeField] private float revealRange = 6f;
    [SerializeField] private float fullRevealRange = 3f;
    [SerializeField] private float fadeSpeed = 14f;
    [SerializeField, Range(0.1f, 1f)] private float visibleAlpha = 1f;
    [SerializeField] private float revealBeforeAttack = 0.4f;
    [SerializeField] private float revealOnHitDuration = 1f;

    [Header("Effects")]
    public SpriteRenderer spriteRenderer;
    [SerializeField] private Color attackFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

    private Transform player;
    private Health playerHealth;
    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private Health health;
    private SpriteRenderer[] renderers;
    private Color[] baseColors;

    private Coroutine revealOnHitCoroutine;
    private float nextAttackTime;
    private float nextTargetSearchTime;
    private float forcedRevealUntil;
    private bool isAttacking;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        health = GetComponent<Health>();

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayers.value == 0 && groundLayer >= 0)
            groundLayers = 1 << groundLayer;

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = Mathf.Max(body.gravityScale, minimumGravityScale);
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0 && spriteRenderer != null)
            renderers = new[] { spriteRenderer };

        if (spriteRenderer == null && renderers.Length > 0)
            spriteRenderer = renderers[0];

        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i].color;

        SetAlphaImmediate(stealthAlpha);
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health != null)
        {
            health.onDamaged += OnHurt;
            health.onDeath += OnDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onDamaged -= OnHurt;
            health.onDeath -= OnDeath;
        }
    }

    private void Start()
    {
        TryAcquirePlayer();
    }

    private void Update()
    {
        if (health != null && health.health <= 0)
            return;

        if (player == null)
        {
            if (Time.unscaledTime >= nextTargetSearchTime)
            {
                nextTargetSearchTime = Time.unscaledTime + 0.5f;
                TryAcquirePlayer();
            }

            FadeToAlpha(stealthAlpha);
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        float targetAlpha = Time.time < forcedRevealUntil
            ? visibleAlpha
            : CalculateAlpha(distance);
        if (distance <= fullRevealRange || Time.time < forcedRevealUntil)
            SetAlphaImmediate(visibleAlpha);
        else
            FadeToAlpha(targetAlpha);

        if (isAttacking)
            return;

        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(AttackRoutine());
            return;
        }

        if (distance <= aggroRange)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            float horizontalDirection = Mathf.Sign(direction.x);
            bool canWalk = IsGrounded() && CanMoveToward(horizontalDirection);
            body.velocity = new Vector2(canWalk ? horizontalDirection * moveSpeed : 0f, body.velocity.y);

            if (spriteRenderer != null)
                spriteRenderer.flipX = direction.x < 0f;
        }
        else
        {
            body.velocity = new Vector2(0f, body.velocity.y);
        }
    }

    private bool IsGrounded()
    {
        if (bodyCollider == null || groundLayers.value == 0)
            return false;

        Bounds bounds = bodyCollider.bounds;
        Vector2 size = new Vector2(bounds.size.x * 0.82f, Mathf.Max(0.05f, bounds.size.y * 0.08f));
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + size.y * 0.5f);
        return Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayers).collider != null;
    }

    private bool CanMoveToward(float direction)
    {
        if (bodyCollider == null || groundLayers.value == 0 || Mathf.Approximately(direction, 0f))
            return false;

        Bounds bounds = bodyCollider.bounds;
        Vector2 wallOrigin = new Vector2(bounds.center.x, bounds.center.y);
        Vector2 wallSize = new Vector2(Mathf.Max(0.05f, bounds.size.x * 0.12f), bounds.size.y * 0.72f);
        bool blockedByWall = Physics2D.BoxCast(
            wallOrigin,
            wallSize,
            0f,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayers).collider != null;

        float frontInset = Mathf.Max(0.04f, bounds.size.x * 0.08f);
        Vector2 ledgeOrigin = new Vector2(
            direction > 0f ? bounds.max.x - frontInset : bounds.min.x + frontInset,
            bounds.min.y + groundCheckDistance);
        bool hasGroundAhead = Physics2D.Raycast(
            ledgeOrigin,
            Vector2.down,
            ledgeCheckDistance,
            groundLayers).collider != null;

        return !blockedByWall && hasGroundAhead;
    }

    private float CalculateAlpha(float distance)
    {
        if (distance >= revealRange)
            return stealthAlpha;

        if (distance <= fullRevealRange)
            return visibleAlpha;

        float proximity = 1f - Mathf.InverseLerp(fullRevealRange, revealRange, distance);
        proximity = Mathf.SmoothStep(0f, 1f, proximity);
        return Mathf.Lerp(stealthAlpha, visibleAlpha, proximity);
    }

    private void TryAcquirePlayer()
    {
        GameObject playerObject = null;

        if (!string.IsNullOrWhiteSpace(targetTag))
        {
            try
            {
                playerObject = GameObject.FindGameObjectWithTag(targetTag);
            }
            catch (UnityException)
            {
                // Fall back to the Player component when the tag is not configured.
            }
        }

        if (playerObject == null)
        {
            Player playerComponent = FindObjectOfType<Player>();
            if (playerComponent != null)
                playerObject = playerComponent.gameObject;
        }

        if (playerObject == null)
            return;

        player = playerObject.transform;
        playerHealth = playerObject.GetComponent<Health>();
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        forcedRevealUntil = Mathf.Max(forcedRevealUntil, Time.time + revealBeforeAttack + 0.35f);
        body.velocity = new Vector2(0f, body.velocity.y);

        SetTint(attackFlashColor);
        yield return new WaitForSeconds(revealBeforeAttack * 0.5f);
        RestoreBaseTint();
        yield return new WaitForSeconds(revealBeforeAttack * 0.5f);

        if (player != null && playerHealth != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= attackRange * 1.5f)
                playerHealth.changeHealth(-attackDamage);
        }

        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    private void OnHurt()
    {
        forcedRevealUntil = Mathf.Max(forcedRevealUntil, Time.time + revealOnHitDuration);

        if (revealOnHitCoroutine != null)
            StopCoroutine(revealOnHitCoroutine);
        revealOnHitCoroutine = StartCoroutine(RevealOnHitRoutine());
    }

    private IEnumerator RevealOnHitRoutine()
    {
        SetTint(Color.red);
        yield return new WaitForSeconds(0.1f);
        RestoreBaseTint();
        revealOnHitCoroutine = null;
    }

    private void OnDeath()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }

    private void FadeToAlpha(float targetAlpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Color color = renderers[i].color;
            color.a = Mathf.MoveTowards(color.a, targetAlpha, fadeSpeed * Time.deltaTime);
            renderers[i].color = color;
        }
    }

    private void SetAlphaImmediate(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Color color = renderers[i].color;
            color.a = alpha;
            renderers[i].color = color;
        }
    }

    private void SetTint(Color tint)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            float alpha = renderers[i].color.a;
            renderers[i].color = new Color(tint.r, tint.g, tint.b, alpha);
        }
    }

    private void RestoreBaseTint()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            float alpha = renderers[i].color.a;
            Color baseColor = baseColors[i];
            baseColor.a = alpha;
            renderers[i].color = baseColor;
        }
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0.1f, attackRange);
        fullRevealRange = Mathf.Max(attackRange, fullRevealRange);
        revealRange = Mathf.Max(fullRevealRange + 0.1f, revealRange);
        aggroRange = Mathf.Max(revealRange, aggroRange);
        fadeSpeed = Mathf.Max(0.1f, fadeSpeed);
        groundCheckDistance = Mathf.Max(0.05f, groundCheckDistance);
        wallCheckDistance = Mathf.Max(0.05f, wallCheckDistance);
        ledgeCheckDistance = Mathf.Max(groundCheckDistance + 0.05f, ledgeCheckDistance);
        minimumGravityScale = Mathf.Max(0.1f, minimumGravityScale);
        revealBeforeAttack = Mathf.Max(0.05f, revealBeforeAttack);
        revealOnHitDuration = Mathf.Max(0.1f, revealOnHitDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = new Color(1f, 0.65f, 0f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, revealRange);

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, fullRevealRange);

        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                new Vector3(bounds.min.x, bounds.min.y, 0f),
                new Vector3(bounds.max.x, bounds.min.y, 0f));
        }
    }
}
