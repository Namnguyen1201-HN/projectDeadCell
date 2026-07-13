using UnityEngine;
using UnityEngine.UI;

public class BuffIconUI : MonoBehaviour
{
    [Header("Skill Settings")]
    public string skillName;
    [Header("UI Components")]
    public Image iconImage;
    
    [Header("Visual Settings")]
    public Color lockedColor = new Color(1f, 1f, 1f, 0.3f);
    public Color unlockedColor = new Color(1f, 1f, 1f, 1f); 

    private bool isUnlocked = false;

    private void Awake()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
        
        //Trạng thái mặc định
        SetUnlocked(false);
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        
        gameObject.SetActive(isUnlocked);

        if (iconImage != null)
        {
            iconImage.color = isUnlocked ? unlockedColor : lockedColor;
        }
    }
}
