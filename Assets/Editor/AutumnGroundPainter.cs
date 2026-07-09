using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Tool tổng hợp: Vẽ đất Tilemap + Spawn quái/boss/chướng ngại vật
/// đúng vị trí, có cổng vào hang, logic map mùa Thu.
/// Menu: Tools > Build Full Autumn Level
/// </summary>
public class AutumnLevelBuilder : EditorWindow
{
    // ──────────────────────────────────────────
    //  TILE ASSETS (tự động load từ project)
    // ──────────────────────────────────────────
    private TileBase tileGround;    // Tileset_267 – đất mặt trên (cỏ/ruins)
    private TileBase tileFill;      // Tileset_300 – đất lấp bên dưới
    private TileBase tileCave;      // Tileset_233 – đá hang tối
    private TileBase tileCaveFill;  // Tileset_234 – đá hang bên dưới
    private TileBase tilePlatform;  // Tileset_200 – platform nổi

    // GUIDs hardcoded từ project
    private const string GUID_GROUND   = "addee815ab59d974d9d9f6fd409559cc"; // Tileset_267
    private const string GUID_FILL     = "0a4b1f6ef6a737448b4930d0517e2a1f"; // Tileset_300
    private const string GUID_CAVE     = "cf75da94b223a6c438ea456f318a1ade"; // Tileset_233
    private const string GUID_CAVE_FILL= "c2b91dff968deb049961cad045a05fda"; // Tileset_234
    private const string GUID_PLATFORM = "a69ab45bedb0ed94bb65b1c374305b0e"; // Tileset_200

    // ──────────────────────────────────────────
    //  CẤU HÌNH MAP
    // ──────────────────────────────────────────
    // Tilemap
    private Tilemap groundTilemap;
    private int groundY = -3;      // Y của mặt đất (ô tilemap)
    private int groundDepth = 4;   // Số hàng đất lấp xuống

    // Cổng hang (sprite)
    private string caveEntranceSpritePath =
        "Assets/Sprites/MoiTruong/Xuan/craftpix-net-926878-free-platformer-game-tileset-pixel-art/PNG/cave_entrance.png";

    // Prefab quái nấm
    private const string MUSHROOM_PREFAB = "Assets/Prefab/Mushroom_Enemy.prefab";
    private const string SPIKES_PREFAB   = "Assets/Prefab/Spikes_5.prefab";

    // ──────────────────────────────────────────
    //  CONSTANTS
    // ──────────────────────────────────────────
    //  Đơn vị: 1 ô tilemap = 1 Unity unit (PPU=16 → scale phù hợp)
    //
    //  Layout (tọa độ ô tilemap, Y tính từ groundY lên):
    //
    //  [Spawn]  [Zone1: x= 0→ 52]  [Cửa Hang 1: x=53]  [Zone2: x=60→118]  [Cửa Hang 2: x=119]  [Zone3 Boss: x=126→170]  [Exit: x=171]
    //

    private const int ZONE1_START  = 0;
    private const int ZONE1_END    = 52;
    private const int CAVE1_X      = 53;   // Cổng hang 1
    private const int ZONE2_START  = 60;
    private const int ZONE2_END    = 118;
    private const int CAVE2_X      = 119;  // Cổng hang 2
    private const int ZONE3_START  = 126;
    private const int ZONE3_END    = 170;
    private const int EXIT_X       = 172;

    // ──────────────────────────────────────────
    [MenuItem("Tools/Build Full Autumn Level")]
    public static void ShowWindow()
    {
        GetWindow<AutumnLevelBuilder>("Build Autumn Level");
    }

    private void OnEnable() => LoadTiles();

