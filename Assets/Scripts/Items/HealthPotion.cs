using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    [Header("Potion Settings")]
    public int healAmount = 20; // Lượng máu hồi phục khi ăn

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem đối tượng ăn có phải là người chơi không
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Gọi hàm changeHealth với số dương để hồi máu
                playerHealth.changeHealth(healAmount);
                
                // Tiêu hủy vật phẩm sau khi nhặt
                Destroy(gameObject);
            }
        }
    }
}
