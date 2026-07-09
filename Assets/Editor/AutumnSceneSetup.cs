using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Tool: Setup đầy đủ Scene Autumn Ruins
/// - Tạo background parallax tile cho cả 3 zone
/// - Đổi nhân vật sang Archer nữ
/// - Gắn CameraFollow vào Main Camera
/// Menu: Tools > Setup Autumn Scene
/// </summary>
public class AutumnSceneSetup : EditorWindow
{
    // GUIDs background Outdoor
    private const string BG_OUTDOOR_MAIN  = "8f7ba25b61c5ae44bb8ad2b6ce5b8d2a"; // background 1.png
    private const string BG_OUTDOOR_P1    = "68806e995d7b43646b9327a290a4b994"; // Plan-1
    private const string BG_OUTDOOR_P2    = "fcbd4f5ecd5a5684fb630f38ed7f3122"; // Plan-2
    private const string BG_OUTDOOR_P3    = "9fb9c4b4c62261d4194e8207530b0ada"; // Plan-3
    private const string BG_OUTDOOR_P4    = "d9a9cea802f02924eb7eb76695bfc0bd"; // Plan-4
    private const string BG_OUTDOOR_P5    = "22d696e3e53475748a31aaf0da0cfea6"; // Plan-5

    // GUIDs Archer
    private const string ARCHER_IDLE      = "a8177cb609b89b84aad2ef7a57d11927"; // Archer idle Sheet.png
    private const string ARCHER_WALK      = "2895e123e21dbe74eb817e6678e11b61"; // Archer walk Sheet.png
    private const string ARCHER_JUMP      = "43e532abda393ae4a955421bb64bba94"; // Archer Jump Sheet.png
    private const string ARCHER_ATTACK    = "a673c3eb97969cc4fa3be43bed810c6f"; // Archer attack Sheet.png
    private const string ARCHER_HIT       = "775e75e68320dc94f855aa023aa49c67"; // Archer Being hit
    private const string ARCHER_DEATH     = "04490dacdcddbe74d84593ca4f6034a4"; // Archer death
    private const string ARCHER_BASE      = "958e0991c74f88045b74bc0dce96617d"; // Archer base sprite

    // Map layout (đồng bộ với AutumnGroundPainter)
    private const float ZONE1_START_X = 0f;
    private const float ZONE3_END_X   = 180f;
    private const float MAP_WIDTH     = 180f; // tổng chiều rộng map (units)
    private const float MAP_CENTER_Y  = 5f;   // Y tâm của background

    [MenuItem("Tools/Setup Autumn Scene")]
    public static void ShowWindow()
    {
        GetWindow<AutumnSceneSetup>("Setup Autumn Scene");
    }

