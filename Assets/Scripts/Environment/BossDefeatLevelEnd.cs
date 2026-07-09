using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDefeatLevelEnd : MonoBehaviour
{
    public string nextSceneName = "MainMenu";
    public float delay = 2.5f;

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
        StartCoroutine(EndLevelRoutine());
    }

    private IEnumerator EndLevelRoutine()
    {
        yield return new WaitForSeconds(delay);
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
