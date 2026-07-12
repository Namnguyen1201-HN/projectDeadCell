using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Tên Scene chính của game bạn muốn load khi bấm Bắt Đầu")]
    public string mainGameSceneName = "SpringLeverScenes";
    [SerializeField] private string springSceneName = "SpringLeverScenes";
    [SerializeField] private string summerSceneName = "SummerLevel";
    [SerializeField] private string autumnSceneName = "AutumnRuins";
    [SerializeField] private string winterSceneName = "WinterLevel";

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject storyPanel;
    public GameObject tutorialPanel;
    public GameObject levelSelectPanel; // Panel chọn màn chơi

    [Header("Generated Level Select")]
    [SerializeField] private bool rebuildLevelSelectPanel = true;
    [SerializeField] private string levelSelectSpriteResourcePath = "UI/SeasonLevelSelectMenu";

    private bool levelSelectBuilt;

    private void Start()
    {
        ResolveDuplicateLevelSelectPanels();
        if (rebuildLevelSelectPanel) BuildGeneratedLevelSelect();

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
        if (rebuildLevelSelectPanel && !levelSelectBuilt) BuildGeneratedLevelSelect();

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
        LoadSceneByName(autumnSceneName);
    }

    // 3. Load màn Mùa Xuân
    public void LoadSpringScenes()
    {
        LoadSceneByName(springSceneName);
    }

    public void LoadSummerScene()
    {
        LoadSceneByName(summerSceneName);
    }

    public void LoadWinterScene()
    {
        LoadSceneByName(winterSceneName);
    }

    // Bắt đầu chơi mới (load Spring mặc định)
    public void StartNewGame()
    {
        Debug.Log("Bắt đầu game mới! Đang load scene: " + mainGameSceneName);
        LoadSceneByName(mainGameSceneName);
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

    private void BuildGeneratedLevelSelect()
    {
        if (levelSelectPanel == null || levelSelectBuilt) return;

        for (int i = levelSelectPanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(levelSelectPanel.transform.GetChild(i).gameObject);
        }

        Image panelImage = levelSelectPanel.GetComponent<Image>();
        if (panelImage == null) panelImage = levelSelectPanel.AddComponent<Image>();

        Sprite generatedSprite = Resources.Load<Sprite>(levelSelectSpriteResourcePath);
        if (generatedSprite == null)
        {
            Debug.LogWarning($"[MainMenu] Không tìm thấy Sprite Resources/{levelSelectSpriteResourcePath}.png");
        }

        panelImage.sprite = generatedSprite;
        panelImage.color = Color.white;
        panelImage.raycastTarget = false;
        panelImage.preserveAspect = false;

        CreateInvisibleLevelButton("Hitbox_Spring", new Vector2(0.17f, 0.50f), new Vector2(0.41f, 0.69f), LoadSpringScenes);
        CreateInvisibleLevelButton("Hitbox_Summer", new Vector2(0.41f, 0.50f), new Vector2(0.65f, 0.69f), LoadSummerScene);
        CreateInvisibleLevelButton("Hitbox_Autumn", new Vector2(0.17f, 0.29f), new Vector2(0.41f, 0.48f), LoadAutumnRuins);
        CreateInvisibleLevelButton("Hitbox_Winter", new Vector2(0.41f, 0.29f), new Vector2(0.65f, 0.48f), LoadWinterScene);

        levelSelectBuilt = true;
    }

    private void ResolveDuplicateLevelSelectPanels()
    {
        RectTransform[] rectTransforms = FindObjectsOfType<RectTransform>(true);
        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform.name != "LevelSelectPanel") continue;

            GameObject panel = rectTransform.gameObject;
            if (levelSelectPanel == null)
            {
                levelSelectPanel = panel;
                continue;
            }

            if (panel != levelSelectPanel)
            {
                panel.SetActive(false);
            }
        }
    }

    private void CreateInvisibleLevelButton(string buttonName, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(levelSelectPanel.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
    }

    private void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[MainMenu] Scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[MainMenu] Scene '{sceneName}' chưa có trong Build Settings hoặc chưa tồn tại.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