    private void OnGUI()
    {
        GUILayout.Label("═══ SETUP AUTUMN SCENE ═══", EditorStyles.boldLabel);
        GUILayout.Space(6);

        // ── BACKGROUND ──
        GUILayout.Label("🌅 BACKGROUND", EditorStyles.boldLabel);
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("Tạo Background Tiled cho toàn bộ Map", GUILayout.Height(35)))
            SetupBackground();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);

        // ── CAMERA ──
        GUILayout.Label("📷 CAMERA", EditorStyles.boldLabel);
        GUI.backgroundColor = new Color(0.4f, 1f, 0.6f);
        if (GUILayout.Button("Gắn CameraFollow vào Main Camera", GUILayout.Height(35)))
            SetupCamera();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);

        // ── ARCHER ──
        GUILayout.Label("🏹 NHÂN VẬT ARCHER", EditorStyles.boldLabel);
        GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
        if (GUILayout.Button("Đổi sprite Player sang Nữ Archer", GUILayout.Height(35)))
            SetupArcherSprite();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);

        // ── ALL IN ONE ──
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("⚡ LÀM TẤT CẢ (Background + Camera + Archer)", GUILayout.Height(45)))
        {
            SetupBackground();
            SetupCamera();
            SetupArcherSprite();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Thứ tự khuyên dùng:\n" +
            "1. Đã bấm 'Build Full Autumn Level' rồi\n" +
            "2. Bấm 'LÀM TẤT CẢ' để setup nốt Camera + BG + Archer\n" +
            "3. Bấm Play để test",
            MessageType.Info);
    }

    // ════════════════════════════════════════════════════════════
    //  BACKGROUND TILED
    // ════════════════════════════════════════════════════════════

    private void SetupBackground()
    {
        // Tìm hoặc tạo container
        GameObject bgRoot = GameObject.Find("[BackGround]");
        if (bgRoot == null) bgRoot = new GameObject("[BackGround]");

        // Xóa các child cũ của background để rebuild
        for (int i = bgRoot.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(bgRoot.transform.GetChild(i).gameObject);

        // Outdoor background layers (zone 1 + zone 2 approach)
        // Mỗi layer là 1 dải sprite trải dài từ đầu đến cuối map

        // Layer thứ tự từ xa (Z cao) đến gần (Z thấp)
        // SortingOrder: Sky=0, P5=1, P4=2, P3=3, P2=4, P1=5, Ground=6
        CreateTiledBackground(bgRoot.transform, "BG_Sky",   BG_OUTDOOR_MAIN, MAP_WIDTH, 9f,  0,  0f);
        CreateTiledBackground(bgRoot.transform, "BG_P5",    BG_OUTDOOR_P5,   MAP_WIDTH, 8f,  1, -0.05f);
        CreateTiledBackground(bgRoot.transform, "BG_P4",    BG_OUTDOOR_P4,   MAP_WIDTH, 7f,  2, -0.1f);
        CreateTiledBackground(bgRoot.transform, "BG_P3",    BG_OUTDOOR_P3,   MAP_WIDTH, 6f,  3, -0.15f);
        CreateTiledBackground(bgRoot.transform, "BG_P2",    BG_OUTDOOR_P2,   MAP_WIDTH, 5f,  4, -0.2f);
        CreateTiledBackground(bgRoot.transform, "BG_P1",    BG_OUTDOOR_P1,   MAP_WIDTH, 4f,  5, -0.25f);

        // Gắn ParallaxBackground script vào từng layer
        foreach (Transform child in bgRoot.transform)
        {
            ParallaxBackground pb = child.GetComponent<ParallaxBackground>();
            if (pb == null) pb = child.gameObject.AddComponent<ParallaxBackground>();
        }

        Debug.Log("<color=cyan>✅ Đã tạo background tiled trải dài toàn bộ map!</color>\n" +
                  $"Chiều rộng: {MAP_WIDTH} units | 6 layer parallax");
    }

    private void CreateTiledBackground(Transform parent, string layerName, string guid,
        float totalWidth, float yPos, int sortingOrder, float parallaxFactor)
    {
        Sprite sprite = LoadSprite(guid);
        if (sprite == null)
        {
            Debug.LogWarning($"Không load được sprite '{layerName}' (guid: {guid})");
            return;
        }

        // Tính kích thước sprite trong world units
        float spriteWidth  = sprite.bounds.size.x;
        float spriteHeight = sprite.bounds.size.y;

        // Tính số lần cần tile
        int tileCount = Mathf.CeilToInt(totalWidth / spriteWidth) + 2;

        GameObject layerObj = new GameObject(layerName);
        layerObj.transform.parent   = parent;
        layerObj.transform.position = new Vector3(ZONE1_START_X, yPos, 10f);

        for (int i = 0; i < tileCount; i++)
        {
            GameObject tile = new GameObject($"{layerName}_tile{i}");
            tile.transform.parent = layerObj.transform;
            tile.transform.position = new Vector3(
                ZONE1_START_X + i * spriteWidth,
                yPos,
                10f);

            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = sortingOrder;
            sr.sortingLayerName = "Background"; // Nếu có sorting layer này
        }

        // Gắn parallax factor (để scroll chậm hơn khi camera move)
        ParallaxBackground pb = layerObj.AddComponent<ParallaxBackground>();
        pb.parallaxMultiplier = Mathf.Abs(parallaxFactor);

        Undo.RegisterCreatedObjectUndo(layerObj, "Create BG Layer " + layerName);
    }

    // ════════════════════════════════════════════════════════════
    //  CAMERA FOLLOW
    // ════════════════════════════════════════════════════════════

    private void SetupCamera()
    {
        // Tìm Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Không tìm thấy Main Camera trong scene!");
            return;
        }

        // Gắn CameraFollow
        CameraFollow cf = mainCam.GetComponent<CameraFollow>();
        if (cf == null) cf = mainCam.gameObject.AddComponent<CameraFollow>();

        // Tự động tìm Player
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            cf.target = playerObj.transform;
            Debug.Log($"<color=cyan>✅ Camera sẽ follow: {playerObj.name}</color>");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Player! Kéo Player vào field 'Target' của CameraFollow trong Inspector.");
        }

        // Cấu hình bounds (khớp với map layout)
        cf.offset     = new Vector3(3f, 2f, -10f);
        cf.smoothSpeed = 5f;
        cf.minX = -5f;
        cf.maxX = MAP_WIDTH + 5f;
        cf.minY = -8f;
        cf.maxY = 15f;

        // Đặt camera ở vị trí ban đầu ngay trên Player
        if (playerObj != null)
            mainCam.transform.position = playerObj.transform.position + cf.offset;

        EditorUtility.SetDirty(mainCam.gameObject);
        Debug.Log("<color=cyan>✅ CameraFollow đã được gắn vào Main Camera!</color>\n" +
                  "bounds: X[" + cf.minX + "," + cf.maxX + "] Y[" + cf.minY + "," + cf.maxY + "]");
    }

    // ════════════════════════════════════════════════════════════
    //  ARCHER SPRITE SETUP
    // ════════════════════════════════════════════════════════════

    private void SetupArcherSprite()
    {
        // Tìm Player
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            Debug.LogError("Không tìm thấy Player trong Scene!");
            return;
        }

        // Load sprite Archer Idle (frame đầu tiên)
        Sprite idleSheet = LoadSprite(ARCHER_IDLE);
        if (idleSheet == null)
        {
            Debug.LogError("Không load được Archer idle sprite!");
            return;
        }

        // Đổi SpriteRenderer
        SpriteRenderer sr = playerObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = playerObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Undo.RecordObject(sr, "Set Archer Sprite");
            sr.sprite = idleSheet;
            // Scale phù hợp với archer sprite (thường nhỏ hơn knight)
            playerObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            EditorUtility.SetDirty(sr);
        }

        // Scale Player collider nếu cần
        CapsuleCollider2D cap = playerObj.GetComponent<CapsuleCollider2D>();
        if (cap != null)
        {
            Undo.RecordObject(cap, "Set Archer Collider");
            cap.size   = new Vector2(0.6f, 1.4f);
            cap.offset = new Vector2(0f, 0f);
            EditorUtility.SetDirty(cap);
        }

        Debug.Log("<color=#FFAA00>✅ Đã đổi sprite Player sang Archer!</color>\n" +
                  "<color=yellow>⚠ Bạn cần tạo Animator Controller cho Archer:\n" +
                  "  1. Project → Create → Animator Controller → đặt tên 'ArcherAnimator'\n" +
                  "  2. Kéo các sprite sheet vào tạo clip: Idle/Walk/Jump/Attack/Hit/Death\n" +
                  "  3. Kéo 'ArcherAnimator' vào field Animator của Player</color>");

        // In danh sách sprite sheet để làm animation
        Debug.Log("<color=cyan>Các sprite sheet Archer có sẵn:\n" +
                  "• Idle:   Assets/Sprites/NhanVat/archer pack Rpg Miniatures/Archer idle Sheet.png\n" +
                  "• Walk:   .../Archer walk Sheet.png\n" +
                  "• Jump:   .../Archer Jump Sheet.png\n" +
                  "• Attack: .../Archer attack Sheet.png\n" +
                  "• Hit:    .../Archer Being hit Sheet.png\n" +
                  "• Death:  .../Archer death -Sheet.png</color>");
    }

    // ════════════════════════════════════════════════════════════
    //  UTILITY
    // ════════════════════════════════════════════════════════════

    private Sprite LoadSprite(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
