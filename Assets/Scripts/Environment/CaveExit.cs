using UnityEngine;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(Collider2D))]
public class CaveExit : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Scene chuyển tiếp khi nhấn nút Chuyển màn")]
    public string nextSceneName = "SampleScene 1";

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
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        Player player = collision.GetComponentInParent<Player>();
        if (player != null || collision.CompareTag("Player"))
        {
            if (requireBossDefeated)
            {
                
                BossController[] bosses = FindObjectsOfType<BossController>();
                foreach (BossController boss in bosses)
                {
                    
                    if (boss.health != null && boss.health.health > 0)
                    {
                        Debug.Log($"Bạn chưa đánh bại Boss {boss.gameObject.name}! Cửa hang chưa mở.");
                        return; 
                    }
                }
            }

            Debug.Log("Đã tiêu diệt Boss, tiến hành kết thúc màn...");
            
            if (useEndGamePanel && UIManager.Instance != null)
            {
                UIManager.Instance.ShowLevelEndPanel(endMessage, ResolveSceneAlias(nextSceneName), menuSceneName);
            }
            else
            {
                
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.LoadScene(ResolveSceneAlias(nextSceneName));
                }
                else
                {
                   
                    SceneManager.LoadScene(ResolveSceneAlias(nextSceneName));
                }
            }
        }
        else
        {
            Debug.Log($"CaveExit: Object {collision.name} chạm vào nhưng không phải Player.");
        }
    }
    private string ResolveSceneAlias(string sceneName)
    {
        return sceneName == "SummerLevel" ? "SampleScene 1" : sceneName;
    }
}
