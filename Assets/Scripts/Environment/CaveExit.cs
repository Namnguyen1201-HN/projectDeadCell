using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đính kèm script này vào GameObject cửa hang.
/// Đảm bảo GameObject này có 1 component BoxCollider2D (hoặc tương tự) đã tick chọn ô "Is Trigger".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CaveExit : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Scene chuyển tiếp khi nhấn nút Chuyển màn")]
    public string nextSceneName = "SummerLevel";

    [Tooltip("Tên Scene của Menu chính để thoát ra")]
    public string menuSceneName = "MainMenu";
    
    [Tooltip("Bắt buộc phải tiêu diệt Boss mới được qua cửa?")]
    public bool requireBossDefeated = true;

    [Header("UI Settings")]
    [Tooltip("Sử dụng Panel kết thúc thay vì chuyển Scene luôn")]
    public bool useEndGamePanel = true;

    [Tooltip("Thông báo hiện ra khi kết thúc màn")]
    public string endMessage = "Lõi cân bằng mùa xuân đã được thu hồi.";

    private void Awake()
    {
        // Đảm bảo collider được set là trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đối tượng chạm vào cửa hang có phải là người chơi không
        Player player = collision.GetComponentInParent<Player>();
        if (player != null || collision.CompareTag("Player"))
        {
            if (requireBossDefeated)
            {
                // Tìm TẤT CẢ Boss trên bản đồ
                BossController[] bosses = FindObjectsOfType<BossController>();
                foreach (BossController boss in bosses)
                {
                    // Chỉ cần 1 Boss còn sống (máu > 0), thì chặn lại
                    if (boss.health != null && boss.health.health > 0)
                    {
                        Debug.Log($"Bạn chưa đánh bại Boss {boss.gameObject.name}! Cửa hang chưa mở.");
                        return; // Chặn không cho thoát
                    }
                }
            }

            Debug.Log("Đã tiêu diệt Boss, tiến hành kết thúc màn...");
            
            if (useEndGamePanel && UIManager.Instance != null)
            {
                UIManager.Instance.ShowLevelEndPanel(endMessage, nextSceneName, menuSceneName);
            }
            else
            {
                // Chuyển Scene thông qua SceneTransitionManager để có hiệu ứng Fade đen mượt mà
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.LoadScene(nextSceneName);
                }
                else
                {
                    // Dự phòng nếu không có SceneTransitionManager
                    SceneManager.LoadScene(nextSceneName);
                }
            }
        }
        else
        {
            Debug.Log($"CaveExit: Object {collision.name} chạm vào nhưng không phải Player.");
        }
    }
}
