using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 currentRespawnPosition;
    private Health playerHealth;
    private Rigidbody2D rb;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Khởi tạo điểm hồi sinh ban đầu là vị trí lúc mới vào game
        currentRespawnPosition = transform.position;
    }

    // Hàm gọi khi người chơi đi qua một điểm Checkpoint an toàn mới
    public void UpdateRespawnPosition(Vector3 newPosition)
    {
        currentRespawnPosition = newPosition;
    }

    // Hàm gọi khi chạm vào chông hoặc bẫy
    public void TakeHazardDamageAndRespawn(int damageAmount)
    {
        // 1. Trừ máu (sử dụng hàm changeHealth trong Health.cs của bạn)
        if (playerHealth != null)
        {
            playerHealth.changeHealth(-damageAmount);
        }

        // 2. Nếu nhân vật vẫn còn sống thì đưa về vị trí checkpoint
        if (playerHealth != null && playerHealth.health > 0)
        {
            transform.position = currentRespawnPosition;
            
            // Reset vận tốc để nhân vật không giữ gia tốc rơi/di chuyển cũ
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }
    }
}
