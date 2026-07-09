using UnityEngine;

/// <summary>
/// Đạn dùng chung cho kẻ địch (Arrow, Leaf, Crow, v.v.).
/// Di chuyển theo hướng và gây sát thương khi chạm Player.
/// </summary>
public class Projectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    
    [Header("Settings")]
    public float lifetime = 5f;
    public bool destroyOnGroundHit = true;
    public GameObject hitEffectPrefab;

    public void Initialize(Vector2 dir, float spd, int dmg)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;

        // Xoay sprite theo hướng bay
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
        if (other.CompareTag("Player"))
        {
            Health h = other.GetComponent<Health>();
            if (h != null)
            {
                // Kiểm tra khiên (parry) hoặc block
                WeaponSystem ws = other.GetComponent<WeaponSystem>();
                if (ws != null && ws.IsShieldActive())
                {
                    Debug.Log("[Projectile] Blocked by Shield!");
                    // Có thể thêm hiệu ứng block ở đây
                }
                else
                {
                    h.changeHealth(-damage);
                }
            }
            HitAndDestroy();
        }
        else if (destroyOnGroundHit && other.CompareTag("Ground"))
        {
            HitAndDestroy();
        }
    }

    private void HitAndDestroy()
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
