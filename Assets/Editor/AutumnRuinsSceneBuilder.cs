using System.IO;
using AutumnLevel;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class AutumnRuinsSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/AutumnRuins.unity";
    private const string SpringScenePath = "Assets/Scenes/SpringLeverScenes.unity";
    private const string CraftPixAutumnPath = "Assets/Sprites/MoiTruong/Thu/Background_Outdoor";

    [MenuItem("Tools/Autumn/Create Autumn Ruins Scene")]
    public static void CreateAutumnRuinsScene()
    {
        Directory.CreateDirectory("Assets/Scenes");

        Scene springScene = EditorSceneManager.OpenScene(SpringScenePath, OpenSceneMode.Single);
        GameObject sourcePlayer = GameObject.FindGameObjectWithTag("Player");
        GameObject sourceCamera = Camera.main != null ? Camera.main.gameObject : GameObject.Find("Main Camera");
        GameObject sourceGround = FindSpringGroundObject();

        GameObject playerTemplate = sourcePlayer != null ? Object.Instantiate(sourcePlayer) : null;
        GameObject cameraTemplate = sourceCamera != null ? Object.Instantiate(sourceCamera) : null;
        GameObject groundTemplate = sourceGround != null ? Object.Instantiate(sourceGround) : null;

        if (playerTemplate != null)
        {
            playerTemplate.name = "Player";
            playerTemplate.hideFlags = HideFlags.HideAndDontSave;
        }

        if (cameraTemplate != null)
        {
            cameraTemplate.name = "Main Camera";
            cameraTemplate.hideFlags = HideFlags.HideAndDontSave;
        }

        if (groundTemplate != null)
        {
            groundTemplate.name = "[Ground]_SpringTemplate";
            groundTemplate.hideFlags = HideFlags.HideAndDontSave;
        }

        Scene autumnScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        autumnScene.name = "AutumnRuins";

        GameObject root = new GameObject("AutumnRuins_Level");
        GameObject terrainRoot = new GameObject("Terrain_Autumn_Ruins");
        terrainRoot.transform.SetParent(root.transform);
        GameObject enemyRoot = new GameObject("Enemies_Autumn");
        enemyRoot.transform.SetParent(root.transform);
        GameObject hazardRoot = new GameObject("Hazards_Autumn");
        hazardRoot.transform.SetParent(root.transform);
        GameObject detailRoot = new GameObject("Details_Autumn_Ruins");
        detailRoot.transform.SetParent(root.transform);
        CreateAutumnBackground(root.transform);

        GameObject player = playerTemplate != null ? Object.Instantiate(playerTemplate) : CreateFallbackPlayer();
        player.name = "Player";
        player.hideFlags = HideFlags.None;
        player.transform.position = new Vector3(-13.5f, 0.6f, 0f);
        player.transform.SetParent(root.transform);
        EnsurePlayerForAutumn(player);
        EnsureAutumnArcherVisual(player);

        GameObject camera = cameraTemplate != null ? Object.Instantiate(cameraTemplate) : new GameObject("Main Camera");
        camera.name = "Main Camera";
        camera.hideFlags = HideFlags.None;
        camera.SetActive(true);
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(-8f, 0.4f, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.transform.SetParent(root.transform);
        EnsureGameplayCamera(camera);
        CreateCinemachineRig(root.transform, player.transform, camera);

        bool copiedSpringGround = CreateSpringGroundFromTemplate(terrainRoot.transform, groundTemplate);

        Object.DestroyImmediate(playerTemplate);
        Object.DestroyImmediate(cameraTemplate);
        Object.DestroyImmediate(groundTemplate);

        if (!copiedSpringGround)
        {
            CreateGround(terrainRoot.transform, "Ground_Start", new Vector2(-11f, -3f), new Vector2(12f, 1f));
            CreateGround(terrainRoot.transform, "Ground_Central_Ruins", new Vector2(4f, -3f), new Vector2(16f, 1f));
            CreateGround(terrainRoot.transform, "Ground_Boss_Room", new Vector2(22f, -3f), new Vector2(12f, 1f));
        }
        else
        {
            CreateGround(terrainRoot.transform, "Autumn_Boss_Room_Extension", new Vector2(25f, -3f), new Vector2(10f, 1f));
            CreateGround(terrainRoot.transform, "Autumn_Start_Safety_Floor", new Vector2(-13.5f, -3f), new Vector2(6f, 1f));
            CreateGround(terrainRoot.transform, "Autumn_Spawn_Solid_Platform", new Vector2(-13.5f, -1.35f), new Vector2(7f, 0.65f));
            CreateGround(terrainRoot.transform, "Autumn_Cave_Runout_Floor", new Vector2(36f, -3f), new Vector2(14f, 1f));
        }
        CreateGround(terrainRoot.transform, "Broken_Pillar_01", new Vector2(-2f, -0.5f), new Vector2(3f, 0.4f));
        CreateGround(terrainRoot.transform, "Broken_Pillar_02", new Vector2(5f, 1.2f), new Vector2(3f, 0.4f));
        CreateGround(terrainRoot.transform, "Broken_Pillar_03", new Vector2(11f, 0.2f), new Vector2(4f, 0.4f));
        CreateAutumnRuinSetDressing(detailRoot.transform);
        CreateAutumnCaveSection(detailRoot.transform);
        CreateFallingPlatform(hazardRoot.transform, "Falling_Autumn_Platform_01", new Vector2(8f, -0.8f), new Vector2(3f, 0.35f));
        CreateFallingPlatform(hazardRoot.transform, "Falling_Autumn_Platform_02", new Vector2(14f, 1.4f), new Vector2(3f, 0.35f));
        CreateWindZone(hazardRoot.transform, "Autumn_Wind_Gust_Right", new Vector2(6f, -0.5f), new Vector2(7f, 4f), Vector2.right);
        CreateWindZone(hazardRoot.transform, "Autumn_Wind_Gust_Left", new Vector2(16f, 0.5f), new Vector2(5f, 4f), Vector2.left);
        CreateSpikeTrap(hazardRoot.transform, "Autumn_Thorn_Spikes_Start", new Vector2(-6.2f, -2.3f), new Vector2(2.6f, 0.35f));
        CreateSpikeTrap(hazardRoot.transform, "Autumn_Thorn_Spikes_Ruins", new Vector2(11.7f, -2.3f), new Vector2(2.8f, 0.35f));
        CreateSpikeTrap(hazardRoot.transform, "Autumn_Thorn_Spikes_Cave", new Vector2(32.5f, -2.3f), new Vector2(3.2f, 0.35f));
        CreateWindZone(hazardRoot.transform, "Autumn_Cave_Wind_Gust", new Vector2(34f, -0.2f), new Vector2(5.5f, 3.6f), Vector2.left);
        CreateAutumnLightingAndAtmosphere(root.transform);

        GameObject mushroomPrefab = FindPrefab("Mushroom_Enemy");
        CreateStealthEnemy(enemyRoot.transform, mushroomPrefab, "StealthGoblin_Autumn_01", new Vector3(-1f, -2.2f, 0f));
        CreateStealthEnemy(enemyRoot.transform, mushroomPrefab, "StealthGoblin_Autumn_02", new Vector3(7f, -2.2f, 0f));
        CreateSwiftBat(enemyRoot.transform, mushroomPrefab, "SwiftBat_Autumn_01", new Vector3(4f, 2.5f, 0f));
        CreateSwiftBat(enemyRoot.transform, mushroomPrefab, "SwiftBat_Autumn_02", new Vector3(13f, 3f, 0f));
        CreateAutumnBoss(enemyRoot.transform, mushroomPrefab, new Vector3(24f, -1.5f, 0f));

        CreateLevelExit(root.transform, new Vector3(29f, -1.7f, 0f));

        EditorSceneManager.SaveScene(autumnScene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>Created Autumn Ruins scene at Assets/Scenes/AutumnRuins.unity. Player uses bow by LevelFlowManager.</color>");
    }

    private static void EnsurePlayerForAutumn(GameObject player)
    {
        player.tag = "Player";
        if (player.GetComponent<Health>() == null) player.AddComponent<Health>();
        if (player.GetComponent<Combat>() == null) player.AddComponent<Combat>();
        if (player.GetComponent<StanceManager>() == null) player.AddComponent<StanceManager>();
        if (player.GetComponent<WeaponSystem>() == null) player.AddComponent<WeaponSystem>();
        if (player.GetComponent<BuffReceiver>() == null) player.AddComponent<BuffReceiver>();

        WeaponSystem weaponSystem = player.GetComponent<WeaponSystem>();
        weaponSystem.ForcePrimaryWeapon(WeaponSystem.WeaponType.Bow, "Autumn Bow", 8);

        Combat combat = player.GetComponent<Combat>();
        if (combat.player == null) combat.player = player.GetComponent<Player>();
        if (combat.attackDamage <= 0) combat.attackDamage = 4;
        if (combat.swordAttackRadius <= 0f) combat.swordAttackRadius = 1f;
        combat.bowAttackRadius = Mathf.Max(combat.bowAttackRadius, 6f);
        combat.bowCooldown = Mathf.Max(combat.bowCooldown, 1.1f);
    }

    private static void EnsureAutumnArcherVisual(GameObject player)
    {
        Sprite archerSprite = LoadSceneSprite("Assets/Sprites/NhanVat/archer pack Rpg Miniatures/Archer base sprite.png", 32f);
        SpriteRenderer playerRenderer = player.GetComponentInChildren<SpriteRenderer>();
        if (playerRenderer != null && archerSprite != null)
        {
            playerRenderer.sprite = archerSprite;
            playerRenderer.color = Color.white;
            playerRenderer.sortingOrder = 20;
        }

        Transform oldBow = player.transform.Find("Autumn_Bow_Visual");
        if (oldBow != null) Object.DestroyImmediate(oldBow.gameObject);

        GameObject bow = new GameObject("Autumn_Bow_Visual");
        bow.transform.SetParent(player.transform);
        bow.transform.localPosition = new Vector3(0.45f, 0.1f, 0f);
        bow.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        bow.transform.localScale = Vector3.one;

        GameObject grip = CreateDetailBox(bow.transform, "Bow_Grip", Vector2.zero, new Vector2(0.12f, 0.95f), new Color(0.38f, 0.18f, 0.06f), 31, 0f);
        grip.transform.localPosition = Vector3.zero;
        GameObject bowString = CreateDetailBox(bow.transform, "Bow_String", Vector2.zero, new Vector2(0.035f, 1.05f), new Color(0.95f, 0.87f, 0.62f), 32, 0f);
        bowString.transform.localPosition = new Vector3(0.18f, 0f, 0f);

        Transform firePoint = player.transform.Find("Autumn_Bow_FirePoint");
        if (firePoint == null)
        {
            GameObject firePointObject = new GameObject("Autumn_Bow_FirePoint");
            firePointObject.transform.SetParent(player.transform);
            firePointObject.transform.localPosition = new Vector3(0.85f, 0.1f, 0f);
            firePoint = firePointObject.transform;
        }

        Combat combat = player.GetComponent<Combat>();
        if (combat != null)
            combat.bowFirePoint = firePoint;
    }

    private static void EnsureGameplayCamera(GameObject cameraObject)
    {
        Camera camera = cameraObject.GetComponent<Camera>();
        if (camera == null) camera = cameraObject.AddComponent<Camera>();

        camera.enabled = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.2f, 0.15f, 0.08f);
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.depth = -1f;
        camera.targetDisplay = 0;
        camera.cullingMask = ~0;

        AudioListener audioListener = cameraObject.GetComponent<AudioListener>();
        if (audioListener == null) audioListener = cameraObject.AddComponent<AudioListener>();
        audioListener.enabled = true;
    }

    private static void CreateCinemachineRig(Transform root, Transform player, GameObject mainCamera)
    {
        if (mainCamera.GetComponent<CinemachineBrain>() == null)
            mainCamera.AddComponent<CinemachineBrain>();

        GameObject bounds = new GameObject("Autumn_CameraBounds");
        bounds.transform.SetParent(root);
        bounds.transform.position = new Vector3(16f, 0f, 0f);
        PolygonCollider2D polygon = bounds.AddComponent<PolygonCollider2D>();
        polygon.isTrigger = true;
        polygon.points = new Vector2[]
        {
            new Vector2(-36f, -7f),
            new Vector2(40f, -7f),
            new Vector2(40f, 7f),
            new Vector2(-36f, 7f)
        };

        GameObject vcamObject = new GameObject("CM vcam - Autumn Follow");
        vcamObject.transform.SetParent(root);
        vcamObject.transform.position = new Vector3(-8f, 0.4f, -10f);

        CinemachineVirtualCamera vcam = vcamObject.AddComponent<CinemachineVirtualCamera>();
        vcam.Follow = player;
        vcam.LookAt = player;
        vcam.Priority = 20;
        vcam.m_Lens.OrthographicSize = 5.4f;

        CinemachineFramingTransposer transposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_DeadZoneWidth = 0.18f;
        transposer.m_DeadZoneHeight = 0.16f;
        transposer.m_SoftZoneWidth = 0.75f;
        transposer.m_SoftZoneHeight = 0.65f;
        transposer.m_LookaheadTime = 0.18f;
        transposer.m_XDamping = 0.8f;
        transposer.m_YDamping = 1.1f;

        CinemachineConfiner2D confiner = vcamObject.AddComponent<CinemachineConfiner2D>();
        confiner.m_BoundingShape2D = polygon;
        confiner.InvalidateCache();
    }

    private static GameObject CreateFallbackPlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");
        player.AddComponent<Rigidbody2D>().freezeRotation = true;
        player.AddComponent<CapsuleCollider2D>();
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = GetBuiltinSprite();
        sr.color = new Color(0.9f, 0.85f, 0.55f);
        return player;
    }

    private static GameObject FindSpringGroundObject()
    {
        GameObject namedGround = GameObject.Find("[Ground]");
        if (namedGround != null)
            return namedGround;

        TilemapCollider2D tilemapCollider = Object.FindObjectOfType<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            Transform current = tilemapCollider.transform;
            while (current.parent != null && current.parent.GetComponent<Grid>() != null)
                current = current.parent;
            return current.gameObject;
        }

        Tilemap tilemap = Object.FindObjectOfType<Tilemap>();
        return tilemap != null ? tilemap.gameObject : null;
    }

    private static bool CreateSpringGroundFromTemplate(Transform terrainRoot, GameObject groundTemplate)
    {
        if (groundTemplate == null)
            return false;

        GameObject ground = Object.Instantiate(groundTemplate);
        ground.name = "[Ground]_From_Spring_Tilemap";
        ground.transform.SetParent(terrainRoot);
        ground.transform.position = Vector3.zero;
        ground.transform.rotation = Quaternion.identity;
        ground.transform.localScale = Vector3.one;
        ResetObjectTreeForScene(ground);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 0;

        foreach (Transform child in ground.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = groundLayer;

        foreach (TilemapRenderer renderer in ground.GetComponentsInChildren<TilemapRenderer>(true))
        {
            renderer.sortingOrder = 0;
        }

        foreach (Tilemap tilemap in ground.GetComponentsInChildren<Tilemap>(true))
        {
            tilemap.color = new Color(0.86f, 0.72f, 0.55f, 1f);
        }

        foreach (TilemapCollider2D collider in ground.GetComponentsInChildren<TilemapCollider2D>(true))
        {
            collider.enabled = true;
            collider.isTrigger = false;
            collider.usedByComposite = true;
        }

        foreach (CompositeCollider2D composite in ground.GetComponentsInChildren<CompositeCollider2D>(true))
        {
            composite.enabled = true;
            composite.isTrigger = false;
        }

        foreach (Rigidbody2D rb in ground.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.bodyType = RigidbodyType2D.Static;
            rb.simulated = true;
        }

        Debug.Log("[AutumnRuins] Copied Spring tilemap ground into AutumnRuins.");
        return true;
    }

    private static void ResetObjectTreeForScene(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.hideFlags = HideFlags.None;
            child.gameObject.SetActive(true);
        }
        root.hideFlags = HideFlags.None;
        root.SetActive(true);
    }

    private static GameObject CreateGround(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject obj = CreateColoredBox(name, position, size, new Color(0.45f, 0.28f, 0.12f));
        obj.layer = LayerMask.NameToLayer("Ground");
        obj.transform.SetParent(parent);
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 0;
        return obj;
    }

    private static void CreateAutumnRuinSetDressing(Transform parent)
    {
        CreateDetailBox(parent, "Foreground_Branch_Left", new Vector2(-14.5f, 2.7f), new Vector2(2.8f, 0.35f), new Color(0.12f, 0.07f, 0.04f, 0.9f), 85, -18f);
        CreateDetailBox(parent, "Foreground_Branch_Right", new Vector2(19.5f, 3.1f), new Vector2(3.4f, 0.35f), new Color(0.12f, 0.07f, 0.04f, 0.9f), 85, 14f);

        CreateDetailBox(parent, "Ruin_Column_Start_A", new Vector2(-6.2f, -1.1f), new Vector2(0.7f, 3.6f), new Color(0.33f, 0.23f, 0.18f), 8, 0f);
        CreateDetailBox(parent, "Ruin_Column_Start_Broken", new Vector2(-4.9f, 0.1f), new Vector2(0.65f, 2.1f), new Color(0.27f, 0.18f, 0.14f), 8, -7f);
        CreateDetailBox(parent, "Ruin_Arch_Central_Left", new Vector2(2.8f, -0.5f), new Vector2(0.8f, 3.2f), new Color(0.31f, 0.2f, 0.16f), 8, 0f);
        CreateDetailBox(parent, "Ruin_Arch_Central_Right", new Vector2(6.4f, -0.5f), new Vector2(0.8f, 3.2f), new Color(0.31f, 0.2f, 0.16f), 8, 0f);
        CreateDetailBox(parent, "Ruin_Arch_Central_Top", new Vector2(4.6f, 1.2f), new Vector2(4.4f, 0.55f), new Color(0.28f, 0.18f, 0.14f), 8, 0f);
        CreateDetailBox(parent, "Boss_Room_Back_Wall_Crack", new Vector2(21.2f, -0.3f), new Vector2(0.22f, 3.2f), new Color(0.08f, 0.05f, 0.04f, 0.8f), 9, -12f);

        CreateLeafCluster(parent, "LeafPile_Start", new Vector2(-12.2f, -2.35f), 7);
        CreateLeafCluster(parent, "LeafPile_Central", new Vector2(1.2f, -2.35f), 10);
        CreateLeafCluster(parent, "LeafPile_Boss", new Vector2(21.5f, -2.35f), 8);
    }

    private static void CreateAutumnCaveSection(Transform parent)
    {
        GameObject caveRoot = new GameObject("Autumn_End_Cave_Details");
        caveRoot.transform.SetParent(parent);

        CreateDetailBox(caveRoot.transform, "Cave_Back_Shadow", new Vector2(36f, -0.6f), new Vector2(15f, 5.4f), new Color(0.08f, 0.05f, 0.04f, 0.88f), -5, 0f);
        CreateDetailBox(caveRoot.transform, "Cave_Ceiling", new Vector2(36f, 2.55f), new Vector2(15.5f, 1.2f), new Color(0.16f, 0.09f, 0.06f), 7, 0f);
        CreateDetailBox(caveRoot.transform, "Cave_Left_Wall", new Vector2(29.4f, -0.3f), new Vector2(1.2f, 5.4f), new Color(0.14f, 0.08f, 0.05f), 7, 0f);
        CreateDetailBox(caveRoot.transform, "Cave_Right_Wall", new Vector2(42.7f, -0.3f), new Vector2(1.2f, 5.4f), new Color(0.14f, 0.08f, 0.05f), 7, 0f);

        CreateDetailBox(caveRoot.transform, "Stalactite_01", new Vector2(31.5f, 1.7f), new Vector2(0.55f, 1.4f), new Color(0.22f, 0.12f, 0.08f), 9, 0f);
        CreateDetailBox(caveRoot.transform, "Stalactite_02", new Vector2(35.2f, 1.95f), new Vector2(0.45f, 1.1f), new Color(0.2f, 0.1f, 0.07f), 9, 0f);
        CreateDetailBox(caveRoot.transform, "Stalactite_03", new Vector2(39.2f, 1.65f), new Vector2(0.65f, 1.55f), new Color(0.22f, 0.12f, 0.08f), 9, 0f);
        CreateDetailBox(caveRoot.transform, "Cave_Light_Beam", new Vector2(33.5f, 0.4f), new Vector2(5.5f, 0.35f), new Color(1f, 0.65f, 0.22f, 0.16f), 72, -20f);
        CreateLeafCluster(caveRoot.transform, "LeafPile_Cave_Entrance", new Vector2(30.8f, -2.35f), 7);
    }

    private static void CreateLeafCluster(Transform parent, string name, Vector2 origin, int count)
    {
        GameObject cluster = new GameObject(name);
        cluster.transform.SetParent(parent);
        for (int i = 0; i < count; i++)
        {
            float x = origin.x + Random.Range(-1.3f, 1.3f);
            float y = origin.y + Random.Range(-0.08f, 0.16f);
            Color color = i % 3 == 0
                ? new Color(0.93f, 0.48f, 0.13f)
                : i % 3 == 1
                    ? new Color(0.78f, 0.23f, 0.08f)
                    : new Color(0.96f, 0.68f, 0.18f);
            GameObject leaf = CreateDetailBox(cluster.transform, "Leaf_" + (i + 1), new Vector2(x, y), new Vector2(0.28f, 0.12f), color, 18, Random.Range(-35f, 35f));
            leaf.transform.localScale = new Vector3(leaf.transform.localScale.x * Random.Range(0.75f, 1.25f), leaf.transform.localScale.y, 1f);
        }
    }

    private static void CreateAutumnLightingAndAtmosphere(Transform root)
    {
        GameObject atmosphereRoot = new GameObject("Autumn_Lighting_Atmosphere");
        atmosphereRoot.transform.SetParent(root);

        CreateDetailBox(atmosphereRoot.transform, "Warm_Color_Grade_Overlay", new Vector2(14f, 1f), new Vector2(56f, 16f), new Color(1f, 0.52f, 0.16f, 0.08f), 95, 0f);
        CreateDetailBox(atmosphereRoot.transform, "Sun_Ray_Wide_Left", new Vector2(-5f, 3.9f), new Vector2(20f, 0.9f), new Color(1f, 0.78f, 0.36f, 0.16f), 70, -18f);
        CreateDetailBox(atmosphereRoot.transform, "Sun_Ray_Thin_Mid", new Vector2(9f, 3.1f), new Vector2(16f, 0.45f), new Color(1f, 0.68f, 0.22f, 0.12f), 70, -18f);
        CreateDetailBox(atmosphereRoot.transform, "Low_Mist_Foreground", new Vector2(6f, -1.55f), new Vector2(42f, 0.75f), new Color(0.92f, 0.65f, 0.36f, 0.14f), 75, 0f);

        GameObject particles = new GameObject("FX_Autumn_Falling_Leaves");
        particles.transform.SetParent(atmosphereRoot.transform);
        particles.transform.position = new Vector3(7f, 5.5f, 0f);
        ParticleSystem ps = particles.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.duration = 12f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.54f, 0.1f, 0.65f), new Color(0.65f, 0.18f, 0.05f, 0.45f));
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 12f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(52f, 0.5f, 1f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.75f, -0.25f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.65f, -0.25f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.22f;
        noise.frequency = 0.45f;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 90;
    }

    private static GameObject CreateDetailBox(Transform parent, string name, Vector2 position, Vector2 size, Color color, int sortingOrder, float rotationZ)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        obj.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetBuiltinSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return obj;
    }

    private static void CreateAutumnBackground(Transform levelRoot)
    {
        GameObject backgroundRoot = new GameObject("[BackGround]");
        backgroundRoot.transform.SetParent(levelRoot);
        backgroundRoot.transform.position = Vector3.zero;

        CreateAutumnSkyBackdrop(backgroundRoot.transform);

        if (CreateCraftPixAutumnBackground(backgroundRoot.transform))
            return;

        GameObject skyLayer = new GameObject("Autumn_Sky_Layer");
        skyLayer.transform.SetParent(backgroundRoot.transform);
        skyLayer.AddComponent<ParallaxBackground>().parallaxMultiplier = 0.05f;
        for (int i = 0; i < 5; i++)
        {
            CreateBackgroundBox(
                skyLayer.transform,
                "Autumn_Sky_Panel_" + (i + 1),
                new Vector2(-18f + i * 14f, 2f),
                new Vector2(14.5f, 12f),
                new Color(0.42f, 0.24f, 0.16f),
                -60);
        }

        GameObject hazeLayer = new GameObject("Autumn_Mist_Clouds");
        hazeLayer.transform.SetParent(backgroundRoot.transform);
        hazeLayer.AddComponent<ParallaxBackground>().parallaxMultiplier = 0.12f;
        CreateBackgroundBox(hazeLayer.transform, "Amber_Mist_Left", new Vector2(-12f, 1.2f), new Vector2(12f, 1.2f), new Color(0.88f, 0.54f, 0.24f, 0.45f), -50);
        CreateBackgroundBox(hazeLayer.transform, "Amber_Mist_Mid", new Vector2(4f, 2.1f), new Vector2(16f, 1f), new Color(0.95f, 0.64f, 0.28f, 0.38f), -50);
        CreateBackgroundBox(hazeLayer.transform, "Amber_Mist_Right", new Vector2(21f, 1.4f), new Vector2(13f, 1.1f), new Color(0.75f, 0.38f, 0.18f, 0.42f), -50);

        GameObject hillsLayer = new GameObject("Autumn_Distant_Hills");
        hillsLayer.transform.SetParent(backgroundRoot.transform);
        hillsLayer.AddComponent<ParallaxBackground>().parallaxMultiplier = 0.18f;
        CreateBackgroundBox(hillsLayer.transform, "Distant_Hill_Left", new Vector2(-10f, -1.8f), new Vector2(18f, 4f), new Color(0.18f, 0.13f, 0.11f), -40);
        CreateBackgroundBox(hillsLayer.transform, "Distant_Hill_Right", new Vector2(14f, -1.5f), new Vector2(24f, 4.4f), new Color(0.2f, 0.14f, 0.1f), -40);

        GameObject ruinsLayer = new GameObject("Autumn_Ruins_Silhouette");
        ruinsLayer.transform.SetParent(backgroundRoot.transform);
        ruinsLayer.AddComponent<ParallaxBackground>().parallaxMultiplier = 0.28f;
        CreateRuinsSilhouette(ruinsLayer.transform);

        GameObject leavesLayer = new GameObject("Autumn_Falling_Leaves");
        leavesLayer.transform.SetParent(backgroundRoot.transform);
        leavesLayer.AddComponent<ParallaxBackground>().parallaxMultiplier = 0.36f;
        CreateAutumnLeaves(leavesLayer.transform);
    }

    private static bool CreateCraftPixAutumnBackground(Transform backgroundRoot)
    {
        string[] planPaths = GetExistingBackgroundPlans(CraftPixAutumnPath);
        if (planPaths.Length == 0)
            return false;

        Sprite[] sprites = new Sprite[planPaths.Length];
        for (int i = 0; i < planPaths.Length; i++)
        {
            sprites[i] = LoadBackgroundSprite(planPaths[i]);
            if (sprites[i] == null)
                return false;
        }

        float[] parallax = { 0.04f, 0.08f, 0.14f, 0.22f, 0.34f, 0.46f };
        int[] orders = { -70, -60, -50, -40, -30, -20 };

        for (int i = 0; i < sprites.Length; i++)
        {
            GameObject layer = new GameObject("CraftPix_Autumn_Outdoor_Plan_" + (sprites.Length - i));
            layer.transform.SetParent(backgroundRoot);
            layer.AddComponent<ParallaxBackground>().parallaxMultiplier = parallax[Mathf.Min(i, parallax.Length - 1)];

            for (int tile = -3; tile <= 4; tile++)
            {
                GameObject segment = new GameObject("Segment_" + (tile + 4));
                segment.transform.SetParent(layer.transform);
                segment.transform.position = new Vector3(tile * 18f + 11f, 0.2f, 8f);
                segment.transform.localScale = new Vector3(3.4f, 3.4f, 1f);

                SpriteRenderer sr = segment.AddComponent<SpriteRenderer>();
                sr.sprite = sprites[i];
                sr.sortingOrder = orders[Mathf.Min(i, orders.Length - 1)];
            }
        }

        return true;
    }

    private static void CreateAutumnSkyBackdrop(Transform backgroundRoot)
    {
        GameObject skyFill = new GameObject("Autumn_Outdoor_Sky_Fill");
        skyFill.transform.SetParent(backgroundRoot);
        skyFill.AddComponent<ParallaxBackground>().parallaxMultiplier = 0.02f;

        for (int i = 0; i < 11; i++)
        {
            CreateBackgroundBox(
                skyFill.transform,
                "Sky_Fill_Panel_" + (i + 1),
                new Vector2(-48f + i * 14f, 1.6f),
                new Vector2(14.5f, 13f),
                new Color(0.78f, 0.86f, 0.86f),
                -90);
        }
    }

    private static string[] GetExistingBackgroundPlans(string backgroundPath)
    {
        string[] candidates =
        {
            backgroundPath + "/Plan-6.png",
            backgroundPath + "/Plan-5.png",
            backgroundPath + "/Plan-4.png",
            backgroundPath + "/Plan-3.png",
            backgroundPath + "/Plan-2.png",
            backgroundPath + "/Plan-1.png"
        };

        System.Collections.Generic.List<string> existingPaths = new System.Collections.Generic.List<string>();
        foreach (string candidate in candidates)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(candidate) != null)
                existingPaths.Add(candidate);
        }

        return existingPaths.ToArray();
    }

    private static Sprite LoadBackgroundSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static Sprite LoadSceneSprite(string assetPath, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void CreateRuinsSilhouette(Transform parent)
    {
        CreateBackgroundBox(parent, "Ruined_Arch_Left_Pillar", new Vector2(-7f, -0.3f), new Vector2(0.8f, 4.2f), new Color(0.13f, 0.09f, 0.08f), -30);
        CreateBackgroundBox(parent, "Ruined_Arch_Right_Pillar", new Vector2(-3.8f, -0.3f), new Vector2(0.8f, 4.2f), new Color(0.13f, 0.09f, 0.08f), -30);
        CreateBackgroundBox(parent, "Ruined_Arch_Top", new Vector2(-5.4f, 1.8f), new Vector2(4f, 0.6f), new Color(0.13f, 0.09f, 0.08f), -30);
        CreateBackgroundBox(parent, "Broken_Tower_Back", new Vector2(8f, 0.2f), new Vector2(1.6f, 5.2f), new Color(0.11f, 0.08f, 0.08f), -30);
        CreateBackgroundBox(parent, "Broken_Tower_Cap", new Vector2(8.4f, 3.1f), new Vector2(2.2f, 0.6f), new Color(0.11f, 0.08f, 0.08f), -30);
        CreateBackgroundBox(parent, "Far_Collapsed_Wall", new Vector2(18f, -0.8f), new Vector2(7f, 2.4f), new Color(0.14f, 0.09f, 0.07f), -30);
    }

    private static void CreateAutumnLeaves(Transform parent)
    {
        Color[] colors =
        {
            new Color(0.95f, 0.45f, 0.12f),
            new Color(0.85f, 0.26f, 0.09f),
            new Color(0.98f, 0.72f, 0.22f),
            new Color(0.62f, 0.21f, 0.08f)
        };

        Vector2[] positions =
        {
            new Vector2(-14f, 3.4f), new Vector2(-10f, 1.8f), new Vector2(-5f, 4.2f),
            new Vector2(0f, 2.8f), new Vector2(4f, 4.6f), new Vector2(9f, 2.5f),
            new Vector2(13f, 4.1f), new Vector2(18f, 2.1f), new Vector2(23f, 3.5f),
            new Vector2(27f, 1.9f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject leaf = CreateBackgroundBox(parent, "Autumn_Leaf_" + (i + 1), positions[i], new Vector2(0.35f, 0.18f), colors[i % colors.Length], -20);
            leaf.transform.rotation = Quaternion.Euler(0f, 0f, -35f + i * 17f);
        }
    }

    private static GameObject CreateBackgroundBox(Transform parent, string name, Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(position.x, position.y, 8f);
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetBuiltinSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        return obj;
    }

    private static void CreateFallingPlatform(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject obj = CreateGround(parent, name, position, size);
        obj.GetComponent<SpriteRenderer>().color = new Color(0.65f, 0.38f, 0.12f);
        obj.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        FallingPlatform platform = obj.AddComponent<FallingPlatform>();
        platform.shakeDelay = 0.4f;
        platform.fallDelay = 1f;
        platform.respawnDelay = 3f;
    }

    private static void CreateWindZone(Transform parent, string name, Vector2 position, Vector2 size, Vector2 direction)
    {
        GameObject obj = CreateColoredBox(name, position, size, new Color(0.9f, 0.55f, 0.18f, 0.25f));
        obj.transform.SetParent(parent);
        BoxCollider2D collider = obj.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        AutumnWindZone wind = obj.AddComponent<AutumnWindZone>();
        wind.windDirection = direction;
        wind.windForce = 12f;
        wind.isConstant = false;
        wind.gustInterval = 1.8f;
        wind.gustDuration = 0.9f;
    }

    private static void CreateSpikeTrap(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject trap = CreateColoredBox(name, position, size, new Color(0.66f, 0.18f, 0.08f, 0.9f));
        trap.transform.SetParent(parent);
        trap.layer = LayerMask.NameToLayer("Default");

        BoxCollider2D collider = trap.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;

        Spike spike = trap.AddComponent<Spike>();
        spike.damage = 15;

        for (int i = 0; i < 7; i++)
        {
            GameObject thorn = CreateDetailBox(
                trap.transform,
                "Thorn_" + (i + 1),
                Vector2.zero,
                new Vector2(0.18f, 0.55f),
                new Color(0.84f, 0.34f, 0.08f),
                16,
                -25f + i * 8f);
            thorn.transform.localPosition = new Vector3(-0.42f + i * 0.14f, 0.35f, 0f);
        }
    }

    private static GameObject CreateColoredBox(string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetBuiltinSprite();
        sr.color = color;
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        return obj;
    }

    private static void CreateStealthEnemy(Transform parent, GameObject prefab, string name, Vector3 position)
    {
        GameObject enemy = InstantiateEnemy(prefab, name, position, parent);
        RemoveEnemyScripts(enemy);
        StealthGoblin stealth = enemy.AddComponent<StealthGoblin>();
        stealth.moveSpeed = 3.4f;
        stealth.aggroRange = 8f;
        stealth.revealDistance = 4f;
        Tint(enemy, new Color(0.95f, 0.45f, 0.12f));
    }

    private static void CreateSwiftBat(Transform parent, GameObject prefab, string name, Vector3 position)
    {
        GameObject enemy = InstantiateEnemy(prefab, name, position, parent);
        RemoveEnemyScripts(enemy);
        SwiftBat bat = enemy.AddComponent<SwiftBat>();
        bat.moveSpeed = 3.8f;
        bat.aggroRange = 10f;
        bat.dashSpeed = 12f;
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0f;
        Tint(enemy, new Color(0.55f, 0.2f, 0.75f));
    }

    private static void CreateAutumnBoss(Transform parent, GameObject prefab, Vector3 position)
    {
        GameObject boss = InstantiateEnemy(prefab, "BOSS_Autumn_Guardian", position, parent);
        boss.transform.localScale = new Vector3(2.3f, 2.3f, 1f);
        RemoveEnemyScripts(boss);
        AutumnBoss autumnBoss = boss.AddComponent<AutumnBoss>();
        autumnBoss.bossName = "Autumn Guardian";
        autumnBoss.stanceDrop = StanceManager.StanceType.Autumn;
        autumnBoss.maxSummons = 2;
        autumnBoss.aggroRange = 14f;
        autumnBoss.attackRange = 2f;
        autumnBoss.moveSpeed = 2f;
        Health health = boss.GetComponent<Health>();
        if (health != null) health.maxHealth = 180;
        Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0f;
        BossDefeatLevelEnd endLevel = boss.AddComponent<BossDefeatLevelEnd>();
        endLevel.nextSceneName = "MainMenu";
        endLevel.delay = 2.8f;
        Tint(boss, new Color(0.75f, 0.2f, 0.95f));
    }

    private static GameObject InstantiateEnemy(GameObject prefab, string name, Vector3 position, Transform parent)
    {
        GameObject enemy = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : CreateColoredBox(name, position, new Vector2(1f, 1f), Color.red);

        enemy.name = name;
        enemy.transform.position = position;
        enemy.transform.SetParent(parent);
        enemy.layer = LayerMask.NameToLayer("Enemy");
        foreach (Transform child in enemy.GetComponentsInChildren<Transform>())
            child.gameObject.layer = enemy.layer;

        if (enemy.GetComponent<Rigidbody2D>() == null) enemy.AddComponent<Rigidbody2D>();
        if (enemy.GetComponent<Collider2D>() == null) enemy.AddComponent<BoxCollider2D>();
        if (enemy.GetComponent<Health>() == null) enemy.AddComponent<Health>();

        Health health = enemy.GetComponent<Health>();
        if (health.maxHealth <= 0) health.maxHealth = 100;

        return enemy;
    }

    private static void RemoveEnemyScripts(GameObject obj)
    {
        foreach (EnemyBase script in obj.GetComponents<EnemyBase>())
            Object.DestroyImmediate(script);

        BossController bossController = obj.GetComponent<BossController>();
        if (bossController != null) Object.DestroyImmediate(bossController);

        EnemyController enemyController = obj.GetComponent<EnemyController>();
        if (enemyController != null) Object.DestroyImmediate(enemyController);
    }

    private static void CreateLevelExit(Transform parent, Vector3 position)
    {
        GameObject exit = CreateColoredBox("Exit_To_MainMenu", position, new Vector2(1.2f, 2.5f), new Color(0.9f, 0.7f, 0.2f, 0.6f));
        exit.transform.SetParent(parent);
        exit.GetComponent<BoxCollider2D>().isTrigger = true;
        LevelTransition transition = exit.AddComponent<LevelTransition>();
        transition.nextSceneName = "MainMenu";
        transition.requireInput = true;
    }

    private static void Tint(GameObject obj, Color color)
    {
        foreach (SpriteRenderer sr in obj.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.color = color;
            sr.sortingOrder = 10;
        }
    }

    private static GameObject FindPrefab(string prefabName)
    {
        string[] guids = AssetDatabase.FindAssets(prefabName + " t:Prefab");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static Sprite GetBuiltinSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == scenePath)
                return;
        }

        EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(updated, 0);
        updated[updated.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = updated;
    }
}
