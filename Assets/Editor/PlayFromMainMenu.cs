using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Forces Play mode to start from MainMenu scene instead of the currently open scene.
/// Menu: Tools > Play From Main Menu  (or Ctrl+Alt+P)
/// </summary>
public static class PlayFromMainMenu
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private static string _previousScenePath;

    [MenuItem("Tools/Play From Main Menu %&p")]
    public static void PlayFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        string currentPath = EditorSceneManager.GetActiveScene().path;

        if (!string.IsNullOrEmpty(currentPath) && currentPath != MainMenuPath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                _previousScenePath = currentPath;
                EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
            }
        }
        else
        {
            if (currentPath != MainMenuPath)
                EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

            EditorApplication.isPlaying = true;
        }
    }

    [InitializeOnLoadMethod]
    private static void RestorePreviousSceneOnStop()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && !string.IsNullOrEmpty(_previousScenePath))
            {
                EditorApplication.delayCall += () =>
                {
                    if (!EditorSceneManager.GetActiveScene().path.Equals(_previousScenePath))
                        EditorSceneManager.OpenScene(_previousScenePath, OpenSceneMode.Single);
                    _previousScenePath = null;
                };
            }
        };
    }
}
