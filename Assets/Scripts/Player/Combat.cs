using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combat : MonoBehaviour
{
    public Player player;

    [Header("Attack Settings")]
    public Transform attackPoint;
    public float attackRadius;
    public LayerMask enemyLayer;
    public int attackDamage;
    public float attackCooldown;
    public float nextAttackTime;

    public Animator hitFX;

    public void AttackAnimationFinished()
    {
        player.AttackAnimationFinished();
    }

    public void onAttack()
    {
        if (attackPoint == null)
        {
            Debug.LogError("LỖI: Bạn chưa kéo GameObject Attack Point vào ô tương ứng ở Player script!");
            return;
        }

        Debug.Log("2. Bắt đầu vung kiếm! Quét vùng đánh với bán kính " + attackRadius);

        // 1. Quét vòng tròn tại attackPoint để tìm các vật thể thuộc enemyLayer
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);

        if (hitEnemies.Length == 0)
        {
            Debug.Log("-> Không chém trúng vật thể nào. Nguyên nhân có thể do: Xa quá, sai Layer, hoặc Hình nộm chưa có Collider.");
        }

        // 2. Duyệt qua danh sách kẻ địch trúng đòn
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("-> Đã chạm vào Collider của: " + enemy.name);

            hitFX.Play("HitFX");

            // Tìm component Health và gọi hàm trừ máu
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.changeHealth(-attackDamage);
                Debug.Log("--> Đã gọi hàm trừ máu trên: " + enemy.name);
            }
            else
            {
                Debug.LogWarning("--> Kẻ địch " + enemy.name + " trúng đòn nhưng không tìm thấy script Health!");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
