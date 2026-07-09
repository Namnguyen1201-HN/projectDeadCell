using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Tên Scene chính của game bạn muốn load khi bấm Bắt Đầu")]
    public string mainGameSceneName = "SpringLeverScenes";

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject storyPanel;
    public GameObject tutorialPanel;
    public GameObject levelSelectPanel; // Panel chọn màn chơi

    private void Start()
    {
        if (storyPanel != null) storyPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
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

    // 2. Mở bảng chọn màn chơi
    public void ShowLevelSelect()
    {
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void HideLevelSelect()
    {
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // 3. Load màn Mùa Thu
    public void LoadAutumnRuins()
    {
        SceneManager.LoadScene("AutumnRuins");
    }

    // 3. Load màn Mùa Xuân
    public void LoadSpringScenes()
    {
        SceneManager.LoadScene("SpringLeverScenes");
    }

    // Bắt đầu chơi mới (load Spring mặc định)
    public void StartNewGame()
    {
        Debug.Log("Bắt đầu game mới! Đang load scene: " + mainGameSceneName);
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
