using UnityEngine;
using UnityEngine.UI;

public class BuffIconUI : MonoBehaviour
{
    [Header("Skill Settings")]
    public string skillName; // Tên skill khớp với lúc gọi Invoke trong Player.cs (ví dụ: "DoubleJump")
    
    [Header("UI Components")]
    public Image iconImage;
    
    [Header("Visual Settings")]
    public Color lockedColor = new Color(1f, 1f, 1f, 0.3f); // Làm mờ khi chưa có skill
    public Color unlockedColor = new Color(1f, 1f, 1f, 1f); // Sáng lên khi đã có skill

    private bool isUnlocked = false;

    private void Awake()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
        
        // Trạng thái mặc định ban đầu là bị khóa
        SetUnlocked(false);
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        if (iconImage != null)
        {
            iconImage.color = isUnlocked ? unlockedColor : lockedColor;
        }
    }
}
