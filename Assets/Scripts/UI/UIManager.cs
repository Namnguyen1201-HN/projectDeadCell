using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Player References")]
    public Player player;
    public Health playerHealth;

    [Header("Avatar Settings")]
    public Image avatarImage;

    [Header("Health Bar Settings")]
    public Slider healthSlider;

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
}
