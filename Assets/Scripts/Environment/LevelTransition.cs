using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// End-of-level transition.
/// If nextSceneName is empty, it can fall back to the team's current flow:
/// Spring -> SummerLevel -> WinterLevel -> AutumnRuins.
/// </summary>
public class LevelTransition : MonoBehaviour
{
    public string nextSceneName;
    public bool requireInput = true;
    public bool useTeamFlowFallback = true;

    private bool isPlayerInRange = false;

    private void Update()
    {
        if (requireInput && isPlayerInRange && Input.GetKeyDown(KeyCode.E))
            Transition();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = true;
        if (!requireInput)
            Transition();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = false;
    }

    private void Transition()
    {
        string targetScene = ResolveNextSceneName();
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[LevelTransition] No next scene configured.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogWarning("[LevelTransition] Scene is not in Build Settings: " + targetScene);
            return;
        }

        Time.timeScale = 1f;

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(targetScene);
        else
            SceneManager.LoadScene(targetScene);
    }

    private string ResolveNextSceneName()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            return nextSceneName;

        if (!useTeamFlowFallback)
            return string.Empty;

        string current = SceneManager.GetActiveScene().name.ToLowerInvariant();

        if (current.Contains("spring") || current.Contains("xuan") || current.Contains("xuân"))
            return "SummerLevel";

        if (current.Contains("summer") || current.Contains("ha") || current.Contains("hạ"))
            return "WinterLevel";

        if (current.Contains("winter") || current.Contains("dong") || current.Contains("đông"))
            return "AutumnRuins";

        if (current.Contains("autumn") || current.Contains("fall") || current.Contains("thu"))
            return "MainMenu";

        return string.Empty;
    }
}