    private void OnGUI()
    {
        GUILayout.Label("═══ BUILD AUTUMN LEVEL ═══", EditorStyles.boldLabel);
        GUILayout.Space(4);

        // Tilemap target
        groundTilemap = (Tilemap)EditorGUILayout.ObjectField(
            "Tilemap (Ground)", groundTilemap, typeof(Tilemap), true);

        GUILayout.Space(4);
        GUILayout.Label("Tiles (tự động load nếu để trống):", EditorStyles.miniLabel);
        tileGround   = (TileBase)EditorGUILayout.ObjectField("Đất ngoài trời", tileGround,   typeof(TileBase), false);
        tileFill     = (TileBase)EditorGUILayout.ObjectField("Đất lấp (dưới)", tileFill,     typeof(TileBase), false);
        tileCave     = (TileBase)EditorGUILayout.ObjectField("Đá hang (trên)",  tileCave,     typeof(TileBase), false);
        tileCaveFill = (TileBase)EditorGUILayout.ObjectField("Đá hang (dưới)", tileCaveFill, typeof(TileBase), false);
        tilePlatform = (TileBase)EditorGUILayout.ObjectField("Platform nổi",   tilePlatform, typeof(TileBase), false);

        GUILayout.Space(4);
        groundY     = EditorGUILayout.IntField("Y mặt đất",     groundY);
        groundDepth = EditorGUILayout.IntField("Độ sâu đất",    groundDepth);

        GUILayout.Space(8);

        // ─── NÚT CHÍNH ───
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("🏗️  BUILD MAP ĐẦY ĐỦ (Vẽ đất + Cổng + Quái + Boss)", GUILayout.Height(40)))
        {
            BuildAll();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(4);

        // Các nút lẻ
        if (GUILayout.Button("🗺️  Chỉ vẽ đất (Tilemap)"))        { EnsureTilemap(); PaintAllGround(); }
        if (GUILayout.Button("👾  Chỉ spawn quái & Boss"))         { SpawnAllEnemies(); }
        if (GUILayout.Button("🚪  Chỉ tạo cổng hang + Exit"))      { SpawnPortalsAndExit(); }
        if (GUILayout.Button("⚠️  Chỉ tạo chướng ngại vật"))       { SpawnObstacles(); }

        GUILayout.Space(4);
        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("🧹  XÓA TOÀN BỘ (Tilemap + Objects)")) { ClearAll(); }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Layout map:\n" +
            "• Zone 1 (Rừng Thu):  x= 0 → 52   | Quái: Mushroom, StealthGoblin\n" +
            "• Cổng Hang 1:        x= 53\n" +
            "• Zone 2 (Hang Tối):  x=60 → 118  | Quái: SwiftBat, Mushroom | Trần+Tường\n" +
            "• Cổng Hang 2:        x=119\n" +
            "• Zone 3 (Boss Room): x=126 → 170 | Boss: Shadow Dragon | Tường kín\n" +
            "• Exit:               x=172",
            MessageType.Info);
    }

    // ════════════════════════════════════════════════════════════
    //  BUILD ALL
    // ════════════════════════════════════════════════════════════

    private void BuildAll()
    {
        LoadTiles();
        EnsureTilemap();

        if (groundTilemap == null)
        {
            Debug.LogError("Không có Tilemap! Tạo một Tilemap trong Scene trước (2D Object > Tilemap > Rectangular).");
            return;
        }

        // 1. Xóa map cũ
        ClearOldAutumnObjects();
        groundTilemap.ClearAllTiles();

        // 2. Vẽ đất
        PaintAllGround();

        // 3. Cổng hang & exit
        SpawnPortalsAndExit();

        // 4. Chướng ngại vật
        SpawnObstacles();

        // 5. Quái & Boss
        SpawnAllEnemies();

        // 6. Gắn tag Ground cho tilemap
        try { groundTilemap.gameObject.tag = "Ground"; }
        catch { Debug.LogWarning("Tag 'Ground' chưa tồn tại, hãy tạo trong Tags & Layers."); }

        groundTilemap.RefreshAllTiles();

        Debug.Log("<color=#00FF88>════ BUILD AUTUMN LEVEL XONG ════</color>\n" +
                  "• Vẽ 3 khu vực đất (Rừng → Hang → Boss Room)\n" +
                  "• Tạo 2 cổng hang có sprite\n" +
                  "• Spawn quái đúng vị trí trên mặt đất\n" +
                  "• Spawn Boss Shadow Dragon trong Boss Room\n" +
                  "<color=yellow>⚠ Kéo thả Destination cho 2 Portal trong Inspector!\n" +
                  "⚠ Gán Animator Controller cho Boss Dragon!</color>");
    }

    // ════════════════════════════════════════════════════════════
    //  VẼ ĐẤT
    // ════════════════════════════════════════════════════════════

    private void PaintAllGround()
    {
        Undo.RecordObject(groundTilemap, "Paint Autumn Ground");

        // ── ZONE 1: Rừng Thu ──────────────────────────────
        // Mặt đất liền từ 0 → 52 (có 2 hố nhảy)
        PaintStrip(ZONE1_START, ZONE1_END, groundY, groundDepth, tileGround, tileFill);
        // Hố nhảy Zone 1
        ClearStrip(15, 17, groundY, groundDepth + 2);   // Hố 1 (nhảy platform)
        ClearStrip(35, 37, groundY, groundDepth + 2);   // Hố 2

        // Platform nổi Zone 1 (nhảy qua hố)
        PaintStrip(12, 14, groundY + 3, 1, tilePlatform ?? tileGround, null); // Platform trước hố 1
        PaintStrip(18, 22, groundY + 3, 1, tilePlatform ?? tileGround, null); // Platform sau hố 1
        PaintStrip(29, 33, groundY + 4, 1, tilePlatform ?? tileGround, null); // Platform giữa zone 1
        PaintStrip(38, 42, groundY + 3, 1, tilePlatform ?? tileGround, null); // Platform sau hố 2
        PaintStrip(45, 50, groundY + 5, 1, tilePlatform ?? tileGround, null); // Platform cao

        // ── KHU CHUYỂN TIẾP: Cổng vào Hang 1 (x=53–59) ──
        // Bức tường 2 bên cổng + khoảng trống ở giữa (cửa)
        PaintStrip(CAVE1_X - 1, CAVE1_X - 1, groundY, groundDepth + 8, tileCave ?? tileFill, tileCaveFill ?? tileFill); // Tường trái
        PaintStrip(CAVE1_X + 4, CAVE1_X + 4, groundY, groundDepth + 8, tileCave ?? tileFill, tileCaveFill ?? tileFill); // Tường phải
        PaintStrip(CAVE1_X, CAVE1_X + 3, groundY + 8, 1, tileCave ?? tileFill, null);  // Trần cổng hang 1
        PaintStrip(CAVE1_X, CAVE1_X + 3, groundY + 9, 1, tileCave ?? tileFill, null);

        // ── ZONE 2: Hang Tối ──────────────────────────────
        TileBase caveTop  = tileCave     ?? tileGround;
        TileBase caveFill = tileCaveFill ?? tileFill;

        // Mặt đất hang từ 60 → 118
        PaintStrip(ZONE2_START, ZONE2_END, groundY, groundDepth, caveTop, caveFill);
        // Tường trái + phải hang
        PaintWall(ZONE2_START - 1, groundY + 1, 11, caveFill);
        PaintWall(ZONE2_END   + 1, groundY + 1, 11, caveFill);
        // Trần hang
        PaintStrip(ZONE2_START - 1, ZONE2_END + 1, groundY + 11, 2, caveFill, caveFill);

        // Hố trong hang
        ClearStrip(73, 75, groundY, groundDepth + 2);   // Hố hang 1
        ClearStrip(95, 97, groundY, groundDepth + 2);   // Hố hang 2
        ClearStrip(110, 112, groundY, groundDepth + 2); // Hố hang 3

        // Platform trong hang
        PaintStrip(65, 71, groundY + 4, 1, caveTop, null); // Platform 1
        PaintStrip(77, 83, groundY + 3, 1, caveTop, null); // Platform 2 (sau hố 1)
        PaintStrip(87, 93, groundY + 5, 1, caveTop, null); // Platform 3
        PaintStrip(98, 104, groundY + 4, 1, caveTop, null);// Platform 4 (sau hố 2)
        PaintStrip(107, 109, groundY + 6, 1, caveTop, null);// Platform 5
        PaintStrip(113, 117, groundY + 3, 1, caveTop, null);// Platform 6 (sau hố 3)

        // Thạch nhũ từ trần
        PaintStalactite(68,  groundY + 11, 4, caveFill);
        PaintStalactite(82,  groundY + 11, 3, caveFill);
        PaintStalactite(100, groundY + 11, 5, caveFill);
        PaintStalactite(114, groundY + 11, 4, caveFill);

        // ── KHU CHUYỂN TIẾP: Cổng vào Hang 2 (x=119–125) ──
        PaintStrip(CAVE2_X - 1, CAVE2_X - 1, groundY, groundDepth + 8, caveTop, caveFill);
        PaintStrip(CAVE2_X + 4, CAVE2_X + 4, groundY, groundDepth + 8, caveTop, caveFill);
        PaintStrip(CAVE2_X, CAVE2_X + 3, groundY + 8, 2, caveTop, null);

        // ── ZONE 3: Boss Room ─────────────────────────────
        PaintStrip(ZONE3_START, ZONE3_END, groundY, groundDepth, caveTop, caveFill);
        // Tường bao kín Boss Room
        PaintWall(ZONE3_START - 1, groundY + 1, 14, caveFill);
        PaintWall(ZONE3_END   + 1, groundY + 1, 14, caveFill);
        PaintStrip(ZONE3_START - 1, ZONE3_END + 1, groundY + 14, 2, caveFill, caveFill);

        // Platform trong Boss Room (né boss)
        PaintStrip(130, 136, groundY + 4, 1, caveTop, null);
        PaintStrip(143, 150, groundY + 6, 1, caveTop, null);
        PaintStrip(157, 163, groundY + 4, 1, caveTop, null);

        // Exit corridor
        PaintStrip(ZONE3_END + 2, ZONE3_END + 8, groundY, groundDepth, tileGround, tileFill);

        Debug.Log($"<color=cyan>Đã vẽ xong đất! groundY={groundY}</color>");
    }

    // ════════════════════════════════════════════════════════════
    //  CỔNG HANG & EXIT
    // ════════════════════════════════════════════════════════════

    private void SpawnPortalsAndExit()
    {
        GameObject root = GetOrCreateRoot();
        float worldY = TileToWorld(groundY + 1).y;  // ngay trên mặt đất

        // Load sprite cửa hang
        Sprite caveSprite = AssetDatabase.LoadAssetAtPath<Sprite>(caveEntranceSpritePath);

        // ── Cổng hang 1 ──────────────────────────────────
        Vector3 cave1Pos = new Vector3(TileToWorld(CAVE1_X + 1).x, worldY, 0f);
        GameObject cave1 = CreatePortalObject("Portal_Cave1_Enter", cave1Pos, caveSprite, root.transform);

        // SpawnPoint trong hang (nơi Player xuất hiện sau khi đi qua)
        GameObject sp1 = new GameObject("SpawnPoint_InCave1");
        sp1.transform.parent = root.transform;
        sp1.transform.position = new Vector3(TileToWorld(ZONE2_START + 3).x, worldY, 0f);
        // Gán destination
        Teleport t1 = cave1.GetComponent<Teleport>();
        if (t1 != null) t1.destination = sp1.transform;

        // ── Cổng hang 2 ──────────────────────────────────
        Vector3 cave2Pos = new Vector3(TileToWorld(CAVE2_X + 1).x, worldY, 0f);
        GameObject cave2 = CreatePortalObject("Portal_Cave2_Enter", cave2Pos, caveSprite, root.transform);

        GameObject sp2 = new GameObject("SpawnPoint_InBossRoom");
        sp2.transform.parent = root.transform;
        sp2.transform.position = new Vector3(TileToWorld(ZONE3_START + 3).x, worldY, 0f);
        Teleport t2 = cave2.GetComponent<Teleport>();
        if (t2 != null) t2.destination = sp2.transform;

        // ── Exit cuối màn ─────────────────────────────────
        Vector3 exitPos = new Vector3(TileToWorld(EXIT_X).x, worldY, 0f);
        GameObject exit = new GameObject("Exit_To_MainMenu");
        exit.transform.parent = root.transform;
        exit.transform.position = exitPos;
        BoxCollider2D ec = exit.AddComponent<BoxCollider2D>();
        ec.isTrigger = true;
        ec.size = new Vector2(1.5f, 3f);
        LevelTransition lt = exit.AddComponent<LevelTransition>();
        lt.nextSceneName = "MainMenu";
        lt.requireInput = true;

        // Tạo sprite placeholder cho exit (màu vàng sáng)
        SpriteRenderer exitSR = exit.AddComponent<SpriteRenderer>();
        exitSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        exitSR.color  = new Color(1f, 0.85f, 0f, 0.8f);
        exitSR.sortingOrder = 5;
        exit.transform.localScale = new Vector3(1.5f, 3f, 1f);

        Debug.Log($"<color=cyan>Đã tạo 2 cổng hang + Exit tại vị trí tương ứng.</color>" +
                  "\n<color=yellow>⚠ Kiểm tra Destination đã được gán tự động vào Teleport chưa (Inspector)!</color>");
    }

    private GameObject CreatePortalObject(string name, Vector3 pos, Sprite sprite, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = parent;
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite :
                    AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color = sprite != null ? Color.white : new Color(0.3f, 0.8f, 1f, 0.9f);
        sr.sortingOrder = 5;
        if (sprite != null) obj.transform.localScale = new Vector3(2f, 2f, 1f);
        else obj.transform.localScale = new Vector3(2f, 4f, 1f);

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 2f);

        obj.AddComponent<Teleport>();

        Undo.RegisterCreatedObjectUndo(obj, "Create Portal");
        return obj;
    }

    // ════════════════════════════════════════════════════════════
    //  CHƯỚNG NGẠI VẬT (Spikes, Wind Zone, FallingPlatform)
    // ════════════════════════════════════════════════════════════

    private void SpawnObstacles()
    {
        GameObject root = GetOrCreateRoot();
        float worldY = TileToWorld(groundY + 1).y;
        float worldYGap = TileToWorld(groundY).y; // Đáy hố

        // Prefab Spikes
        GameObject spikesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SPIKES_PREFAB);

        // ── Zone 1: Gai ở hố 1 và hố 2 ──
        if (spikesPrefab != null)
        {
            SpawnPrefabAt(spikesPrefab, "Spikes_Z1_Hole1",
                new Vector3(TileToWorld(15).x, worldYGap - 0.2f, 0f), root.transform);
            SpawnPrefabAt(spikesPrefab, "Spikes_Z1_Hole2",
                new Vector3(TileToWorld(35).x, worldYGap - 0.2f, 0f), root.transform);
        }

        // ── Zone 1: Wind Zone (gió thổi từng cơn) ──
        CreateWindZone(root.transform,
            new Vector3(TileToWorld(25).x, worldY + 3f, 0f),
            new Vector2(30f, 6f)); // Bao phủ giữa zone 1

        // ── Zone 2: Gai ở các hố hang ──
        if (spikesPrefab != null)
        {
            SpawnPrefabAt(spikesPrefab, "Spikes_Z2_Hole1",
                new Vector3(TileToWorld(73).x, worldYGap - 0.2f, 0f), root.transform);
            SpawnPrefabAt(spikesPrefab, "Spikes_Z2_Hole2",
                new Vector3(TileToWorld(95).x, worldYGap - 0.2f, 0f), root.transform);
            SpawnPrefabAt(spikesPrefab, "Spikes_Z2_Hole3",
                new Vector3(TileToWorld(110).x, worldYGap - 0.2f, 0f), root.transform);
        }

        // ── Zone 2: FallingPlatform ──
        CreateFallingPlatform(root.transform, "FallingPlatform_1",
            new Vector3(TileToWorld(77).x, TileToWorld(groundY + 3).y, 0f));
        CreateFallingPlatform(root.transform, "FallingPlatform_2",
            new Vector3(TileToWorld(98).x, TileToWorld(groundY + 4).y, 0f));
        CreateFallingPlatform(root.transform, "FallingPlatform_3",
            new Vector3(TileToWorld(107).x, TileToWorld(groundY + 6).y, 0f));

        // ── Zone 2: Wind Zone trong hang ──
        CreateWindZone(root.transform,
            new Vector3(TileToWorld(89).x, TileToWorld(groundY + 5).y, 0f),
            new Vector2(20f, 8f));

        Debug.Log("<color=cyan>Đã tạo chướng ngại vật: Spikes, Wind Zone, FallingPlatform!</color>");
    }

    private void CreateWindZone(Transform parent, Vector3 pos, Vector2 size)
    {
        GameObject wz = new GameObject("Autumn_Wind_Gust");
        wz.transform.parent = parent;
        wz.transform.position = pos;
        BoxCollider2D col = wz.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;
        AutumnWindZone wind = wz.AddComponent<AutumnWindZone>();
        wind.windDirection = Vector2.left;
        wind.windForce = 8f;
        wind.isConstant = false;
        wind.gustInterval = 3f;
        wind.gustDuration = 1.5f;
        Undo.RegisterCreatedObjectUndo(wz, "Create Wind Zone");
    }

    private void CreateFallingPlatform(Transform parent, string name, Vector3 pos)
    {
        GameObject fp = new GameObject(name);
        fp.transform.parent = parent;
        fp.transform.position = pos;
        fp.transform.localScale = new Vector3(3f, 0.5f, 1f);
        SpriteRenderer sr = fp.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color  = new Color(0.6f, 0.35f, 0.1f);
        sr.sortingOrder = 5;
        fp.AddComponent<BoxCollider2D>();
        Rigidbody2D rb = fp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        FallingPlatform fall = fp.AddComponent<FallingPlatform>();
        fall.shakeDelay = 0.5f;
        fall.fallDelay  = 1f;
        fall.respawnDelay = 3f;
        try { fp.tag = "Ground"; } catch { }
        Undo.RegisterCreatedObjectUndo(fp, "Create FallingPlatform");
    }

    // ════════════════════════════════════════════════════════════
    //  SPAWN QUÁI & BOSS
    // ════════════════════════════════════════════════════════════

    private void SpawnAllEnemies()
    {
        GameObject root = GetOrCreateRoot();
        float worldY = TileToWorld(groundY + 1).y; // Y ngay trên mặt đất

        GameObject mushroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MUSHROOM_PREFAB);

        // ── ZONE 1: Rừng Thu ─────────────────────────────
        // Mushroom (mặt đất, trước các hố)
        SpawnMushroomAt(mushroomPrefab, root.transform, "Mushroom_Z1_1",  8f, worldY);
        SpawnMushroomAt(mushroomPrefab, root.transform, "Mushroom_Z1_2", 25f, worldY);
        SpawnMushroomAt(mushroomPrefab, root.transform, "Mushroom_Z1_3", 43f, worldY);

        // StealthGoblin (tàng hình, xuất hiện đột ngột)
        SpawnPlaceholder(root.transform, "StealthGoblin_Z1_1",
            new Vector3(TileToWorld(20).x, worldY, 0f),
            new Color(0.5f, 0f, 0.7f, 0.5f), "AutumnLevel.StealthGoblin");
        SpawnPlaceholder(root.transform, "StealthGoblin_Z1_2",
            new Vector3(TileToWorld(45).x, worldY, 0f),
            new Color(0.5f, 0f, 0.7f, 0.5f), "AutumnLevel.StealthGoblin");

        // ── ZONE 2: Hang Tối ─────────────────────────────
        // Mushroom trên platform hang
        SpawnMushroomAt(mushroomPrefab, root.transform, "Mushroom_Z2_1",
            TileToWorld(67).x, TileToWorld(groundY + 5).y); // trên platform 1
        SpawnMushroomAt(mushroomPrefab, root.transform, "Mushroom_Z2_2",
            TileToWorld(80).x, TileToWorld(groundY + 4).y); // trên platform 2
        SpawnMushroomAt(mushroomPrefab, root.transform, "Mushroom_Z2_3",
            TileToWorld(100).x, TileToWorld(groundY + 5).y);// trên platform 4

        // SwiftBat (quái bay — nổi trong hang)
        SpawnPlaceholder(root.transform, "SwiftBat_Z2_1",
            new Vector3(TileToWorld(70).x, TileToWorld(groundY + 7).y, 0f),
            Color.cyan, "AutumnLevel.SwiftBat");
        SpawnPlaceholder(root.transform, "SwiftBat_Z2_2",
            new Vector3(TileToWorld(90).x, TileToWorld(groundY + 8).y, 0f),
            Color.cyan, "AutumnLevel.SwiftBat");
        SpawnPlaceholder(root.transform, "SwiftBat_Z2_3",
            new Vector3(TileToWorld(108).x, TileToWorld(groundY + 7).y, 0f),
            Color.cyan, "AutumnLevel.SwiftBat");

        // Autumn Guardian (mini-boss gác cổng hang 2)
        GameObject guardian = SpawnPlaceholder(root.transform, "Autumn_Guardian_MiniBoss",
            new Vector3(TileToWorld(115).x, worldY, 0f),
            new Color(0.2f, 0.9f, 0.3f), null);
        guardian.transform.localScale = new Vector3(2f, 2f, 1f);
        SetupGuardianScripts(guardian);

        // ── ZONE 3: Boss Room ─────────────────────────────
        // Shadow Dragon Boss (giữa boss room)
        float bossWorldY = TileToWorld(groundY + 1).y;
        float bossWorldX = TileToWorld((ZONE3_START + ZONE3_END) / 2).x;

        GameObject boss = SpawnPlaceholder(root.transform, "BOSS_Shadow_Dragon",
            new Vector3(bossWorldX, bossWorldY, 0f),
            new Color(0.3f, 0f, 0.5f), null);
        boss.transform.localScale = new Vector3(3f, 3f, 1f);
        SetupShadowDragonScripts(boss);

        // SpawnPoint Boss (để Teleport portal → đây)
        GameObject spBoss = new GameObject("SpawnPoint_Boss");
        spBoss.transform.parent = root.transform;
        spBoss.transform.position = new Vector3(TileToWorld(ZONE3_START + 3).x, bossWorldY, 0f);

        // Camera Bounds Boss Room
        CreateBossCameraBounds(root.transform);

        Debug.Log($"<color=green>Đã spawn:\n" +
                  $"• 3 Mushroom + 2 StealthGoblin (Zone 1)\n" +
                  $"• 3 Mushroom + 3 SwiftBat + 1 Guardian (Zone 2)\n" +
                  $"• 1 Shadow Dragon Boss (Zone 3)</color>");
    }

    // ─── Helper: Spawn Mushroom prefab ───
    private void SpawnMushroomAt(GameObject prefab, Transform parent, string name, float worldX, float worldY)
    {
        Vector3 pos = new Vector3(worldX, worldY, 0f);
        GameObject obj;

        if (prefab != null)
        {
            obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
        }
        else
        {
            obj = SpawnPlaceholder(parent, name, pos, new Color(0.8f, 0.4f, 0f), null);
        }

        obj.transform.parent   = parent;
        obj.transform.position = pos;

        FixEnemyBasics(obj);
        Undo.RegisterCreatedObjectUndo(obj, "Spawn " + name);
    }

    // ─── Helper: Spawn placeholder (cube màu) ───
    // scriptTypeName: full name kể cả namespace, ví dụ "AutumnLevel.SwiftBat" hoặc null
    private GameObject SpawnPlaceholder(Transform parent, string name, Vector3 pos, Color color, string scriptTypeName)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent   = parent;
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color  = color;
        sr.sortingOrder = 50;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale   = 1f;
        obj.AddComponent<BoxCollider2D>();
        Health h = obj.AddComponent<Health>();
        h.maxHealth = 80;

        if (!string.IsNullOrEmpty(scriptTypeName))
        {
            // Tìm type trong tất cả assemblies đang load
            System.Type t = System.Type.GetType(scriptTypeName);
            if (t == null)
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType(scriptTypeName);
                    if (t != null) break;
                }
            }
            if (t != null)
                obj.AddComponent(t);
            else
                Debug.LogWarning($"[AutumnBuilder] Không tìm thấy type '{scriptTypeName}'. Script chưa được gắn, hãy gắn thủ công.");
        }

        FixEnemyLayer(obj);
        Undo.RegisterCreatedObjectUndo(obj, "Spawn " + name);
        return obj;
    }

    // ─── Setup boss scripts ───
    private void SetupShadowDragonScripts(GameObject boss)
    {
        if (boss.GetComponent<Rigidbody2D>() == null) { var rb = boss.AddComponent<Rigidbody2D>(); rb.freezeRotation = true; }
        if (boss.GetComponent<BoxCollider2D>() == null) boss.AddComponent<BoxCollider2D>();
        Health h = boss.GetComponent<Health>(); if (h == null) h = boss.AddComponent<Health>();
        h.maxHealth = 200;

        ShadowDragonBoss dragon = boss.GetComponent<ShadowDragonBoss>();
        if (dragon == null) dragon = boss.AddComponent<ShadowDragonBoss>();
        dragon.bossName        = "Shadow Demon Dragon";
        dragon.aggroRange      = 16f;
        dragon.attackRange     = 2.5f;
        dragon.moveSpeed       = 3f;
        dragon.attackDamage    = 20;
        dragon.attackCooldown  = 1.5f;
        dragon.projectileSpeed = 8f;
        dragon.projectileDamage = 10;
        dragon.breathCount     = 5;
        dragon.maxSummons      = 2;

        // Tự động load Dragon sprite
        string[] guids = AssetDatabase.FindAssets("Idle_Right t:Texture2D",
            new[] { "Assets/Sprites/NhanVat/Shadow_Demon_Dragon" });
        if (guids.Length > 0)
        {
            string sp = AssetDatabase.GUIDToAssetPath(guids[0]);
            Sprite  s  = AssetDatabase.LoadAssetAtPath<Sprite>(sp);
            SpriteRenderer sr = boss.GetComponent<SpriteRenderer>();
            if (sr != null && s != null) sr.sprite = s;
        }

        // FirePoint
        GameObject fp = new GameObject("FirePoint");
        fp.transform.parent = boss.transform;
        fp.transform.localPosition = new Vector3(1.2f, 0.5f, 0f);
        dragon.firePoint = fp.transform;

        // Audio
        TryLoadDragonAudio(dragon);
        FixEnemyLayer(boss);
    }

    private void SetupGuardianScripts(GameObject guardian)
    {
        if (guardian.GetComponent<Rigidbody2D>() == null) { var rb = guardian.AddComponent<Rigidbody2D>(); rb.freezeRotation = true; }
        if (guardian.GetComponent<BoxCollider2D>() == null) guardian.AddComponent<BoxCollider2D>();
        Health h = guardian.GetComponent<Health>(); if (h == null) h = guardian.AddComponent<Health>();
        h.maxHealth = 120;
        AutumnBoss ab = guardian.GetComponent<AutumnBoss>();
        if (ab == null) ab = guardian.AddComponent<AutumnBoss>();
        ab.bossName    = "Autumn Guardian";
        ab.aggroRange  = 12f;
        ab.attackRange = 2f;
        ab.moveSpeed   = 2f;
        FixEnemyLayer(guardian);
    }

    private void TryLoadDragonAudio(ShadowDragonBoss dragon)
    {
        string[] ags = AssetDatabase.FindAssets("t:AudioClip",
            new[] { "Assets/Sprites/NhanVat/Shadow_Demon_Dragon/Audio" });
        foreach (string g in ags)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g));
            if (clip == null) continue;
            string n = clip.name.ToLower();
            if (n.Contains("attack")) dragon.attackSFX = clip;
            else if (n.Contains("hit")) dragon.hitSFX  = clip;
            else if (n.Contains("death") && !n.Contains("fall")) dragon.deathSFX = clip;
            else if (n.Contains("idle")) dragon.idleSFX = clip;
        }
    }

    private void CreateBossCameraBounds(Transform parent)
    {
        GameObject bb = new GameObject("BossRoom_CameraBounds");
        bb.transform.parent   = parent;
        float cx = TileToWorld((ZONE3_START + ZONE3_END) / 2).x;
        float cy = TileToWorld(groundY + 7).y;
        bb.transform.position = new Vector3(cx, cy, 0f);
        PolygonCollider2D pc = bb.AddComponent<PolygonCollider2D>();
        pc.isTrigger = true;
        float hw = TileToWorld(ZONE3_END - ZONE3_START).x / 2f + 1f;
        float hh = 8f;
        pc.points = new Vector2[]
        {
            new Vector2(-hw, -hh), new Vector2(hw, -hh),
            new Vector2(hw,  hh), new Vector2(-hw, hh)
        };
    }

    // ════════════════════════════════════════════════════════════
    //  TILEMAP HELPERS
    // ════════════════════════════════════════════════════════════

    private void PaintStrip(int xStart, int xEnd, int yTop, int depth, TileBase topTile, TileBase fillTile)
    {
        if (groundTilemap == null || topTile == null) return;
        for (int x = xStart; x <= xEnd; x++)
        {
            groundTilemap.SetTile(new Vector3Int(x, yTop, 0), topTile);
            if (fillTile != null)
                for (int d = 1; d < depth; d++)
                    groundTilemap.SetTile(new Vector3Int(x, yTop - d, 0), fillTile);
        }
    }

    private void ClearStrip(int xStart, int xEnd, int yTop, int depth)
    {
        if (groundTilemap == null) return;
        for (int x = xStart; x <= xEnd; x++)
            for (int d = 0; d < depth; d++)
                groundTilemap.SetTile(new Vector3Int(x, yTop - d, 0), null);
    }

    private void PaintWall(int x, int yStart, int height, TileBase tile)
    {
        if (groundTilemap == null || tile == null) return;
        for (int y = yStart; y < yStart + height; y++)
            groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private void PaintStalactite(int x, int yTop, int length, TileBase tile)
    {
        if (groundTilemap == null || tile == null) return;
        for (int i = 1; i <= length; i++)
            groundTilemap.SetTile(new Vector3Int(x, yTop - i, 0), tile);
    }

    // Chuyển tọa độ tile sang World position
    private Vector3 TileToWorld(int tileX, int tileY = 0)
    {
        if (groundTilemap != null)
            return groundTilemap.CellToWorld(new Vector3Int(tileX, tileY, 0));
        return new Vector3(tileX, tileY, 0f); // Fallback: 1 tile = 1 unit
    }

    // ════════════════════════════════════════════════════════════
    //  UTILITY
    // ════════════════════════════════════════════════════════════

    private void LoadTiles()
    {
        if (tileGround   == null) tileGround   = LoadTile(GUID_GROUND);
        if (tileFill     == null) tileFill     = LoadTile(GUID_FILL);
        if (tileCave     == null) tileCave     = LoadTile(GUID_CAVE);
        if (tileCaveFill == null) tileCaveFill = LoadTile(GUID_CAVE_FILL);
        if (tilePlatform == null) tilePlatform = LoadTile(GUID_PLATFORM);
    }

    private TileBase LoadTile(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<TileBase>(path);
    }

    private void EnsureTilemap()
    {
        if (groundTilemap == null)
            groundTilemap = FindObjectOfType<Tilemap>();
    }

    private GameObject GetOrCreateRoot()
    {
        GameObject root = GameObject.Find("═══ AUTUMN_MAP ═══");
        if (root == null)
        {
            root = new GameObject("═══ AUTUMN_MAP ═══");
            Undo.RegisterCreatedObjectUndo(root, "Create Autumn Root");
        }
        return root;
    }

    private void SpawnPrefabAt(GameObject prefab, string name, Vector3 pos, Transform parent)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        obj.name = name;
        obj.transform.parent   = parent;
        obj.transform.position = pos;
        Undo.RegisterCreatedObjectUndo(obj, "Spawn " + name);
    }

    private void FixEnemyBasics(GameObject obj)
    {
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = 50;
        Vector3 p = obj.transform.position; p.z = 0f;
        obj.transform.position = p;
        FixEnemyLayer(obj);
        Health h = obj.GetComponent<Health>();
        if (h != null && h.maxHealth <= 0) h.maxHealth = 80;
    }

    private void FixEnemyLayer(GameObject obj)
    {
        int layer = LayerMask.NameToLayer("Enemy");
        if (layer < 0) return;
        obj.layer = layer;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>())
            t.gameObject.layer = layer;
    }

    private void ClearOldAutumnObjects()
    {
        GameObject old = GameObject.Find("═══ AUTUMN_MAP ═══");
        if (old != null) Undo.DestroyObjectImmediate(old);
    }

    private void ClearAll()
    {
        ClearOldAutumnObjects();
        EnsureTilemap();
        if (groundTilemap != null)
        {
            Undo.RecordObject(groundTilemap, "Clear Tilemap");
            groundTilemap.ClearAllTiles();
        }
        Debug.Log("<color=yellow>Đã xóa toàn bộ map + tilemap!</color>");
    }
}
