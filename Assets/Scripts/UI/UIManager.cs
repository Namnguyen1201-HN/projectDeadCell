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

        // Ép tìm lại Player đang active trên Scene để tránh dính reference cũ
        Player[] allPlayers = FindObjectsOfType<Player>();
        foreach (Player p in allPlayers)
        {
            if (p.gameObject.activeInHierarchy)
            {
                // Ưu tiên player có tag là "Player" hoặc là Archer
                if (p.CompareTag("Player") || p is Archer)
                {
                    player = p;
                    break;
                }
                
                // Fallback nếu không có tag
                if (player == null)
                {
                    player = p;
                }
            }
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
        }
    }
    [Header("Player References")]
    public Player player;
    public Health playerHealth;

    [Header("Avatar Settings")]
    public Image avatarImage;

    [Header("Death Menu Settings")]
    public GameObject deathMenuPanel;

    [Header("Pause Menu Settings")]
    public GameObject pauseMenuPanel;
    private bool isPaused = false;

    [Header("Health Bar Settings")]
    public Slider healthSlider;

    [Header("Boss Health Settings")]
    public GameObject bossHealthPanel;
    public Slider bossHealthSlider;
    private Health currentBossHealth;

    [Header("Buffs/Skills Settings")]
    // Danh sách tất cả các UI của Icon buff mà mình kéo vào từ Inspector
    public List<BuffIconUI> buffIcons;

    [Header("Items Settings")]
    public TMPro.TextMeshProUGUI keyText;
    private int lastKeyCount = -1;

    [Header("Level End Settings")]
    public GameObject levelEndPanel;
    public TMPro.TextMeshProUGUI levelEndMessageText;
    private string levelEndNextScene;
    private string levelEndMenuScene;

    private void OnEnable()
    {
        // Đăng ký sự kiện (lắng nghe)
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged += UpdateHealthBar;
            playerHealth.onDeath += ShowDeathMenu;
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
            playerHealth.onDeath -= ShowDeathMenu;
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

        // Ẩn Menu Chết khi mới bắt đầu game
        if (deathMenuPanel != null)
        {
            deathMenuPanel.SetActive(false);
        }

        // Ẩn Menu Tạm dừng khi mới bắt đầu game
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // Ẩn Panel Kết thúc màn khi mới bắt đầu game
        if (levelEndPanel != null)
        {
            levelEndPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Nhấn ESC để bật/tắt tạm dừng
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        // Cập nhật số lượng chìa khóa
        if (player != null && keyText != null)
        {
            if (player.keyCount != lastKeyCount)
            {
                lastKeyCount = player.keyCount;
                keyText.text = lastKeyCount.ToString();
            }
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

    // -- DEATH MENU UI --
    private void ShowDeathMenu()
    {
        if (deathMenuPanel != null)
        {
            deathMenuPanel.SetActive(true);
        }
    }

    public void OnRestartLevelClicked()
    {
        // Tải lại Scene hiện tại
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void OnReviveClicked()
    {
        if (deathMenuPanel != null)
        {
            deathMenuPanel.SetActive(false);
        }

        if (player != null)
        {
            PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                respawn.ReviveAtCheckpoint();
            }
        }
    }

    public void OnBackToMainMenuClicked()
    {
        // Tải scene MainMenu. Đảm bảo scene MainMenu đã được thêm vào Build Settings!
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // -- PAUSE MENU UI --
    public void TogglePause()
    {
        isPaused = !isPaused;

        // Dừng thời gian (0) hoặc chạy bình thường (1)
        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isPaused);
        }
    }

    public void OnResumeClicked()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }

    public void OnPauseReviveClicked()
    {
        // Trả lại thời gian trước khi thực hiện logic khác
        if (isPaused) TogglePause();

        if (player != null)
        {
            PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                respawn.ReviveAtCheckpoint();
            }
        }
    }

    public void OnPauseMainMenuClicked()
    {
        // Quan trọng: Trả lại thời gian bình thường trước khi chuyển Scene
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadLevelAutumnRuins()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("AutumnRuins");
    }

    public void LoadLevelSpringScenes()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("SpringLeverScenes");
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // -- LEVEL END UI --
    public void ShowLevelEndPanel(string message, string nextScene, string menuScene)
    {
        levelEndNextScene = ResolveSceneAlias(nextScene);
        levelEndMenuScene = menuScene;

        if (levelEndMessageText != null)
        {
            levelEndMessageText.text = message;
        }

        if (levelEndPanel != null)
        {
            levelEndPanel.SetActive(true);
        }

        // Tạm dừng game khi hiện bảng kết thúc
        Time.timeScale = 0f;
    }

    public void OnLevelEndNextClicked()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(levelEndNextScene))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene(levelEndNextScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(levelEndNextScene);
        }
    }

    private string ResolveSceneAlias(string sceneName)
    {
        return sceneName == "SummerLevel" ? "SampleScene 1" : sceneName;
    }

    public void OnLevelEndMenuClicked()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(levelEndMenuScene))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene(levelEndMenuScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(levelEndMenuScene);
        }
    }
}
