using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("Spike Settings")]
    public int damage = 10; // Lượng máu bị mất khi chạm chông

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đối tượng chạm vào có phải là Player không
        if (collision.CompareTag("Player"))
        {
            // Lấy script PlayerRespawn và gọi hàm xử lý
            PlayerRespawn respawn = collision.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                respawn.TakeHazardDamageAndRespawn(damage);
            }
        }
        // Kiểm tra nếu quái vật rơi vào bẫy gai
        else if (collision.CompareTag("Enemy"))
        {
            Health enemyHealth = collision.GetComponent<Health>();
            if (enemyHealth != null)
            {
                // Trừ 1 lượng máu khổng lồ để quái chết ngay lập tức
                enemyHealth.changeHealth(-9999);
            }
        }
    }
}
