using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Tên Scene chính của game bạn muốn load khi bấm Bắt Đầu")]
    public string mainGameSceneName = "SampleScene"; // Đổi tên này thành tên Scene game thực tế của bạn

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject storyPanel;
    public GameObject tutorialPanel;

    private void Start()
    {
        // Đảm bảo các panel phụ bị ẩn khi mới mở menu, và hiện menu chính
        if (storyPanel != null) storyPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // 1. Cốt truyện
    public void ShowStory()
    {
        if (storyPanel != null) storyPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void HideStory()
    {
        if (storyPanel != null) storyPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // 2. Bắt đầu chơi mới
    public void StartNewGame()
    {
        // Có thể reset save data ở đây nếu bạn có hệ thống lưu trữ
        Debug.Log("Bắt đầu game mới! Đang load scene: " + mainGameSceneName);
        SceneManager.LoadScene(mainGameSceneName);
    }

    // 3. Tiếp tục chơi
    public void ContinueGame()
    {
        // TODO: Viết logic load save game ở đây (VD: Load vị trí, máu, skill, v.v...)
        // Hiện tại tạm thời gọi load scene giống StartNewGame
        Debug.Log("Tiếp tục chơi! Đang load save data...");
        SceneManager.LoadScene(mainGameSceneName);
    }

    // 4. Hướng dẫn chơi
    public void ShowTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void HideTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // 5. Thoát game
    public void QuitGame()
    {
        Debug.Log("Thoát game!");
        Application.Quit();
    }
}
