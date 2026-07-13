using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDefeatLevelEnd : MonoBehaviour
{
    public string nextSceneName = "MainMenu";
    public float delay = 1f;
    public GameObject victoryPanel;
    public float victoryDisplayDuration = 3.5f;
    public bool freezeGameDuringVictory = true;

    private Health health;
    private bool ending;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health == null) health = GetComponent<Health>();
        if (health != null) health.onDeath += HandleBossDeath;
    }

    private void OnDisable()
    {
        if (health != null) health.onDeath -= HandleBossDeath;
    }

    private void HandleBossDeath()
    {
        if (ending) return;
        ending = true;
        if (UIManager.Instance != null)
            UIManager.Instance.StartCoroutine(EndLevelRoutine());
        else
            StartCoroutine(EndLevelRoutine());
    }

    private IEnumerator EndLevelRoutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));

        if (UIManager.Instance != null)
            UIManager.Instance.HideBossHealth();
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        if (freezeGameDuringVictory)
            Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, victoryDisplayDuration));
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene(nextSceneName);
            else
                SceneManager.LoadScene(nextSceneName);
        }
    }
}
