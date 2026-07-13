using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    
    public string mainGameSceneName = "SpringLeverScenes";
    [SerializeField] private string springSceneName = "SpringLeverScenes";
    [SerializeField] private string summerSceneName = "SampleScene 1";
    [SerializeField] private string autumnSceneName = "AutumnRuins";
    [SerializeField] private string winterSceneName = "WinterLevel";

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject storyPanel;
    public GameObject tutorialPanel;
    public GameObject levelSelectPanel; 

    [Header("Generated Level Select")]
    [SerializeField] private bool rebuildLevelSelectPanel = true;
    [SerializeField] private string levelSelectSpriteResourcePath = "UI/SeasonLevelSelectMenu";

    private bool levelSelectBuilt;

    private void Start()
    {
        ResolveLevelSelectPanel();
        EnsureNewGameButtonOpensLevelSelect();
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

        if (levelSelectPanel != null)
        {
            levelSelectPanel.transform.SetAsLastSibling();
            levelSelectPanel.SetActive(true);
        }
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
        Debug.Log("[MainMenu] Autumn selected.");
        LoadSceneByName(autumnSceneName);
    }

    // 3. Load màn Mùa Xuân
    public void LoadSpringScenes()
    {
        LoadSceneByName(springSceneName);
    }

    public void LoadSummerScene()
    {
        Debug.Log("[MainMenu] Summer selected.");
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
        ShowLevelSelect();
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

        CreateInvisibleLevelButton("Hitbox_Spring", new Vector2(0.21f, 0.39f), new Vector2(0.49f, 0.62f), LoadSpringScenes);
        CreateInvisibleLevelButton("Hitbox_Summer", new Vector2(0.51f, 0.39f), new Vector2(0.79f, 0.62f), LoadSummerScene);
        CreateInvisibleLevelButton("Hitbox_Autumn", new Vector2(0.21f, 0.16f), new Vector2(0.49f, 0.39f), LoadAutumnRuins);

        levelSelectBuilt = true;
    }

    private void ResolveLevelSelectPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[MainMenu] No Canvas found for level select menu.");
            return;
        }

        Transform generatedPanel = canvas.transform.Find("GeneratedLevelSelectPanel");
        if (generatedPanel == null)
        {
            GameObject panelObject = new GameObject("GeneratedLevelSelectPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            generatedPanel = panelObject.transform;
            generatedPanel.SetParent(canvas.transform, false);

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        levelSelectPanel = generatedPanel.gameObject;
        levelSelectPanel.transform.SetAsLastSibling();

        RectTransform[] rectTransforms = canvas.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform.name != "LevelSelectPanel") continue;
            rectTransform.gameObject.SetActive(false);
        }

        levelSelectPanel.SetActive(false);
    }

    private void EnsureNewGameButtonOpensLevelSelect()
    {
        GameObject newGameButtonObject = GameObject.Find("Btn_NewGame");
        if (newGameButtonObject == null) return;

        Button newGameButton = newGameButtonObject.GetComponent<Button>();
        if (newGameButton == null) return;

        newGameButton.onClick.RemoveListener(StartNewGame);
        newGameButton.onClick.RemoveListener(ShowLevelSelect);
        newGameButton.onClick.AddListener(ShowLevelSelect);
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
        if (sceneName == "SummerLevel")
            sceneName = "SampleScene 1";

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

        Debug.Log("[MainMenu] Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
