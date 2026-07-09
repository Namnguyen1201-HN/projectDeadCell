using System;
using UnityEngine;

/// <summary>
/// Quản lý máu cho Player và Enemy.
/// FIXED v2:
/// - Start(): nếu maxHealth = 0 thì log warning thay vì set health = 0 ngầm.
/// - Reset isDead khi gọi changeHealth heal về > 0 (Revive scenario).
/// - Guard: không fire onDamaged nếu lượng heal (amount > 0).
/// </summary>
public class Health : MonoBehaviour
{
    public event Action onDamaged;
    public event Action onDeath;
    public event Action<int, int> onHealthChanged;

    public int health;
    public int maxHealth;

    private bool isDead = false;

    private void Start()
    {
        if (maxHealth <= 0)
        {
            Debug.LogWarning($"[Health] '{gameObject.name}' có maxHealth = {maxHealth}. " +
                             "Gán maxHealth > 0 trong Inspector hoặc qua script.");
            maxHealth = Mathf.Max(maxHealth, 1); // Tránh chia cho 0 trong UI
        }

        if (health <= 0)
            health = maxHealth;

        onHealthChanged?.Invoke(health, maxHealth);
    }

    public void changeHealth(int amount)
    {
        if (isDead && amount < 0) return; // Chặn trừ máu sau khi chết

        // Kiểm tra Invincibility (chỉ với damage)
        if (amount < 0)
        {
            BuffReceiver buffReceiver = GetComponent<BuffReceiver>();
            if (buffReceiver != null && buffReceiver.isInvincible)
            {
                Debug.Log($"[Health] [{gameObject.name}] Damage blocked by Invincibility!");
                return;
            }
        }

        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);

        if (health <= 0 && !isDead)
        {
            isDead = true;
            onDeath?.Invoke();
        }
        else if (health > 0 && isDead)
        {
            // Revive: reset trạng thái chết khi được heal về > 0
            isDead = false;
        }
        else if (amount < 0 && !isDead)
        {
            onDamaged?.Invoke();
        }

        onHealthChanged?.Invoke(health, maxHealth);
    }
}
