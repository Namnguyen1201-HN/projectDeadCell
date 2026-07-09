using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Singleton quản lý chuyển scene với hiệu ứng fade.
/// Tạo 1 GameObject DontDestroyOnLoad chứa script này + Canvas + Image đen.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Panel")]
    public CanvasGroup fadePanel;   // Image đen toàn màn hình
    public float fadeDuration = 1f;

    private bool _isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (fadePanel != null)
        {
            fadePanel.alpha          = 0f;
            fadePanel.blocksRaycasts = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────

    public void LoadScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    public void LoadScene(int buildIndex)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(buildIndex));
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    // ─────────────────────────────────────────────────────────────────
    //  COROUTINES
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator TransitionRoutine(string sceneName)
    {
        _isTransitioning = true;
        yield return StartCoroutine(FadeTo(1f));

        SceneManager.LoadScene(sceneName);

        yield return null; // Đợi frame mới scene load xong
        yield return StartCoroutine(FadeTo(0f));
        _isTransitioning = false;
    }

    private IEnumerator TransitionRoutine(int buildIndex)
    {
        _isTransitioning = true;
        yield return StartCoroutine(FadeTo(1f));

        SceneManager.LoadScene(buildIndex);

        yield return null;
        yield return StartCoroutine(FadeTo(0f));
        _isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadePanel == null) yield break;

        fadePanel.blocksRaycasts = targetAlpha > 0f;

        float startAlpha = fadePanel.alpha;
        float elapsed    = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed      += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = targetAlpha;
    }
}
