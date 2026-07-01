using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    [Header("Player References")]
    public Player player;
    public Health playerHealth;

    [Header("Avatar Settings")]
    public Image avatarImage;

    [Header("Health Bar Settings")]
    public Slider healthSlider;

    [Header("Boss Health Settings")]
    public GameObject bossHealthPanel;
    public Slider bossHealthSlider;
    private Health currentBossHealth;

    [Header("Buffs/Skills Settings")]
    // Danh sách tất cả các UI của Icon buff mà mình kéo vào từ Inspector
    public List<BuffIconUI> buffIcons;

    private void OnEnable()
    {
        // Đăng ký sự kiện (lắng nghe)
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged += UpdateHealthBar;
        }

        if (player != null)
        {
            player.onSkillUnlocked += HandleSkillUnlocked;
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký sự kiện để tránh lỗi bộ nhớ khi UIManager bị xóa
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged -= UpdateHealthBar;
        }

        if (player != null)
        {
            player.onSkillUnlocked -= HandleSkillUnlocked;
        }
    }

    private void Start()
    {
        // Khởi tạo thanh máu lần đầu
        if (playerHealth != null)
        {
            UpdateHealthBar(playerHealth.health, playerHealth.maxHealth);
        }

        // Ẩn thanh máu Boss khi mới bắt đầu game
        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(false);
        }
    }

    // Hàm này được gọi tự động khi sự kiện onHealthChanged phát ra
    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    // Hàm này được gọi tự động khi sự kiện onSkillUnlocked phát ra
    private void HandleSkillUnlocked(string skillName)
    {
        // Tìm Icon tương ứng với tên skill và bật nó lên
        foreach (var buffIcon in buffIcons)
        {
            if (buffIcon.skillName == skillName)
            {
                buffIcon.SetUnlocked(true);
            }
        }
    }

    // -- BOSS HEALTH UI --
    public void ShowBossHealth(Health bossHealth)
    {
        if (bossHealth == null) return;
        
        currentBossHealth = bossHealth;
        currentBossHealth.onHealthChanged += UpdateBossHealthBar;
        
        if (bossHealthPanel != null) bossHealthPanel.SetActive(true);
        UpdateBossHealthBar(currentBossHealth.health, currentBossHealth.maxHealth);
    }

    public void HideBossHealth()
    {
        if (currentBossHealth != null)
        {
            currentBossHealth.onHealthChanged -= UpdateBossHealthBar;
            currentBossHealth = null;
        }
        
        if (bossHealthPanel != null) bossHealthPanel.SetActive(false);
    }

    private void UpdateBossHealthBar(int currentHealth, int maxHealth)
    {
        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = maxHealth;
            bossHealthSlider.value = currentHealth;
        }
    }
}
