using UnityEngine;
using UnityEditor;
using AutumnLevel;

/// <summary>
/// Editor Window hỗ trợ thiết lập toàn bộ Màn Mùa Thu (AutumnRuins).
/// Menu: Tools > Setup Autumn Level Scripts
/// </summary>
public class AutumnLevelSetup : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("Tools/Setup Autumn Level Scripts")]
    public static void ShowWindow()
    {
        GetWindow<AutumnLevelSetup>("Setup Autumn Scripts");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ═══════════════════════════════════════════
        //  SECTION 1: MAP GENERATION
        // ═══════════════════════════════════════════
        GUILayout.Label("═══ TẠO MAP ═══", EditorStyles.boldLabel);

        if (GUILayout.Button("🗺️ TẠO MAP AUTUMN ĐẦY ĐỦ (3 khu vực + 2 hang)"))
        {
            SpawnFullAutumnMap();
        }

        if (GUILayout.Button("🖼️ Setup Parallax Background (Outdoor)"))
        {
            SetupParallaxBackground(false);
        }

        if (GUILayout.Button("🖼️ Setup Parallax Background (Cave)"))
        {
            SetupParallaxBackground(true);
        }

        GUILayout.Space(10);

        // ═══════════════════════════════════════════
        //  SECTION 2: SPAWN ENEMIES
        // ═══════════════════════════════════════════
        GUILayout.Label("═══ SPAWN QUÁI & BOSS ═══", EditorStyles.boldLabel);

        if (GUILayout.Button("⚔️ Spawn Mushroom Enemy (Có sẵn hình ảnh)"))
        {
            SpawnMushroomEnemy();
        }

        if (GUILayout.Button("🐉 Spawn Shadow Dragon Boss"))
        {
            SpawnShadowDragonBoss();
        }

        GUILayout.Space(10);

        // ═══════════════════════════════════════════
        //  SECTION 3: GẮN SCRIPT
        // ═══════════════════════════════════════════
        GUILayout.Label("═══ GẮN SCRIPT (Chọn trong Hierarchy) ═══", EditorStyles.boldLabel);

        if (GUILayout.Button("1. 🏹 Gắn Script cho Player (Archer)"))
        {
            SetupPlayer();
        }

        if (GUILayout.Button("2. 🐉 Gắn Script cho Shadow Dragon Boss Đang Chọn"))
        {
            SetupShadowDragonBoss();
        }

        if (GUILayout.Button("2b. 🛡️ Gắn Script cho Autumn Guardian (Mini-Boss)"))
        {
            SetupAutumnGuardian();
        }

        if (GUILayout.Button("3. Gắn Script Quái Thường (GoblinEnemy)"))
        {
            SetupSlime();
        }

        if (GUILayout.Button("3b. Gắn Script QUÁI TÀNG HÌNH (StealthGoblin)"))
        {
            SetupStealthEnemy();
        }

        if (GUILayout.Button("3c. Gắn Script QUÁI BAY NHANH (SwiftBat)"))
        {
            SetupSwiftEnemy();
        }

        if (GUILayout.Button("4. Gắn Script Teleport (Cổng) Đang Chọn"))
        {
            SetupTeleport();
        }

        if (GUILayout.Button("5. Gắn Script CaveZone (Khu vực hang)"))
        {
            SetupCaveZone();
        }

        GUILayout.Space(10);

        // ═══════════════════════════════════════════
        //  SECTION 4: CÔNG CỤ SỬA LỖI
        // ═══════════════════════════════════════════
        GUILayout.Label("═══ CÔNG CỤ SỬA LỖI ═══", EditorStyles.boldLabel);

        if (GUILayout.Button("🛠 Sửa Lỗi Không Thấy Quái/Boss"))
        {
            FixVisibility();
        }

        EditorGUILayout.EndScrollView();
    }

    // ════════════════════════════════════════════════════════════════
    //  FULL MAP GENERATION
    // ════════════════════════════════════════════════════════════════

    private void SpawnFullAutumnMap()
    {
        // Tìm prefab quái nấm
        string[] guids = AssetDatabase.FindAssets("Mushroom_Enemy t:Prefab");
        GameObject mushroomPrefab = null;
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            mushroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // Tìm Player làm điểm gốc
        Vector3 basePos = Vector3.zero;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) basePos = player.transform.position;

        // ── ROOT ──
        GameObject root = new GameObject("═══ AUTUMN_MAP ═══");
        Undo.RegisterCreatedObjectUndo(root, "Create Autumn Map");

        // ════════════════════════════════════════════
        //  KHU 1: RỪNG THU (x = 0 → 60)
        // ════════════════════════════════════════════
        GameObject zone1 = CreateZone(root, "Zone1_ForestRuins", basePos);

        // Spawn quái thường ở khu 1
        if (mushroomPrefab != null)
        {
            float[] offsets1 = { 8f, 16f, 24f, 35f, 45f };
            for (int i = 0; i < offsets1.Length; i++)
            {
                Vector3 pos = new Vector3(basePos.x + offsets1[i], basePos.y, 0f);
                SpawnEnemyAtPosition(mushroomPrefab, pos, $"Mushroom_Zone1_{i + 1}", zone1.transform);
            }
        }

        // StealthGoblin ở khu 1
        SpawnPlaceholderEnemy(zone1.transform, "StealthGoblin_Zone1",
            new Vector3(basePos.x + 30f, basePos.y, 0f), new Color(0.5f, 0f, 0.5f, 0.5f));
        SpawnPlaceholderEnemy(zone1.transform, "StealthGoblin_Zone1_2",
            new Vector3(basePos.x + 50f, basePos.y, 0f), new Color(0.5f, 0f, 0.5f, 0.5f));

        // Cổng vào hang 1 (ở cuối khu 1)
        CreatePortal(zone1.transform, "Portal_To_Cave1",
            new Vector3(basePos.x + 58f, basePos.y + 1f, 0f),
            "treasure cave entrance");

        // ════════════════════════════════════════════
        //  KHU 2: HANG ĐỘNG (x = 70 → 130)
        // ════════════════════════════════════════════
        float cave1X = basePos.x + 70f;
        GameObject zone2 = CreateZone(root, "Zone2_CaveDark", new Vector3(cave1X, basePos.y, 0f));

        // Spawn SwiftBat ở hang
        SpawnPlaceholderEnemy(zone2.transform, "SwiftBat_Cave_1",
            new Vector3(cave1X + 10f, basePos.y + 3f, 0f), Color.cyan);
        SpawnPlaceholderEnemy(zone2.transform, "SwiftBat_Cave_2",
            new Vector3(cave1X + 25f, basePos.y + 4f, 0f), Color.cyan);
        SpawnPlaceholderEnemy(zone2.transform, "SwiftBat_Cave_3",
            new Vector3(cave1X + 40f, basePos.y + 2f, 0f), Color.cyan);

        // Mushroom trong hang
        if (mushroomPrefab != null)
        {
            float[] offsets2 = { 15f, 30f, 45f };
            for (int i = 0; i < offsets2.Length; i++)
            {
                Vector3 pos = new Vector3(cave1X + offsets2[i], basePos.y, 0f);
                SpawnEnemyAtPosition(mushroomPrefab, pos, $"Mushroom_Cave_{i + 1}", zone2.transform);
            }
        }

        // FallingPlatform trong hang
        for (int i = 0; i < 3; i++)
        {
            GameObject fp = CreateFallingPlatform(zone2.transform,
                new Vector3(cave1X + 18f + i * 12f, basePos.y + 2f, 0f), i + 1);
        }

        // Autumn Guardian (mini-boss gác cổng hang 2)
        GameObject guardian = SpawnPlaceholderEnemy(zone2.transform, "AutumnGuardian_MiniBoss",
            new Vector3(cave1X + 55f, basePos.y, 0f), new Color(0.2f, 0.8f, 0.3f));
        guardian.transform.localScale = new Vector3(2f, 2f, 1f);

        // Cổng vào phòng Boss (cuối hang)
        CreatePortal(zone2.transform, "Portal_To_BossRoom",
            new Vector3(cave1X + 58f, basePos.y + 1f, 0f),
            "treasure cave entrance");

        // ════════════════════════════════════════════
        //  KHU 3: PHÒNG BOSS DRAGON (x = 140 → 180)
        // ════════════════════════════════════════════
        float bossX = basePos.x + 145f;
        GameObject zone3 = CreateZone(root, "Zone3_BossRoom", new Vector3(bossX, basePos.y, 0f));

        // SpawnPoint cho Boss
        GameObject bossSpawnPoint = new GameObject("SpawnPoint_Boss");
        bossSpawnPoint.transform.parent = zone3.transform;
        bossSpawnPoint.transform.position = new Vector3(bossX + 15f, basePos.y, 0f);

        // Shadow Dragon Boss
        GameObject dragonBoss = SpawnPlaceholderEnemy(zone3.transform, "BOSS_Shadow_Dragon",
            new Vector3(bossX + 15f, basePos.y, 0f), new Color(0.3f, 0f, 0.5f));
        dragonBoss.transform.localScale = new Vector3(3f, 3f, 1f);

        // Gắn ShadowDragonBoss script
        ConfigureShadowDragonBoss(dragonBoss);

        // Exit cuối màn
        GameObject exitObj = new GameObject("Exit_To_MainMenu");
        exitObj.transform.parent = zone3.transform;
        exitObj.transform.position = new Vector3(bossX + 35f, basePos.y + 1f, 0f);
        BoxCollider2D exitColl = exitObj.AddComponent<BoxCollider2D>();
        exitColl.isTrigger = true;
        exitColl.size = new Vector2(2f, 3f);
        LevelTransition exitTransition = exitObj.AddComponent<LevelTransition>();
        exitTransition.nextSceneName = "MainMenu";
        exitTransition.requireInput = true;

        // ── AutumnWindZone xuyên suốt khu 1 ──
        GameObject windZone = new GameObject("Autumn_Wind_Gust");
        windZone.transform.parent = zone1.transform;
        windZone.transform.position = new Vector3(basePos.x + 30f, basePos.y + 3f, 0f);
        BoxCollider2D windColl = windZone.AddComponent<BoxCollider2D>();
        windColl.isTrigger = true;
        windColl.size = new Vector2(60f, 10f);
        AutumnWindZone wind = windZone.AddComponent<AutumnWindZone>();
        wind.windDirection = Vector2.left;
        wind.windForce = 8f;
        wind.isConstant = false;
        wind.gustInterval = 3f;
        wind.gustDuration = 1.5f;

        // ── Camera Bounds cho Boss Room ──
        GameObject bossBounds = new GameObject("BossMap_Bounds");
        bossBounds.transform.parent = zone3.transform;
        bossBounds.transform.position = new Vector3(bossX + 15f, basePos.y + 5f, 0f);
        PolygonCollider2D polyBounds = bossBounds.AddComponent<PolygonCollider2D>();
        polyBounds.isTrigger = true;
        polyBounds.points = new Vector2[]
        {
            new Vector2(-20f, -10f), new Vector2(20f, -10f),
            new Vector2(20f, 15f), new Vector2(-20f, 15f)
        };

        // Select root
        Selection.activeGameObject = root;

        Debug.Log("<color=cyan>═══ ĐÃ TẠO MAP AUTUMN ĐẦY ĐỦ ═══</color>\n" +
            "• Zone 1: Rừng Thu (5 Mushroom + 2 StealthGoblin + Wind Zone)\n" +
            "• Zone 2: Hang Tối (3 SwiftBat + 3 Mushroom + 3 FallingPlatform + Guardian Mini-Boss)\n" +
            "• Zone 3: Phòng Boss (Shadow Dragon + Exit)\n" +
            "<color=yellow>⚠️ Nhớ kéo thả Destination cho các Portal và setup Camera Bounds!</color>");
    }

    // ════════════════════════════════════════════════════════════════
    //  PARALLAX BACKGROUND SETUP
    // ════════════════════════════════════════════════════════════════

    private void SetupParallaxBackground(bool isCave)
    {
        string folder = isCave
            ? "Assets/Sprites/MoiTruong/Thu/Background_Cave"
            : "Assets/Sprites/MoiTruong/Thu/Background_Outdoor";

        string rootName = isCave ? "BG_Cave" : "BG_Outdoor";

        // Tìm tất cả PNG trong thư mục
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        if (guids.Length == 0)
        {
            Debug.LogError($"Không tìm thấy sprite nào trong {folder}! Kiểm tra lại thư mục nhé.");
            return;
        }

        // Root object cho background
        GameObject bgRoot = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(bgRoot, "Create Parallax BG");

        // Sắp xếp theo tên để đúng thứ tự layer
        System.Collections.Generic.List<string> paths = new System.Collections.Generic.List<string>();
        foreach (string guid in guids)
        {
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }
        paths.Sort();

        float parallaxStep = 0.15f; // Mỗi layer tăng parallax 0.15
        int sortOrder = -100;

        for (int i = 0; i < paths.Count; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
            if (sprite == null) continue;

            GameObject layer = new GameObject($"Layer_{i}_{sprite.name}");
            layer.transform.parent = bgRoot.transform;

            SpriteRenderer sr = layer.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortOrder + i;

            // Gắn parallax
            ParallaxBackground parallax = layer.AddComponent<ParallaxBackground>();
            parallax.parallaxMultiplier = parallaxStep * i; // Layer 0 đứng yên, layer cuối gần camera nhất

            // Scale để cover full camera view
            layer.transform.localScale = new Vector3(3f, 3f, 1f);
        }

        Selection.activeGameObject = bgRoot;
        Debug.Log($"<color=green>Đã tạo Parallax Background [{rootName}] với {paths.Count} layers!</color>");
    }

    // ════════════════════════════════════════════════════════════════
    //  SHADOW DRAGON BOSS
    // ════════════════════════════════════════════════════════════════

    private void SpawnShadowDragonBoss()
    {
        Vector3 spawnPos = GetSpawnPosition(20f);

        // Tìm sprite Dragon
        string[] guids = AssetDatabase.FindAssets("Idle_Right t:Texture2D",
            new[] { "Assets/Sprites/NhanVat/Shadow_Demon_Dragon" });

        GameObject boss = new GameObject("BOSS_Shadow_Dragon");
        boss.transform.position = spawnPos;
        boss.transform.localScale = new Vector3(3f, 3f, 1f);

        // Gắn SpriteRenderer
        SpriteRenderer sr = boss.AddComponent<SpriteRenderer>();
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) sr.sprite = sprite;
        }
        sr.sortingOrder = 50;
        sr.color = new Color(0.6f, 0.2f, 0.8f); // Tím tối đặc trưng

        // Physics
        Rigidbody2D rb = boss.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        boss.AddComponent<BoxCollider2D>();

        // Health
        Health h = boss.AddComponent<Health>();
        h.maxHealth = 200;

        // Configure AI
        ConfigureShadowDragonBoss(boss);

        // Fix layer
        FixEnemyLayer(boss);

        Undo.RegisterCreatedObjectUndo(boss, "Spawn Shadow Dragon Boss");
        Selection.activeGameObject = boss;

        Debug.Log($"<color=magenta>🐉 Đã spawn Shadow Dragon Boss tại {spawnPos}!</color>");
    }

    private void ConfigureShadowDragonBoss(GameObject boss)
    {
        // Xóa script Boss cũ nếu có
        BossController oldBoss = boss.GetComponent<BossController>();
        if (oldBoss != null) DestroyImmediate(oldBoss);
        AutumnBoss oldAutumn = boss.GetComponent<AutumnBoss>();
        if (oldAutumn != null) DestroyImmediate(oldAutumn);

        ShadowDragonBoss dragon = boss.GetComponent<ShadowDragonBoss>();
        if (dragon == null) dragon = boss.AddComponent<ShadowDragonBoss>();

        dragon.bossName = "Shadow Demon Dragon";
        dragon.stanceDrop = StanceManager.StanceType.Autumn;
        dragon.aggroRange = 15f;
        dragon.attackRange = 2.5f;
        dragon.moveSpeed = 3f;
        dragon.attackDamage = 20;
        dragon.attackCooldown = 1.5f;
        dragon.projectileSpeed = 8f;
        dragon.projectileDamage = 10;
        dragon.breathCount = 5;
        dragon.breathSpreadAngle = 30f;
        dragon.maxSummons = 2;

        // Tìm và gắn audio clips
        TryLoadAudio(dragon);

        // Tạo FirePoint
        Transform existingFP = boss.transform.Find("FirePoint");
        if (existingFP == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.parent = boss.transform;
            fp.transform.localPosition = new Vector3(1f, 0.5f, 0f);
            dragon.firePoint = fp.transform;
        }
        else
        {
            dragon.firePoint = existingFP;
        }

        Debug.Log($"<color=green>Đã cấu hình ShadowDragonBoss cho {boss.name}. " +
            $"Boss Thu sẽ unlock Autumn Stance khi bị hạ.</color>");
    }

    private void TryLoadAudio(ShadowDragonBoss dragon)
    {
        string audioPath = "Assets/Sprites/NhanVat/Shadow_Demon_Dragon/Audio";
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { audioPath });

        foreach (string guid in audioGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            string clipName = clip.name.ToLowerInvariant();
            if (clipName.Contains("attack")) dragon.attackSFX = clip;
            else if (clipName.Contains("hit")) dragon.hitSFX = clip;
            else if (clipName.Contains("death") && !clipName.Contains("falling")) dragon.deathSFX = clip;
            else if (clipName.Contains("idle")) dragon.idleSFX = clip;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  AUTUMN GUARDIAN (Mini-Boss)
    // ════════════════════════════════════════════════════════════════

    private void SetupAutumnGuardian()
    {
        GameObject boss = Selection.activeGameObject;
        if (boss == null)
        {
            Debug.LogError("Vui lòng click chọn Boss trong Hierarchy trước!");
            return;
        }

        if (boss.GetComponent<Rigidbody2D>() == null) boss.AddComponent<Rigidbody2D>();
        if (boss.GetComponent<BoxCollider2D>() == null) boss.AddComponent<BoxCollider2D>();
        if (boss.GetComponent<Health>() == null) boss.AddComponent<Health>();

        ConfigureAutumnGuardian(boss);
    }

    private void ConfigureAutumnGuardian(GameObject boss)
    {
        BossController oldBoss = boss.GetComponent<BossController>();
        if (oldBoss != null) DestroyImmediate(oldBoss);

        AutumnBoss autumnBoss = boss.GetComponent<AutumnBoss>();
        if (autumnBoss == null) autumnBoss = boss.AddComponent<AutumnBoss>();

        autumnBoss.bossName = "Autumn Guardian";
        autumnBoss.stanceDrop = StanceManager.StanceType.None; // Mini-boss không rớt stance
        autumnBoss.aggroRange = Mathf.Max(autumnBoss.aggroRange, 12f);
        autumnBoss.attackRange = Mathf.Max(autumnBoss.attackRange, 2f);
        autumnBoss.moveSpeed = Mathf.Max(autumnBoss.moveSpeed, 2f);

        Health h = boss.GetComponent<Health>();
        if (h != null && h.maxHealth <= 0) h.maxHealth = 100;

        Debug.Log($"<color=green>Đã gắn AutumnBoss (Mini-Boss Guardian) cho {boss.name}.</color>");
    }

    // ════════════════════════════════════════════════════════════════
    //  SHADOW DRAGON BOSS SETUP (from Selection)
    // ════════════════════════════════════════════════════════════════

    private void SetupShadowDragonBoss()
    {
        GameObject boss = Selection.activeGameObject;
        if (boss == null)
        {
            Debug.LogError("Vui lòng click chọn Boss trong Hierarchy trước!");
            return;
        }

        if (boss.GetComponent<Rigidbody2D>() == null) boss.AddComponent<Rigidbody2D>();
        if (boss.GetComponent<BoxCollider2D>() == null) boss.AddComponent<BoxCollider2D>();
        if (boss.GetComponent<Health>() == null)
        {
            Health h = boss.AddComponent<Health>();
            h.maxHealth = 200;
        }

        ConfigureShadowDragonBoss(boss);
    }

    // ════════════════════════════════════════════════════════════════
    //  MUSHROOM ENEMY
    // ════════════════════════════════════════════════════════════════

    private void SpawnMushroomEnemy()
    {
        string[] guids = AssetDatabase.FindAssets("Mushroom_Enemy t:Prefab");
        if (guids.Length == 0)
        {
            Debug.LogError("Không tìm thấy Mushroom_Enemy.prefab! Kiểm tra thư mục Assets/Prefab nhé.");
            return;
        }
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        Vector3 spawnPos = GetSpawnPosition(6f);
        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        enemy.transform.position = spawnPos;

        FixEnemySetup(enemy);
        FixEnemyLayer(enemy);

        Health h = enemy.GetComponent<Health>();
        if (h != null && h.maxHealth <= 0) h.maxHealth = 100;

        Undo.RegisterCreatedObjectUndo(enemy, "Spawn Mushroom");
        Selection.activeGameObject = enemy;
        Debug.Log($"<color=green>Đã spawn [{enemy.name}] vào vị trí {spawnPos}!</color>");
    }

    // ════════════════════════════════════════════════════════════════
    //  PLAYER SETUP
    // ════════════════════════════════════════════════════════════════

    private void SetupPlayer()
    {
        GameObject player = Selection.activeGameObject;

        if (player == null)
        {
            Debug.LogError("Vui lòng click chọn Player trong Hierarchy trước!");
            return;
        }

        // Các Script cũ
        if (player.GetComponent<Health>() == null) player.AddComponent<Health>();
        if (player.GetComponent<Combat>() == null) player.AddComponent<Combat>();

        // Các Script Core mới
        if (player.GetComponent<StanceManager>() == null) player.AddComponent<StanceManager>();
        if (player.GetComponent<WeaponSystem>() == null) player.AddComponent<WeaponSystem>();
        if (player.GetComponent<BuffReceiver>() == null) player.AddComponent<BuffReceiver>();

        // Tìm và gắn sprite Archer Idle
        string[] idleGuids = AssetDatabase.FindAssets("Archer idle Sheet t:Texture2D",
            new[] { "Assets/Sprites/NhanVat/archer pack Rpg Miniatures" });
        if (idleGuids.Length > 0)
        {
            string idlePath = AssetDatabase.GUIDToAssetPath(idleGuids[0]);
            Sprite idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(idlePath);

            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr == null) sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && idleSprite != null)
            {
                sr.sprite = idleSprite;
                Debug.Log($"<color=cyan>Đã gắn sprite Archer (Idle) cho {player.name}!</color>");
            }
        }

        Debug.Log($"<color=green>Đã gắn thành công các Script cho Player Archer {player.name}!</color>");
    }

    // ════════════════════════════════════════════════════════════════
    //  ENEMY TYPES
    // ════════════════════════════════════════════════════════════════

    private void SetupSlime()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogError("Vui lòng click chọn Quái trong Hierarchy trước!");
            return;
        }

        if (obj.GetComponent<Rigidbody2D>() == null) obj.AddComponent<Rigidbody2D>();
        if (obj.GetComponent<BoxCollider2D>() == null) obj.AddComponent<BoxCollider2D>();
        if (obj.GetComponent<Health>() == null) obj.AddComponent<Health>();

        if (obj.GetComponent<GoblinEnemy>() == null)
        {
            obj.AddComponent<GoblinEnemy>();
            Debug.Log($"<color=green>Đã gắn GoblinEnemy cho {obj.name}!</color>");
        }
    }

    private void SetupStealthEnemy()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogError("Vui lòng click chọn Quái trong Hierarchy trước!");
            return;
        }

        if (obj.GetComponent<Rigidbody2D>() == null) obj.AddComponent<Rigidbody2D>();
        if (obj.GetComponent<BoxCollider2D>() == null) obj.AddComponent<BoxCollider2D>();
        if (obj.GetComponent<Health>() == null) obj.AddComponent<Health>();

        // Xóa script cũ
        if (obj.GetComponent<GoblinEnemy>() != null) DestroyImmediate(obj.GetComponent<GoblinEnemy>());
        if (obj.GetComponent<SwiftBat>() != null) DestroyImmediate(obj.GetComponent<SwiftBat>());

        if (obj.GetComponent<StealthGoblin>() == null)
        {
            obj.AddComponent<StealthGoblin>();
            Debug.Log($"<color=magenta>Đã gắn StealthGoblin (SÁT THỦ TÀNG HÌNH) cho {obj.name}!</color>");
        }
    }

    private void SetupSwiftEnemy()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogError("Vui lòng click chọn Quái trong Hierarchy trước!");
            return;
        }

        if (obj.GetComponent<Rigidbody2D>() == null) obj.AddComponent<Rigidbody2D>();
        if (obj.GetComponent<BoxCollider2D>() == null) obj.AddComponent<BoxCollider2D>();
        if (obj.GetComponent<Health>() == null) obj.AddComponent<Health>();

        // Xóa script cũ
        if (obj.GetComponent<GoblinEnemy>() != null) DestroyImmediate(obj.GetComponent<GoblinEnemy>());
        if (obj.GetComponent<StealthGoblin>() != null) DestroyImmediate(obj.GetComponent<StealthGoblin>());

        if (obj.GetComponent<SwiftBat>() == null)
        {
            obj.AddComponent<SwiftBat>();
            Debug.Log($"<color=cyan>Đã gắn SwiftBat (QUÁI BAY TỐC ĐỘ CAO) cho {obj.name}!</color>");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  TELEPORT & CAVE ZONE
    // ════════════════════════════════════════════════════════════════

    private void SetupTeleport()
    {
        GameObject door = Selection.activeGameObject;
        if (door == null)
        {
            Debug.LogError("Vui lòng click chọn cửa/cổng trong Hierarchy trước!");
            return;
        }

        if (door.GetComponent<BoxCollider2D>() == null) door.AddComponent<BoxCollider2D>().isTrigger = true;
        if (door.GetComponent<Teleport>() == null)
        {
            door.AddComponent<Teleport>();
            Debug.Log($"<color=green>Đã gắn Teleport cho {door.name}!</color>");
        }
    }

    private void SetupCaveZone()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogError("Vui lòng click chọn khu vực hang trong Hierarchy trước!");
            return;
        }

        BoxCollider2D coll = obj.GetComponent<BoxCollider2D>();
        if (coll == null) coll = obj.AddComponent<BoxCollider2D>();
        coll.isTrigger = true;
        coll.size = new Vector2(60f, 20f); // Vùng bao phủ hang

        if (obj.GetComponent<CaveZone>() == null)
        {
            obj.AddComponent<CaveZone>();
            Debug.Log($"<color=green>Đã gắn CaveZone cho {obj.name}!</color>");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  FIX VISIBILITY
    // ════════════════════════════════════════════════════════════════

    private void FixVisibility()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogError("Vui lòng chọn Quái hoặc Boss trong Hierarchy trước!");
            return;
        }

        // Z = 0
        Vector3 pos = obj.transform.position;
        pos.z = 0f;
        obj.transform.position = pos;

        // SpriteRenderer
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = obj.AddComponent<SpriteRenderer>();
            Debug.Log($"<color=yellow>Đã thêm SpriteRenderer cho {obj.name}.</color>");
        }

        if (sr.sprite == null)
        {
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = Color.red;
            Debug.Log($"<color=cyan>Đã gắn ảnh tạm màu đỏ cho {obj.name}!</color>");
        }

        sr.sortingOrder = 10;

        // Fix Gravity
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        Debug.Log($"<color=green>Đã fix tàng hình cho {obj.name}!</color>");
    }

    // ════════════════════════════════════════════════════════════════
    //  HELPER METHODS
    // ════════════════════════════════════════════════════════════════

    private Vector3 GetSpawnPosition(float offset)
    {
        Vector3 pos = Vector3.zero;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            pos = player.transform.position + new Vector3(offset, 0f, 0f);
        else
        {
            Camera cam = Camera.main;
            if (cam != null) pos = cam.transform.position + new Vector3(3f, 0f, 10f);
        }
        pos.z = 0f;
        return pos;
    }

    private GameObject CreateZone(GameObject parent, string name, Vector3 position)
    {
        GameObject zone = new GameObject(name);
        zone.transform.parent = parent.transform;
        zone.transform.position = position;
        return zone;
    }

    private void SpawnEnemyAtPosition(GameObject prefab, Vector3 pos, string name, Transform parent)
    {
        GameObject e = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        e.transform.position = pos;
        e.transform.parent = parent;
        e.name = name;
        FixEnemySetup(e);
    }

    private GameObject SpawnPlaceholderEnemy(Transform parent, string name, Vector3 pos, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = parent;
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color = color;
        sr.sortingOrder = 50;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        obj.AddComponent<BoxCollider2D>();
        Health h = obj.AddComponent<Health>();
        h.maxHealth = 80;

        FixEnemyLayer(obj);
        return obj;
    }

    private void CreatePortal(Transform parent, string name, Vector3 pos, string spriteName)
    {
        // Tìm sprite hang
        string[] guids = AssetDatabase.FindAssets(spriteName + " t:Sprite");

        GameObject portal = new GameObject(name);
        portal.transform.parent = parent;
        portal.transform.position = pos;

        SpriteRenderer sr = portal.AddComponent<SpriteRenderer>();
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) sr.sprite = sprite;
        }
        sr.sortingOrder = 5;

        BoxCollider2D coll = portal.AddComponent<BoxCollider2D>();
        coll.isTrigger = true;
        coll.size = new Vector2(2f, 3f);

        portal.AddComponent<Teleport>();
    }

    private GameObject CreateFallingPlatform(Transform parent, Vector3 pos, int index)
    {
        GameObject fp = new GameObject($"Falling_Autumn_Platform_{index}");
        fp.transform.parent = parent;
        fp.transform.position = pos;

        SpriteRenderer sr = fp.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color = new Color(0.6f, 0.4f, 0.2f); // Nâu gỗ
        sr.sortingOrder = 5;
        fp.transform.localScale = new Vector3(3f, 0.5f, 1f);

        Rigidbody2D rb = fp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        fp.AddComponent<BoxCollider2D>();

        FallingPlatform falling = fp.AddComponent<FallingPlatform>();
        falling.shakeDelay = 0.5f;
        falling.fallDelay = 1f;
        falling.respawnDelay = 3f;

        // Gán tag Ground nếu có
        try { fp.tag = "Ground"; }
        catch { Debug.LogWarning($"Tag 'Ground' chưa tồn tại. Hãy tạo trong Tags & Layers."); }

        return fp;
    }

    private void FixEnemySetup(GameObject obj)
    {
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 50;
        }

        Vector3 p = obj.transform.position;
        p.z = 0f;
        obj.transform.position = p;

        FixEnemyLayer(obj);

        Health h = obj.GetComponent<Health>();
        if (h != null && h.maxHealth <= 0) h.maxHealth = 100;
    }

    private void FixEnemyLayer(GameObject obj)
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            obj.layer = enemyLayer;
            foreach (Transform t in obj.GetComponentsInChildren<Transform>())
                t.gameObject.layer = enemyLayer;
        }
    }
}
