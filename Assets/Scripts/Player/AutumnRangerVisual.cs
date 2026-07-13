using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public sealed class AutumnRangerVisual : MonoBehaviour
{
    private const string AtlasResourcePath = "AutumnRanger/AutumnRangerAtlas";
    private const int Columns = 8;
    private const int Rows = 7;
    private const float PixelsPerUnit = 90f;

    [SerializeField] private float idleFramesPerSecond = 6f;
    [SerializeField] private float actionFramesPerSecond = 10f;
    [SerializeField] private Vector3 visualLocalPosition = new Vector3(0.5f, -1.06f, 0f);
    [SerializeField] private Vector3 visualLocalScale = Vector3.one;

    private Player player;
    private Health health;
    private SpriteRenderer originalSpriteRenderer;
    private SpriteRenderer spriteRenderer;
    private Sprite[,] frames;
    private float hurtUntil;
    private bool isDead;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= AddToAutumnPlayer;
        SceneManager.sceneLoaded += AddToAutumnPlayer;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddToInitiallyLoadedScene()
    {
        AddToAutumnPlayer(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void AddToAutumnPlayer(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.ToLowerInvariant().Contains("autumn"))
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null && playerObject.GetComponent<AutumnRangerVisual>() == null)
            playerObject.AddComponent<AutumnRangerVisual>();
    }

    private void Awake()
    {
        player = GetComponent<Player>();
        health = GetComponent<Health>();

        Transform visual = transform.Find("Sprite");
        if (visual == null)
        {
            Debug.LogError("[AutumnRanger] Player is missing the Sprite child.", this);
            enabled = false;
            return;
        }

        originalSpriteRenderer = visual.GetComponent<SpriteRenderer>();
        spriteRenderer = CreateDedicatedRenderer(originalSpriteRenderer);
        frames = BuildFrames();

        if (spriteRenderer == null || frames == null)
        {
            Debug.LogError("[AutumnRanger] Visual setup failed.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health != null)
        {
            health.onDamaged += ShowHurt;
            health.onDeath += ShowDeath;
            health.onHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onDamaged -= ShowHurt;
            health.onDeath -= ShowDeath;
            health.onHealthChanged -= HandleHealthChanged;
        }
    }

    private void LateUpdate()
    {
        if (frames == null || spriteRenderer == null || player == null || player.StateMachine == null)
            return;

        int row = GetAnimationRow();
        float framesPerSecond = row == 0 ? idleFramesPerSecond : actionFramesPerSecond;
        int frame = Mathf.FloorToInt(Time.time * framesPerSecond) % Columns;

        if (isDead)
            frame = Mathf.Min(Columns - 1, Mathf.FloorToInt((Time.time - hurtUntil) * actionFramesPerSecond));

        spriteRenderer.sprite = frames[row, Mathf.Clamp(frame, 0, Columns - 1)];

        // PlayerEffects still targets the original renderer, so mirror its tint here.
        if (originalSpriteRenderer != null)
            spriteRenderer.color = originalSpriteRenderer.color;
    }

    private int GetAnimationRow()
    {
        if (isDead)
            return 6;

        if (Time.time < hurtUntil)
            return 5;

        PlayerState current = player.StateMachine.CurrentState;
        if (current == player.AttackState)
            return 4;
        if (current == player.RollState)
            return 3;
        if (current == player.JumpState)
            return 2;
        if (current == player.MoveState)
            return 1;

        return 0;
    }

    private Sprite[,] BuildFrames()
    {
        Texture2D atlas = Resources.Load<Texture2D>(AtlasResourcePath);
        if (atlas == null)
        {
            Debug.LogError("[AutumnRanger] Missing Resources/" + AtlasResourcePath + ".png", this);
            return null;
        }

        atlas.filterMode = FilterMode.Point;
        atlas.wrapMode = TextureWrapMode.Clamp;

        int cellWidth = atlas.width / Columns;
        int cellHeight = atlas.height / Rows;
        Sprite[,] result = new Sprite[Rows, Columns];

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                int x = column * cellWidth;
                int y = atlas.height - ((row + 1) * cellHeight);
                Rect rect = new Rect(x, y, cellWidth, cellHeight);
                result[row, column] = Sprite.Create(
                    atlas,
                    rect,
                    new Vector2(0.5f, 0.05f),
                    PixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                result[row, column].name = $"AutumnRanger_R{row}_F{column}";
            }
        }

        return result;
    }

    private SpriteRenderer CreateDedicatedRenderer(SpriteRenderer source)
    {
        if (source == null)
            return null;

        source.enabled = false;

        Transform existing = transform.Find("AutumnRangerRenderer");
        GameObject visualObject = existing != null
            ? existing.gameObject
            : new GameObject("AutumnRangerRenderer");

        visualObject.transform.SetParent(transform, false);
        visualObject.transform.localPosition = visualLocalPosition;
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = visualLocalScale;

        SpriteRenderer renderer = visualObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = visualObject.AddComponent<SpriteRenderer>();

        renderer.sharedMaterial = source.sharedMaterial;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        renderer.color = source.color;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private void ShowHurt()
    {
        hurtUntil = Time.time + 0.55f;
    }

    private void ShowDeath()
    {
        isDead = true;
        hurtUntil = Time.time;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth > 0)
            isDead = false;
    }
}
