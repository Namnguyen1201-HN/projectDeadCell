using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEditor.Events;
using UnityEngine.Events;

/// <summary>
/// Tool tự động tạo LevelSelectPanel trong MainMenu scene
/// và gán hàm đúng vào nút New Game.
/// </summary>
public class LevelSelectSetup : EditorWindow
{
    [MenuItem("Tools/Setup Level Select Menu")]
    public static void SetupLevelSelectMenu()
    {
        // Yêu cầu mở scene MainMenu
        string currentScene = EditorSceneManager.GetActiveScene().name;
        if (currentScene != "MainMenu")
        {
            bool open = EditorUtility.DisplayDialog(
                "Setup Level Select",
                "Scene hiện tại không phải MainMenu.\nBạn có muốn mở MainMenu.unity không?",
                "Mở MainMenu", "Hủy");
            if (open)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            }
            else return;
        }

        // Tìm MainMenuController
        MainMenuController controller = Object.FindObjectOfType<MainMenuController>();
        if (controller == null)
        {
            Debug.LogError("[LevelSelectSetup] Không tìm thấy MainMenuController trong scene!");
            return;
        }

        // Tìm Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[LevelSelectSetup] Không tìm thấy Canvas trong scene!");
            return;
        }

        // ---- Tạo LevelSelectPanel ----
        GameObject existingPanel = GameObject.Find("LevelSelectPanel");
        if (existingPanel != null)
        {
            Debug.Log("[LevelSelectSetup] LevelSelectPanel đã tồn tại, xóa và tạo lại...");
            Object.DestroyImmediate(existingPanel);
        }

        GameObject levelPanel = new GameObject("LevelSelectPanel");
        levelPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = levelPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = levelPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.85f);
        levelPanel.SetActive(false); // ẩn mặc định

        // Tiêu đề
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(levelPanel.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 120);
        titleRect.sizeDelta = new Vector2(400, 60);

        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "CHỌN MÀN CHƠI";
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Nút Mùa Thu
        CreateLevelButton(levelPanel.transform, "Btn_AutumnRuins",
            "🍂  Mùa Thu (Autumn Ruins)", new Vector2(0, 30),
            controller, "LoadAutumnRuins", new Color(0.6f, 0.3f, 0.1f));

        // Nút Mùa Xuân
        CreateLevelButton(levelPanel.transform, "Btn_SpringScenes",
            "🌸  Mùa Xuân (Spring Scenes)", new Vector2(0, -50),
            controller, "LoadSpringScenes", new Color(0.1f, 0.6f, 0.3f));

        // Nút Quay lại
        CreateLevelButton(levelPanel.transform, "Btn_Back",
            "← Quay lại", new Vector2(0, -130),
            controller, "HideLevelSelect", new Color(0.3f, 0.3f, 0.3f));

        // Gán LevelSelectPanel vào controller
        SerializedObject so = new SerializedObject(controller);
        SerializedProperty prop = so.FindProperty("levelSelectPanel");
        if (prop != null)
        {
            prop.objectReferenceValue = levelPanel;
            so.ApplyModifiedProperties();
        }

        // ---- Sửa nút New Game để gọi ShowLevelSelect ----
        Button[] allButtons = Object.FindObjectsOfType<Button>();
        foreach (Button btn in allButtons)
        {
            string btnName = btn.gameObject.name.ToLower();
            if (btnName.Contains("newgame") || btnName.Contains("new game") ||
                btnName.Contains("start") || btnName.Contains("batdau") || btnName.Contains("bắt đầu"))
            {
                // Sử dụng UnityEventTools để lưu cố định sự kiện vào scene
                UnityEventTools.RemovePersistentListener(btn.onClick, 0); // Thử xóa cái đầu tiên
                UnityAction action = new UnityAction(controller.ShowLevelSelect);
                UnityEventTools.AddPersistentListener(btn.onClick, action);
                Debug.Log("[LevelSelectSetup] Đã cập nhật nút '" + btn.gameObject.name + "' → ShowLevelSelect() (Persistent)");
                break;
            }
        }

        // Lưu scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Debug.Log("[LevelSelectSetup] ✅ Hoàn tất! LevelSelectPanel đã được tạo trong MainMenu.");
        EditorUtility.DisplayDialog("Hoàn tất!",
            "LevelSelectPanel đã được tạo!\n\nKhi bấm New Game → hiện bảng chọn màn.\nBấm Mùa Thu hoặc Mùa Xuân để vào game.",
            "OK");
    }

    private static void CreateLevelButton(Transform parent, string name, string label,
        Vector2 position, MainMenuController controller, string methodName, Color bgColor)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(300, 55);

        Image bg = btnGO.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnGO.AddComponent<Button>();

        // Tạo text con
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        // Gán onClick Persistent
        if (methodName == "LoadAutumnRuins")
        {
            UnityAction action = new UnityAction(controller.LoadAutumnRuins);
            UnityEventTools.AddPersistentListener(btn.onClick, action);
        }
        else if (methodName == "LoadSpringScenes")
        {
            UnityAction action = new UnityAction(controller.LoadSpringScenes);
            UnityEventTools.AddPersistentListener(btn.onClick, action);
        }
        else if (methodName == "HideLevelSelect")
        {
            UnityAction action = new UnityAction(controller.HideLevelSelect);
            UnityEventTools.AddPersistentListener(btn.onClick, action);
        }
    }
}
