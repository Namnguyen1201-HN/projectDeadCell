using UnityEngine;

public class Combat : MonoBehaviour
{
    public Player player;

    [Header("Attack Settings")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public LayerMask enemyLayer;
    public int attackDamage = 10;
    public float attackCooldown = 0.6f;
    public float nextAttackTime;

    [Header("Sword Profile")]
    public float swordAttackRadius = 1f;
    public float swordCooldown = 0.6f;

    [Header("Bow Profile")]
    public float bowAttackRadius = 6f;
    public float bowCooldown = 1.1f;
    public GameObject arrowProjectilePrefab;
    public Transform bowFirePoint;
    public float arrowSpeed = 12f;

    public Animator hitFX;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<Player>();

        if (enemyLayer.value == 0)
            enemyLayer = LayerMask.GetMask("Enemy");
    }

    public void AttackAnimationFinished()
    {
        if (player != null)
            player.AttackAnimationFinished();
    }

    public float GetCurrentAttackCooldown()
    {
        return GetActiveWeaponType() == WeaponSystem.WeaponType.Bow ? bowCooldown : attackCooldown;
    }

    public void onAttack()
    {
        if (attackPoint == null)
        {
            Debug.LogError("[Combat] Missing Attack Point on Player.");
            return;
        }

        int finalDamage = GetFinalDamage();
        WeaponSystem.WeaponType weaponType = GetActiveWeaponType();

        if (weaponType == WeaponSystem.WeaponType.Bow && TryFireArrow(finalDamage))
            return;

        float radius = weaponType == WeaponSystem.WeaponType.Bow ? bowAttackRadius : swordAttackRadius;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, radius, enemyLayer);

        if (hitEnemies.Length == 0)
        {
            Debug.Log("[Combat] Attack missed. Check distance, enemy layer, or collider.");
            return;
        }

        foreach (Collider2D enemy in hitEnemies)
        {
            if (hitFX != null)
                hitFX.Play("HitFX");

            Health enemyHealth = enemy.GetComponentInParent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.changeHealth(-finalDamage);
                player?.stanceManager?.TryApplyBurn(enemyHealth);
            }
            else
            {
                Debug.LogWarning("[Combat] Hit enemy without Health: " + enemy.name);
            }

            EnemyBase enemyBase = enemy.GetComponentInParent<EnemyBase>();
            player?.stanceManager?.TryApplySlow(enemyBase);
        }
    }

    private bool TryFireArrow(int finalDamage)
    {
        FireArrowReal(finalDamage);
        return true;
    }

    private void FireArrowReal(int finalDamage)
    {
        Transform firePoint = bowFirePoint != null ? bowFirePoint : attackPoint;
        if (firePoint == null) return;

        if (arrowProjectilePrefab == null)
        {
            arrowProjectilePrefab = Resources.Load<GameObject>("ArrowPrefab");
        }

        GameObject arrow = arrowProjectilePrefab != null
            ? Instantiate(arrowProjectilePrefab, firePoint.position, Quaternion.identity)
            : CreateFallbackArrow(firePoint.position);
        
        arrow.SetActive(true);

        Arrow oldArrow = arrow.GetComponent<Arrow>();
        if (oldArrow != null) Destroy(oldArrow);

        Vector2 direction = player != null && !player.isFacingRight ? Vector2.left : Vector2.right;

        PlayerProjectile playerProjectile = arrow.GetComponent<PlayerProjectile>();
        if (playerProjectile == null)
        {
            playerProjectile = arrow.AddComponent<PlayerProjectile>();
        }

        playerProjectile.Initialize(direction, arrowSpeed, finalDamage, enemyLayer, player != null ? player.stanceManager : null);
    }

    private GameObject CreateFallbackArrow(Vector3 position)
    {
        GameObject arrow = new GameObject("Autumn_Fallback_Arrow");
        arrow.transform.position = position;

        SpriteRenderer sr = arrow.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRuntimePixelSprite();
        sr.color = new Color(0.95f, 0.74f, 0.28f);
        sr.sortingOrder = 30;
        arrow.transform.localScale = new Vector3(0.75f, 0.12f, 1f);

        BoxCollider2D collider = arrow.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        Rigidbody2D rb = arrow.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;

        arrow.AddComponent<PlayerProjectile>();
        return arrow;
    }

    private static Sprite CreateRuntimePixelSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private int GetFinalDamage()
    {
        int finalDamage = attackDamage;

        if (player != null && player.weaponSystem != null)
            finalDamage += player.weaponSystem.GetActiveDamage();

        if (player != null && player.buffReceiver != null && player.buffReceiver.hasDoubleDamage)
            finalDamage *= 2;

        return finalDamage;
    }

    private WeaponSystem.WeaponType GetActiveWeaponType()
    {
        if (player != null && player is Archer)
            return WeaponSystem.WeaponType.Bow;

        if (player == null || player.weaponSystem == null)
            return WeaponSystem.WeaponType.Sword;

        return player.weaponSystem.GetActiveWeapon().type;
    }

    private Vector2 GetFacingDirection()
    {
        if (player == null)
            return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

        return player.isFacingRight ? Vector2.right : Vector2.left;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, swordAttackRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint.position, bowAttackRadius);
    }
}
