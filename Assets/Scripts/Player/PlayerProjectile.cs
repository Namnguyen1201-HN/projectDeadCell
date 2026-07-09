using UnityEngine;

/// <summary>
/// Projectile fired by the player's bow. It damages enemies only.
/// </summary>
public class PlayerProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private StanceManager ownerStance;
    private LayerMask enemyLayer;

    [Header("Settings")]
    public float lifetime = 4f;
    public GameObject hitEffectPrefab;

    public void Initialize(Vector2 dir, float projectileSpeed, int projectileDamage, LayerMask targetLayer, StanceManager stanceManager)
    {
        direction = dir.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        enemyLayer = targetLayer;
        ownerStance = stanceManager;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            return;

        bool isEnemyLayer = (enemyLayer.value & (1 << other.gameObject.layer)) != 0;
        bool belongsToEnemy = other.GetComponentInParent<EnemyBase>() != null || other.GetComponentInParent<Health>() != null;
        if (!isEnemyLayer && !belongsToEnemy) return;

        Health targetHealth = other.GetComponentInParent<Health>();
        if (targetHealth != null)
        {
            targetHealth.changeHealth(-damage);
            ownerStance?.TryApplyBurn(targetHealth);
        }

        EnemyBase enemyBase = other.GetComponentInParent<EnemyBase>();
        ownerStance?.TryApplySlow(enemyBase);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
