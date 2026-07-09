using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public class AutumnArcherVisualEnforcer : MonoBehaviour
{
    [SerializeField] private Sprite archerSprite;
    [SerializeField] private RuntimeAnimatorController archerController;
    [SerializeField] private bool onlyInAutumnScene = true;
    [SerializeField] private Vector3 visualLocalPosition = new Vector3(0f, -0.75f, 0f);
    [SerializeField] private Vector3 visualLocalScale = new Vector3(1.35f, 1.35f, 1f);

    private void Awake()
    {
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            Apply();
    }
#endif

    public void Apply()
    {
        if (onlyInAutumnScene && !SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("autumn"))
            return;

        Transform spriteTransform = transform.Find("Sprite");
        if (spriteTransform == null)
            return;

        spriteTransform.localPosition = visualLocalPosition;
        spriteTransform.localScale = visualLocalScale;

        SpriteRenderer spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && archerSprite != null)
            spriteRenderer.sprite = archerSprite;

        Animator animator = spriteTransform.GetComponent<Animator>();
        if (animator != null && archerController != null)
            animator.runtimeAnimatorController = archerController;

        Player player = GetComponent<Player>();
        if (player != null && animator != null)
            player.anim = animator;
    }
}
