using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class AutumnRuinsRedesignTool
{
    private const string ScenePath = "Assets/Scenes/AutumnRuins.unity";
    private const string RootName = "[Autumn_Redesign_Pass]";
    private const string PlatformTileGuid = "addee815ab59d974d9d9f6fd409559cc";
    private const string DragonAnimationFolder = "Assets/Animations/Enemy/ShadowDragonAutumn";
    private const string DragonControllerPath = DragonAnimationFolder + "/ShadowDragonAutumn.controller";
    private const string AutumnPrefabFolder = "Assets/Prefab/Autumn";
    private const string ShadowBallPrefabPath = AutumnPrefabFolder + "/ShadowBall.prefab";
    private const string DragonIconPath =
        "Assets/Sprites/NhanVat/Shadow_Demon_Dragon/Animations/Idle_Left/Idle_left.png";
    private const string VictoryPanelName = "[Autumn_Victory_Panel]";
    private const string QueuedApplyPath = "Temp/AutumnRedesignApply.request";
    private const string QueuedApplyDonePath = "Temp/AutumnRedesignApply.done";
    private const string QueuedValidationPath = "Temp/AutumnRedesignValidation.request";
    private const string QueuedValidationDonePath = "Temp/AutumnRedesignValidation.done";
    private static bool isProcessingQueuedRequest;

    private static readonly string[] BackgroundFolders =
    {
        "Assets/Sprites/MoiTruong/Thu/CraftPix_Autumn_Backgrounds_PNG/background 1",
        "Assets/Sprites/MoiTruong/Thu/CraftPix_Autumn_Backgrounds_PNG/background 2",
        "Assets/Sprites/MoiTruong/Thu/CraftPix_Autumn_Backgrounds_PNG/background 3",
        "Assets/Sprites/MoiTruong/Thu/CraftPix_Autumn_Backgrounds_PNG/background 4"
    };

    static AutumnRuinsRedesignTool()
    {
        EditorApplication.update -= ProcessQueuedRequests;
        EditorApplication.update += ProcessQueuedRequests;
    }

    private static void ProcessQueuedRequests()
    {
        if (isProcessingQueuedRequest)
            return;

        if (File.Exists(QueuedApplyPath))
        {
            isProcessingQueuedRequest = true;
            RunQueuedApplyAfterCompile();
        }
        else if (File.Exists(QueuedValidationPath))
        {
            isProcessingQueuedRequest = true;
            RunQueuedValidationAfterCompile();
        }
    }

    private static void RunQueuedApplyAfterCompile()
    {
        if (!File.Exists(QueuedApplyPath))
            return;

        File.Delete(QueuedApplyPath);
        EditorApplication.delayCall += () =>
        {
            try
            {
                Apply(true);
                File.WriteAllText(QueuedApplyDonePath, DateTime.Now.ToString("O"));
            }
            catch (Exception exception)
            {
                File.WriteAllText(QueuedApplyDonePath, exception.ToString());
                Debug.LogException(exception);
            }
            finally
            {
                isProcessingQueuedRequest = false;
            }
        };
    }

    private static void RunQueuedValidationAfterCompile()
    {
        // Validation is queued separately after the scene apply has completed.
        if (!File.Exists(QueuedValidationPath))
            return;

        File.Delete(QueuedValidationPath);
        EditorApplication.delayCall += () =>
        {
            try
            {
                ValidateAppliedScene();
                CapturePreviews();
                File.WriteAllText(QueuedValidationDonePath, DateTime.Now.ToString("O"));
            }
            catch (Exception exception)
            {
                File.WriteAllText(QueuedValidationDonePath, exception.ToString());
                Debug.LogException(exception);
            }
            finally
            {
                isProcessingQueuedRequest = false;
            }
        };
    }

    [MenuItem("Tools/Autumn/Apply Distinctive Redesign")]
    public static void ApplyFromMenu()
    {
        Apply(false);
    }

    [MenuItem("Tools/Autumn/Fix Grounding, Spike Pits, And Player Scale")]
    public static void FixGroundingSpikePitsAndPlayerScale()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Tilemap ground = FindGroundTilemap();
        Player player = UnityEngine.Object.FindObjectOfType<Player>();
        if (ground == null || player == null)
            throw new InvalidOperationException("Autumn ground or Player is missing.");

        int disabledGhostVisuals = FixStealthEnemyVisualAndGrounding();
        int alignedEnemies = AlignGroundEnemies();
        int clearedGroundTiles = ClearTilesCoveringSpikePits(ground);
        ScalePlayerVisual(player, 2.2f);

        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        ValidateRequestedDetails(ground, player);
        Debug.Log(
            $"[AutumnDetailFix] PASS | alignedEnemies={alignedEnemies} | " +
            $"disabledGhostVisuals={disabledGhostVisuals} | clearedSpikeTiles={clearedGroundTiles} | " +
            "playerVisualScale=2.2");
    }

    [MenuItem("Tools/Autumn/Apply Boss Icon And Victory Presentation")]
    public static void ApplyBossIconAndVictoryPresentation()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject iconObject = GameObject.Find("AvatarImageBoss");
        Image iconImage = iconObject != null ? iconObject.GetComponent<Image>() : null;
        Sprite dragonIcon = LoadSpriteByName(DragonIconPath, "Idle_00");
        if (iconImage == null || dragonIcon == null)
            throw new InvalidOperationException("Boss avatar Image or Shadow Dragon icon sprite is missing.");
        iconImage.sprite = dragonIcon;
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;
        EditorUtility.SetDirty(iconImage);

        UIManager uiManager = UnityEngine.Object.FindObjectOfType<UIManager>(true);
        Canvas canvas = uiManager != null ? uiManager.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            canvas = UnityEngine.Object.FindObjectOfType<Canvas>(true);
        if (canvas == null)
            throw new InvalidOperationException("Autumn UI Canvas is missing.");

        Transform existingPanel = FindTransformByName(VictoryPanelName);
        if (existingPanel != null)
            UnityEngine.Object.DestroyImmediate(existingPanel.gameObject);
        GameObject victoryPanel = CreateVictoryPanel(canvas.transform);

        GameObject boss = GameObject.Find("[BossAutumnShadowDragon]");
        BossDefeatLevelEnd levelEnd = boss != null ? boss.GetComponent<BossDefeatLevelEnd>() : null;
        if (levelEnd == null)
            throw new InvalidOperationException("Autumn Shadow Dragon level-end component is missing.");
        levelEnd.nextSceneName = "MainMenu";
        levelEnd.delay = 1f;
        levelEnd.victoryPanel = victoryPanel;
        levelEnd.victoryDisplayDuration = 3.5f;
        levelEnd.freezeGameDuringVictory = true;
        EditorUtility.SetDirty(levelEnd);

        victoryPanel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        ValidateBossIconAndVictoryPresentation();
    }

    public static void ValidateBossIconAndVictoryPresentation()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject iconObject = GameObject.Find("AvatarImageBoss");
        Image icon = iconObject != null ? iconObject.GetComponent<Image>() : null;
        string iconPath = icon != null && icon.sprite != null
            ? AssetDatabase.GetAssetPath(icon.sprite)
            : string.Empty;
        GameObject boss = GameObject.Find("[BossAutumnShadowDragon]");
        BossDefeatLevelEnd levelEnd = boss != null ? boss.GetComponent<BossDefeatLevelEnd>() : null;
        Transform panel = FindTransformByName(VictoryPanelName);
        if (icon == null || icon.sprite == null || iconPath != DragonIconPath)
            throw new InvalidOperationException("Boss health icon is not using the Shadow Dragon sprite.");
        if (levelEnd == null || levelEnd.victoryPanel == null || panel == null ||
            levelEnd.victoryPanel != panel.gameObject || levelEnd.victoryDisplayDuration < 3f ||
            levelEnd.delay >= 2f || !levelEnd.freezeGameDuringVictory)
            throw new InvalidOperationException("Autumn victory presentation is not configured correctly.");
        if (panel.gameObject.activeSelf)
            throw new InvalidOperationException("Autumn victory panel must be hidden before the boss dies.");
        if (panel.GetComponentsInChildren<TextMeshProUGUI>(true).Length < 2)
            throw new InvalidOperationException("Autumn victory panel text is incomplete.");

        Debug.Log(
            "[AutumnBossPresentationValidation] PASS | icon=ShadowDragon Idle_00 | " +
            "victoryDelay=1.0 | display=3.5 | realtimeWait=True | nextScene=MainMenu");
    }

    private static int FixStealthEnemyVisualAndGrounding()
    {
        int disabledVisuals = 0;
        foreach (StealthEnemy enemy in UnityEngine.Object.FindObjectsOfType<StealthEnemy>(true))
        {
            BoxCollider2D collider = enemy.GetComponent<BoxCollider2D>();
            SpriteRenderer primary = enemy.spriteRenderer != null
                ? enemy.spriteRenderer
                : enemy.GetComponentInChildren<SpriteRenderer>(true);
            if (collider == null || primary == null)
                continue;

            Transform visual = primary.transform;
            float visualBottom = primary.sprite != null
                ? primary.sprite.bounds.min.y * Mathf.Abs(visual.localScale.y)
                : -0.5f * Mathf.Abs(visual.localScale.y);
            float targetCenterY = collider.offset.y - collider.size.y * 0.5f - visualBottom;
            visual.localPosition = new Vector3(0f, targetCenterY, visual.localPosition.z);
            primary.enabled = true;
            EditorUtility.SetDirty(visual);
            EditorUtility.SetDirty(primary);

            foreach (SpriteRenderer renderer in enemy.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == primary || !renderer.enabled)
                    continue;
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
                disabledVisuals++;
            }
        }
        return disabledVisuals;
    }

    private static int AlignGroundEnemies()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
            return 0;

        int aligned = 0;
        int groundMask = 1 << groundLayer;
        foreach (Rigidbody2D body in UnityEngine.Object.FindObjectsOfType<Rigidbody2D>(true))
        {
            GameObject enemyObject = body.gameObject;
            string lowerName = enemyObject.name.ToLowerInvariant();
            if (enemyObject.CompareTag("Player") || lowerName.Contains("bat") ||
                lowerName.Contains("boss") || enemyObject.GetComponentInParent<ShadowDragonBoss>() != null)
                continue;

            bool isGroundEnemy = enemyObject.layer == LayerMask.NameToLayer("Enemy") ||
                                 enemyObject.GetComponent<StealthEnemy>() != null ||
                                 enemyObject.GetComponent<EnemyController>() != null;
            Collider2D collider = enemyObject.GetComponent<Collider2D>();
            if (!isGroundEnemy || collider == null || collider.isTrigger)
                continue;

            Physics2D.SyncTransforms();
            Bounds bounds = collider.bounds;
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.15f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 4f, groundMask);
            if (hit.collider == null)
                continue;

            float deltaY = hit.point.y - bounds.min.y + 0.02f;
            if (deltaY < -0.25f || deltaY > 3.5f)
                continue;

            if (Mathf.Abs(deltaY) > 0.015f)
            {
                body.transform.position += Vector3.up * deltaY;
                body.velocity = Vector2.zero;
                EditorUtility.SetDirty(body.transform);
                aligned++;
            }
        }
        return aligned;
    }

    private static int ClearTilesCoveringSpikePits(Tilemap ground)
    {
        int removed = 0;
        string[] pitNames = { "Spike_01", "Spike_02" };
        foreach (string pitName in pitNames)
        {
            GameObject pit = GameObject.Find(pitName);
            if (pit == null)
                continue;

            foreach (Renderer spikeRenderer in pit.GetComponentsInChildren<Renderer>(true))
            {
                Bounds spikeBounds = spikeRenderer.bounds;
                spikeBounds.Expand(new Vector3(-0.08f, 0.18f, 0f));
                Vector3Int min = ground.WorldToCell(spikeBounds.min);
                Vector3Int max = ground.WorldToCell(spikeBounds.max);
                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, 0);
                        if (!ground.HasTile(cell))
                            continue;

                        Vector3 cellSize = Vector3.Scale(ground.layoutGrid.cellSize, ground.transform.lossyScale);
                        Bounds tileBounds = new Bounds(
                            ground.GetCellCenterWorld(cell),
                            new Vector3(Mathf.Abs(cellSize.x) * 0.9f, Mathf.Abs(cellSize.y) * 0.9f, 0.1f));
                        if (!tileBounds.Intersects(spikeBounds))
                            continue;

                        ground.SetTile(cell, null);
                        removed++;
                    }
                }
            }
        }
        ground.RefreshAllTiles();
        EditorUtility.SetDirty(ground);
        return removed;
    }

    private static void ScalePlayerVisual(Player player, float targetScale)
    {
        SpriteRenderer renderer = player.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer == null)
            throw new InvalidOperationException("Player visual SpriteRenderer is missing.");

        Transform visual = renderer.transform;
        Physics2D.SyncTransforms();
        float footY = renderer.bounds.min.y;
        float signX = Mathf.Sign(visual.localScale.x);
        visual.localScale = new Vector3(signX * targetScale, targetScale, visual.localScale.z);
        Physics2D.SyncTransforms();
        float correctionY = footY - renderer.bounds.min.y;
        visual.position += Vector3.up * correctionY;
        EditorUtility.SetDirty(visual);
    }

    private static void ValidateRequestedDetails(Tilemap ground, Player player)
    {
        SpriteRenderer playerRenderer = player.GetComponentInChildren<SpriteRenderer>(true);
        if (playerRenderer == null || Mathf.Abs(Mathf.Abs(playerRenderer.transform.localScale.x) - 2.2f) > 0.01f)
            throw new InvalidOperationException("Autumn player visual scale was not applied.");

        foreach (StealthEnemy enemy in UnityEngine.Object.FindObjectsOfType<StealthEnemy>(true))
        {
            SpriteRenderer primary = enemy.spriteRenderer;
            if (primary == null || !primary.enabled || Mathf.Abs(primary.transform.localPosition.x) > 0.01f)
                throw new InvalidOperationException("Stealth enemy visual is not aligned with its collider.");
            foreach (SpriteRenderer renderer in enemy.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer != primary && renderer.enabled)
                    throw new InvalidOperationException("Stealth enemy still has a detached ghost visual.");
        }

        string[] pitNames = { "Spike_01", "Spike_02" };
        foreach (string pitName in pitNames)
        {
            GameObject pit = GameObject.Find(pitName);
            if (pit == null)
                continue;
            foreach (Renderer spikeRenderer in pit.GetComponentsInChildren<Renderer>(true))
            {
                Bounds spikeBounds = spikeRenderer.bounds;
                Vector3Int min = ground.WorldToCell(spikeBounds.min);
                Vector3Int max = ground.WorldToCell(spikeBounds.max);
                for (int x = min.x; x <= max.x; x++)
                    for (int y = min.y; y <= max.y; y++)
                        if (ground.HasTile(new Vector3Int(x, y, 0)))
                            throw new InvalidOperationException($"Ground still covers {pitName} at cell ({x},{y}).");
            }
        }
    }

    public static void ApplyFromCommandLine()
    {
        Apply(true);
    }

    private static void Apply(bool openScene)
    {
        if (openScene || SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("AutumnRuins scene could not be opened.");

        Tilemap ground = FindGroundTilemap();
        if (ground == null)
            throw new InvalidOperationException("Ground Tilemap was not found in AutumnRuins.");

        RemovePreviousPass();

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Autumn redesign pass");

        BoundsInt cells = ground.cellBounds;
        float minX = ground.CellToWorld(new Vector3Int(cells.xMin, 0, 0)).x;
        float maxX = ground.CellToWorld(new Vector3Int(cells.xMax, 0, 0)).x;
        float centerY = GetCameraCenterY();

        ConfigureCameraBoundary(ground, minX, maxX);
        CreateBackgroundZones(root.transform, minX, maxX, centerY);
        CreateRuinLandmarks(root.transform, ground, cells);
        CreateArcheryRoutes(root.transform, ground, cells);
        CreateAtmosphere(root.transform, ground, cells);
        CreateAutumnCollectibles(root.transform, ground, cells);
        ConfigureStealthEnemies();
        RenameLegacySeasonObjects();
        ReplaceBossWithShadowDragon(ground);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        RepairTraversal();
        RepairTrapEscapes();

        Debug.Log(
            $"[AutumnRedesign] Applied successfully. Map X: {minX:0.0} to {maxX:0.0}, " +
            $"stealth enemies: {UnityEngine.Object.FindObjectsOfType<StealthEnemy>(true).Length}.");
    }

    public static void CapturePreviews()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Tilemap ground = FindGroundTilemap();
        Camera camera = Camera.main;
        if (ground == null || camera == null)
            throw new InvalidOperationException("Ground or Main Camera is missing from AutumnRuins.");

        BoundsInt cells = ground.cellBounds;
        float minX = ground.CellToWorld(new Vector3Int(cells.xMin, 0, 0)).x;
        float maxX = ground.CellToWorld(new Vector3Int(cells.xMax, 0, 0)).x;
        float[] fractions = { 0.115f, 0.205f, 0.426f, 0.86f };
        Vector3 originalPosition = camera.transform.position;
        RenderTexture originalTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        string previewFolder = Path.GetFullPath("Logs/AutumnRedesignPreviews");
        Directory.CreateDirectory(previewFolder);

        RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);

        for (int i = 0; i < fractions.Length; i++)
        {
            GameObject boss = GameObject.Find("[BossAutumnShadowDragon]");
            float x = i == fractions.Length - 1 && boss != null
                ? boss.transform.position.x
                : Mathf.Lerp(minX, maxX, fractions[i]);
            int cellX = ground.WorldToCell(new Vector3(x, 0f, 0f)).x;
            float y = i == fractions.Length - 1 && boss != null
                ? boss.transform.position.y + 0.8f
                : GetSurfaceWorldY(ground, cellX) + 2.4f;
            camera.transform.position = new Vector3(x, y, -10f);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(previewFolder, $"Autumn_Zone_{i + 1}.png"), image.EncodeToPNG());
        }

        camera.transform.position = originalPosition;
        camera.targetTexture = originalTarget;
        RenderTexture.active = previousActive;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        Debug.Log("[AutumnRedesign] Preview images written to " + previewFolder);
    }

    public static void ValidateAppliedScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Physics2D.SyncTransforms();

        GameObject bossObject = GameObject.Find("[BossAutumnShadowDragon]");
        if (bossObject == null)
            throw new InvalidOperationException("Autumn Shadow Dragon boss is missing.");
        if (bossObject.GetComponent<ShadowDragonBoss>() == null ||
            bossObject.GetComponent<BossDefeatLevelEnd>() == null ||
            bossObject.GetComponent<BossController>() != null)
            throw new InvalidOperationException("Autumn boss components are not configured correctly.");

        ShadowDragonBoss boss = bossObject.GetComponent<ShadowDragonBoss>();
        if (boss.spriteRenderer == null || boss.anim == null || boss.anim.runtimeAnimatorController == null ||
            boss.shadowBallPrefab == null)
            throw new InvalidOperationException("Autumn boss visual, animator, or projectile is missing.");
        if (boss.spriteRenderer.transform.localScale.x < 2f)
            throw new InvalidOperationException("Autumn boss visual scale is too small.");

        BossDefeatLevelEnd levelEnd = bossObject.GetComponent<BossDefeatLevelEnd>();
        if (levelEnd.delay >= 2f)
            throw new InvalidOperationException("Boss level-end delay exceeds the boss death lifetime.");

        int missingScripts = 0;
        foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform child in rootObject.GetComponentsInChildren<Transform>(true))
                missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
        if (missingScripts > 0)
            throw new InvalidOperationException("Scene contains missing scripts: " + missingScripts);

        StealthEnemy[] stealthEnemies = UnityEngine.Object.FindObjectsOfType<StealthEnemy>(true);
        if (stealthEnemies.Length == 0)
            throw new InvalidOperationException("No stealth enemy exists in AutumnRuins.");
        foreach (StealthEnemy stealthEnemy in stealthEnemies)
        {
            Rigidbody2D body = stealthEnemy.GetComponent<Rigidbody2D>();
            Collider2D collider = stealthEnemy.GetComponent<Collider2D>();
            if (body == null || collider == null || body.bodyType != RigidbodyType2D.Dynamic ||
                body.gravityScale < 3f || collider.isTrigger)
                throw new InvalidOperationException("Stealth enemy ground physics is invalid: " + stealthEnemy.name);
        }

        GameObject collectibleRoot = GameObject.Find(RootName + "/05_Autumn_Runes_And_Relics");
        if (collectibleRoot == null || collectibleRoot.transform.childCount < 7)
            throw new InvalidOperationException("Autumn collectibles were not created correctly.");

        ValidateRouteHazardClearance();

        Debug.Log(
            $"[AutumnValidation] PASS | boss={boss.bossName} | stealth={stealthEnemies.Length} | " +
            $"collectibles={collectibleRoot.transform.childCount} | missingScripts={missingScripts}");
    }

    private static void ValidateRouteHazardClearance()
    {
        Tilemap route = FindTraversalRoute();
        if (route == null)
            throw new InvalidOperationException("Autumn route Tilemap is missing.");

        TilemapCollider2D routeCollider = route.GetComponent<TilemapCollider2D>();
        PlatformEffector2D effector = route.GetComponent<PlatformEffector2D>();
        if (routeCollider == null || effector == null || !routeCollider.usedByEffector || !effector.useOneWay)
            throw new InvalidOperationException("Autumn safety route is not configured as one-way platforms.");

        List<Bounds> hazardBounds = new List<Bounds>();
        foreach (Transform transform in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name.IndexOf("spike", StringComparison.OrdinalIgnoreCase) < 0 &&
                transform.name.IndexOf("thorn", StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            Renderer renderer = transform.GetComponentInChildren<Renderer>();
            Collider2D collider = transform.GetComponentInChildren<Collider2D>();
            if (renderer != null)
                hazardBounds.Add(renderer.bounds);
            else if (collider != null)
                hazardBounds.Add(collider.bounds);
        }

        Vector3 cellSize = Vector3.Scale(route.layoutGrid.cellSize, route.transform.lossyScale);
        foreach (Vector3Int cell in route.cellBounds.allPositionsWithin)
        {
            if (!route.HasTile(cell))
                continue;
            Bounds tileBounds = new Bounds(route.GetCellCenterWorld(cell), new Vector3(
                Mathf.Abs(cellSize.x) * 0.9f,
                Mathf.Abs(cellSize.y) * 0.9f,
                0.1f));
            foreach (Bounds hazard in hazardBounds)
                if (tileBounds.Intersects(hazard))
                    throw new InvalidOperationException(
                        $"Safety tile at {cell} overlaps a spike/thorn hazard at {hazard.center}.");
        }
    }

    public static void AuditTraversal()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Tilemap ground = FindGroundTilemap();
        Tilemap route = FindTraversalRoute();

        Player player = UnityEngine.Object.FindObjectOfType<Player>();
        GameObject boss = GameObject.Find("[BossAutumnShadowDragon]");
        if (ground == null || route == null || player == null || boss == null)
            throw new InvalidOperationException("Traversal audit requires Ground, Autumn route, Player, and boss.");

        TraversalReport report = BuildTraversalReport(
            ground, FindTraversalRoutes(), player.transform.position, boss.transform.position);
        Vector2Int frontierCell = FindRightmostReachable(report.Reachable, ground, boss.transform.position.x);
        Vector2Int nextCell = FindNextTraversalTarget(report.Surfaces, report.Reachable, frontierCell);
        Vector3 frontierWorld = frontierCell.x == int.MinValue
            ? player.transform.position
            : ground.GetCellCenterWorld(new Vector3Int(frontierCell.x, frontierCell.y, 0));
        Vector3 nextWorld = nextCell.x == int.MinValue
            ? boss.transform.position
            : ground.GetCellCenterWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        Debug.Log(
            $"[AutumnTraversal] reachable={report.ReachableCount}/{report.SurfaceCount} | " +
            $"frontierX={report.FrontierWorldX:0.0} | bossX={boss.transform.position.x:0.0} | " +
            $"bossReachable={report.BossReachable} | traps={report.TrapSurfaceCount} | " +
            $"jumpCells={report.MaxJumpCells} | riseCells={report.MaxRiseCells} | " +
            $"frontierCell=({frontierCell.x},{frontierCell.y})@({frontierWorld.x:0.0},{frontierWorld.y:0.0}) | " +
            $"nextCell=({nextCell.x},{nextCell.y})@({nextWorld.x:0.0},{nextWorld.y:0.0})");
        if (report.TrapSurfaces.Count > 0)
        {
            List<string> traps = new List<string>();
            foreach (Vector2Int trap in report.TrapSurfaces)
            {
                Vector3 world = ground.GetCellCenterWorld(new Vector3Int(trap.x, trap.y, 0));
                traps.Add($"({world.x:0.0},{world.y + 0.5f:0.0})");
            }
            Debug.Log("[AutumnTraversalTraps] " + string.Join(", ", traps));
        }

        if (!report.BossReachable)
            throw new InvalidOperationException(
                $"Completion route is blocked near world X={report.FrontierWorldX:0.0}; boss is at X={boss.transform.position.x:0.0}.");
        if (report.TrapSurfaceCount > 0)
            throw new InvalidOperationException(
                $"Traversal contains {report.TrapSurfaceCount} reachable surfaces that cannot return to the completion route.");
    }

    public static void RepairTraversal()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Tilemap ground = FindGroundTilemap();
        Tilemap route = FindTraversalRoute();

        Player player = UnityEngine.Object.FindObjectOfType<Player>();
        GameObject boss = GameObject.Find("[BossAutumnShadowDragon]");
        if (ground == null || route == null || player == null || boss == null)
            throw new InvalidOperationException("Traversal repair requires Ground, Autumn route, Player, and boss.");

        TileBase[] pattern = FindExistingPlatformPattern(ground);
        TileBase fallback = LoadTile(PlatformTileGuid);
        int repairs = 0;
        float previousFrontier = float.MinValue;

        for (int iteration = 0; iteration < 28; iteration++)
        {
            TraversalReport report = BuildTraversalReport(
                ground, FindTraversalRoutes(), player.transform.position, boss.transform.position);
            if (report.BossReachable)
            {
                route.RefreshAllTiles();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[AutumnTraversalRepair] PASS | repairs={repairs} | " +
                    $"reachable={report.ReachableCount}/{report.SurfaceCount} | bossX={boss.transform.position.x:0.0}");
                return;
            }

            Vector2Int frontier = FindRightmostReachable(report.Reachable, ground, boss.transform.position.x);
            Vector2Int target = FindNextTraversalTarget(report.Surfaces, report.Reachable, frontier);
            if (target.x == int.MinValue)
                throw new InvalidOperationException("No traversal surface exists beyond the current frontier.");

            bool forceContinuous = report.FrontierWorldX <= previousFrontier + 0.01f;
            AddTraversalConnection(route, ground, frontier, target, pattern, fallback, forceContinuous);
            previousFrontier = report.FrontierWorldX;
            repairs++;

            Debug.Log(
                $"[AutumnTraversalRepair] bridge {repairs}: " +
                $"({frontier.x},{frontier.y}) -> ({target.x},{target.y}), continuous={forceContinuous}");
        }

        TraversalReport failed = BuildTraversalReport(
            ground, FindTraversalRoutes(), player.transform.position, boss.transform.position);
        throw new InvalidOperationException(
            $"Traversal repair stopped after {repairs} bridges at X={failed.FrontierWorldX:0.0}.");
    }

    public static void RepairTrapEscapes()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Tilemap ground = FindGroundTilemap();
        Tilemap route = FindTraversalRoute();

        Player player = UnityEngine.Object.FindObjectOfType<Player>();
        GameObject boss = GameObject.Find("[BossAutumnShadowDragon]");
        if (ground == null || route == null || player == null || boss == null)
            throw new InvalidOperationException("Trap repair requires Ground, Autumn route, Player, and boss.");

        TileBase[] pattern = FindExistingPlatformPattern(ground);
        TileBase fallback = LoadTile(PlatformTileGuid);
        int repairs = 0;

        for (int iteration = 0; iteration < 12; iteration++)
        {
            TraversalReport report = BuildTraversalReport(
                ground, FindTraversalRoutes(), player.transform.position, boss.transform.position);
            if (!report.BossReachable)
                throw new InvalidOperationException("Main completion route must be repaired before trap escapes.");
            if (report.TrapSurfaceCount == 0)
            {
                route.RefreshAllTiles();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                Debug.Log($"[AutumnTrapRepair] PASS | repairs={repairs} | traps=0");
                return;
            }

            Vector2Int trap = FindTrapClusterCenter(report.TrapSurfaces);
            Vector2Int target = FindNearestSafeSurface(report.Reachable, report.TrapSurfaces, trap);
            if (target.x == int.MinValue)
                throw new InvalidOperationException("No safe surface was found near a traversal trap.");

            PlaceTrapEscapePlatform(route, ground, report.TrapSurfaces, trap, target, pattern, fallback);
            repairs++;
            Debug.Log(
                $"[AutumnTrapRepair] escape {repairs}: ({trap.x},{trap.y}) -> ({target.x},{target.y}), " +
                $"trapsBefore={report.TrapSurfaceCount}");
        }

        TraversalReport failed = BuildTraversalReport(
            ground, FindTraversalRoutes(), player.transform.position, boss.transform.position);
        throw new InvalidOperationException($"Trap repair stopped with {failed.TrapSurfaceCount} trap surfaces remaining.");
    }

    public static void AuditBossZoneRenderers()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Tilemap ground = FindGroundTilemap();
        BoundsInt cells = ground.cellBounds;
        float minX = ground.CellToWorld(new Vector3Int(cells.xMin, 0, 0)).x;
        float maxX = ground.CellToWorld(new Vector3Int(cells.xMax, 0, 0)).x;
        float x = Mathf.Lerp(minX, maxX, 0.86f);
        int cellX = ground.WorldToCell(new Vector3(x, 0f, 0f)).x;
        float y = GetSurfaceWorldY(ground, cellX) + 2.4f;
        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = renderer.bounds;
            if (Mathf.Abs(renderer.transform.position.x - x) > 18f)
                continue;

            Debug.Log(
                $"[BossZoneRenderer] {GetHierarchyPath(renderer.transform)} | order={renderer.sortingOrder} | " +
                $"sprite={AssetDatabase.GetAssetPath(renderer.sprite)} | center={bounds.center} | size={bounds.size} | " +
                $"color={renderer.color}");
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static Tilemap FindGroundTilemap()
    {
        foreach (Tilemap tilemap in UnityEngine.Object.FindObjectsOfType<Tilemap>(true))
        {
            if (tilemap.gameObject.name == "Ground")
                return tilemap;
        }

        return null;
    }

    private static void RemovePreviousPass()
    {
        GameObject previous = GameObject.Find(RootName);
        if (previous != null)
            Undo.DestroyObjectImmediate(previous);
    }

    private static float GetCameraCenterY()
    {
        Camera camera = Camera.main;
        return camera != null ? camera.transform.position.y : 0f;
    }

    private static void CreateBackgroundZones(Transform parent, float minX, float maxX, float centerY)
    {
        GameObject backgroundRoot = CreateChild(parent, "01_Seasonal_Background_Zones");
        string[] zoneNames =
        {
            "Golden_Grove",
            "Crimson_Ruins",
            "Shadow_Ravine",
            "Boss_Gate_Dusk"
        };

        foreach (string zoneName in zoneNames)
            CreateChild(backgroundRoot.transform, zoneName);

        string coherentSet = BackgroundFolders[3];
        CreateBackgroundStrip(
            backgroundRoot.transform,
            coherentSet + "/background 4.png",
            minX,
            maxX,
            centerY + 1f,
            28f,
            -92,
            0f,
            Color.white);
        CreateBackgroundStrip(backgroundRoot.transform, coherentSet + "/Plan-5.png",
            minX, maxX, centerY + 0.85f, 28f, -88, 0.03f, Color.white);
        CreateBackgroundStrip(backgroundRoot.transform, coherentSet + "/Plan-3.png",
            minX, maxX, centerY + 0.65f, 28f, -86, 0.07f, Color.white);
        CreateBackgroundStrip(backgroundRoot.transform, coherentSet + "/Plan-1.png",
            minX, maxX, centerY + 0.45f, 28f, -84, 0.1f, new Color(1f, 1f, 1f, 0.94f));
    }

    private static void CreateBackgroundStrip(
        Transform parent,
        string assetPath,
        float minX,
        float maxX,
        float centerY,
        float targetHeight,
        int sortingOrder,
        float parallax,
        Color tint)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            Debug.LogWarning("[AutumnRedesign] Missing background sprite: " + assetPath);
            return;
        }

        GameObject layer = CreateChild(parent, sprite.name + "_Layer_" + sortingOrder);
        ParallaxBackground parallaxBackground = layer.AddComponent<ParallaxBackground>();
        parallaxBackground.parallaxMultiplier = parallax;

        float scale = targetHeight / sprite.bounds.size.y;
        float tileWidth = sprite.bounds.size.x * scale;
        int tileCount = Mathf.CeilToInt((maxX - minX) / tileWidth) + 2;
        float startX = minX - tileWidth * 0.5f;

        for (int i = 0; i < tileCount; i++)
        {
            GameObject tile = CreateChild(layer.transform, sprite.name + "_" + i);
            tile.transform.position = new Vector3(startX + i * tileWidth, centerY, 10f);
            tile.transform.localScale = new Vector3(scale * 1.01f, scale * 1.01f, 1f);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private static void CreateRuinLandmarks(Transform parent, Tilemap ground, BoundsInt cells)
    {
        GameObject landmarks = CreateChild(parent, "02_Ruin_Landmarks");
        float width = cells.size.x;
        int[] anchors =
        {
            cells.xMin + Mathf.RoundToInt(width * 0.16f),
            cells.xMin + Mathf.RoundToInt(width * 0.39f),
            cells.xMin + Mathf.RoundToInt(width * 0.62f),
            cells.xMin + Mathf.RoundToInt(width * 0.85f)
        };

        CreateBrokenArch(landmarks.transform, ground, anchors[0], "Grove_Broken_Arch", new Color(0.22f, 0.12f, 0.08f, 0.92f));
        CreateWatchTower(landmarks.transform, ground, anchors[1], "Crimson_Watchtower", new Color(0.18f, 0.08f, 0.07f, 0.94f));
        CreateBrokenArch(landmarks.transform, ground, anchors[2], "Ravine_Collapsed_Gate", new Color(0.1f, 0.07f, 0.09f, 0.95f));
        CreateWatchTower(landmarks.transform, ground, anchors[3], "Boss_Gate_Towers", new Color(0.12f, 0.055f, 0.045f, 0.98f));
    }

    private static void CreateBrokenArch(Transform parent, Tilemap ground, int cellX, string name, Color color)
    {
        GameObject root = CreateChild(parent, name);
        float y = GetSurfaceWorldY(ground, cellX);
        float x = ground.CellToWorld(new Vector3Int(cellX, 0, 0)).x;

        CreateBlock(root.transform, "Left_Pillar", new Vector2(x - 2.1f, y + 2.4f), new Vector2(0.85f, 4.8f), color, -24, -2f);
        CreateBlock(root.transform, "Right_Pillar", new Vector2(x + 2.1f, y + 1.85f), new Vector2(0.9f, 3.7f), color, -24, 3f);
        CreateBlock(root.transform, "Broken_Lintel", new Vector2(x - 0.35f, y + 4.45f), new Vector2(3.7f, 0.65f), color, -24, -7f);
        CreateBlock(root.transform, "Fallen_Stone", new Vector2(x + 1.3f, y + 0.35f), new Vector2(1.7f, 0.55f), color, -23, 13f);
    }

    private static void CreateWatchTower(Transform parent, Tilemap ground, int cellX, string name, Color color)
    {
        GameObject root = CreateChild(parent, name);
        float y = GetSurfaceWorldY(ground, cellX);
        float x = ground.CellToWorld(new Vector3Int(cellX, 0, 0)).x;

        CreateBlock(root.transform, "Tower_Core", new Vector2(x, y + 3.2f), new Vector2(2.3f, 6.4f), color, -25, 0f);
        CreateBlock(root.transform, "Tower_Broken_Cap", new Vector2(x + 0.35f, y + 6.4f), new Vector2(3.1f, 0.7f), color, -24, 6f);
        CreateBlock(root.transform, "Window_Cutout", new Vector2(x, y + 3.8f), new Vector2(0.65f, 1.3f), new Color(0.04f, 0.025f, 0.03f, 0.95f), -23, 0f);
        CreateBlock(root.transform, "Fallen_Beam", new Vector2(x - 2.1f, y + 0.7f), new Vector2(3.2f, 0.45f), color, -23, -18f);
    }

    private static void CreateArcheryRoutes(Transform parent, Tilemap ground, BoundsInt cells)
    {
        GameObject routeRoot = CreateChild(parent, "03_Archery_Perches");
        routeRoot.transform.position = ground.transform.parent.position;
        routeRoot.transform.rotation = ground.transform.parent.rotation;
        routeRoot.transform.localScale = ground.transform.parent.lossyScale;

        Grid routeGrid = routeRoot.AddComponent<Grid>();
        GridLayout sourceGrid = ground.layoutGrid;
        if (sourceGrid != null)
        {
            routeGrid.cellSize = sourceGrid.cellSize;
            routeGrid.cellGap = sourceGrid.cellGap;
            routeGrid.cellLayout = sourceGrid.cellLayout;
            routeGrid.cellSwizzle = sourceGrid.cellSwizzle;
        }

        GameObject tilemapObject = CreateChild(routeRoot.transform, "Autumn_Archery_Route_Tilemap");
        tilemapObject.layer = LayerMask.NameToLayer("Ground");
        tilemapObject.tag = "Ground";
        tilemapObject.transform.localPosition = ground.transform.localPosition;
        tilemapObject.transform.localRotation = ground.transform.localRotation;
        tilemapObject.transform.localScale = ground.transform.localScale;

        Tilemap route = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 1;
        TilemapCollider2D routeCollider = tilemapObject.AddComponent<TilemapCollider2D>();
        routeCollider.usedByEffector = true;
        PlatformEffector2D platformEffector = tilemapObject.AddComponent<PlatformEffector2D>();
        platformEffector.useOneWay = true;
        platformEffector.useOneWayGrouping = true;
        platformEffector.surfaceArc = 160f;
        platformEffector.useSideFriction = false;
        platformEffector.useSideBounce = false;

        TileBase[] platformPattern = FindExistingPlatformPattern(ground);
        TileBase fallbackTile = LoadTile(PlatformTileGuid);
        if ((platformPattern == null || platformPattern.Length == 0) && fallbackTile == null)
        {
            Debug.LogWarning("[AutumnRedesign] Platform tile could not be loaded.");
            return;
        }

        float[] fractions = { 0.34f, 0.59f, 0.81f };
        int[] widths = { 4, 3, 4 };
        int[] heights = { 3, 2, 3 };

        for (int i = 0; i < fractions.Length; i++)
        {
            int centerX = cells.xMin + Mathf.RoundToInt(cells.size.x * fractions[i]);
            int surfaceY = FindSurfaceCellY(ground, centerX);
            int platformY = surfaceY + heights[i];
            int half = widths[i] / 2;

            if (!HasClearance(ground, route, centerX - half, centerX + half, platformY))
                continue;

            int startX = centerX - half;
            int endX = centerX + half;
            for (int x = startX; x <= endX; x++)
            {
                int index = x - startX;
                int length = endX - startX + 1;
                TileBase tile = PickPlatformTile(platformPattern, fallbackTile, index, length);
                route.SetTile(new Vector3Int(x, platformY, 0), tile);
            }
        }

        CreateTraversalBridge(route, ground, cells, platformPattern, fallbackTile);

        route.RefreshAllTiles();
    }

    private static void CreateTraversalBridge(
        Tilemap route,
        Tilemap ground,
        BoundsInt cells,
        TileBase[] platformPattern,
        TileBase fallbackTile)
    {
        // The copied Spring terrain leaves a gap slightly wider than the player's normal jump here.
        int centerX = cells.xMin + Mathf.RoundToInt(cells.size.x * 0.362f);
        int leftSurfaceY = FindSurfaceCellY(ground, centerX - 5);
        int rightSurfaceY = FindSurfaceCellY(ground, centerX + 5);
        int bridgeY = Mathf.Min(leftSurfaceY, rightSurfaceY) + 1;
        const int width = 5;
        int startX = centerX - width / 2;

        for (int i = 0; i < width; i++)
        {
            int x = startX + i;
            Vector3Int cell = new Vector3Int(x, bridgeY, 0);
            if (ground.HasTile(cell))
                continue;
            route.SetTile(cell, PickPlatformTile(platformPattern, fallbackTile, i, width));
        }
    }

    private static void ConfigureCameraBoundary(Tilemap ground, float minX, float maxX)
    {
        GameObject boundaryObject = GameObject.Find("CameraBoundary");
        if (boundaryObject == null)
            return;

        PolygonCollider2D boundary = boundaryObject.GetComponent<PolygonCollider2D>();
        if (boundary == null)
            return;

        BoundsInt cells = ground.cellBounds;
        float bottomY = ground.CellToWorld(new Vector3Int(0, cells.yMin, 0)).y - 5f;
        float topY = ground.CellToWorld(new Vector3Int(0, cells.yMax, 0)).y + 8f;
        Vector3[] worldCorners =
        {
            new Vector3(minX - 2f, bottomY, 0f),
            new Vector3(minX - 2f, topY, 0f),
            new Vector3(maxX + 2f, topY, 0f),
            new Vector3(maxX + 2f, bottomY, 0f)
        };
        Vector2[] localPath = new Vector2[worldCorners.Length];
        for (int i = 0; i < worldCorners.Length; i++)
            localPath[i] = boundary.transform.InverseTransformPoint(worldCorners[i]);

        boundary.pathCount = 1;
        boundary.SetPath(0, localPath);
        EditorUtility.SetDirty(boundary);
    }

    private static TileBase PickPlatformTile(TileBase[] pattern, TileBase fallback, int index, int length)
    {
        if (pattern == null || pattern.Length == 0)
            return fallback;
        if (pattern.Length == 1)
            return pattern[0];
        if (index == 0)
            return pattern[0];
        if (index == length - 1)
            return pattern[pattern.Length - 1];
        return pattern[Mathf.Min(1, pattern.Length - 2)];
    }

    private static TileBase[] FindExistingPlatformPattern(Tilemap ground)
    {
        BoundsInt bounds = ground.cellBounds;
        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
        {
            List<TileBase> run = new List<TileBase>();
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                TileBase tile = ground.GetTile(cell);
                bool floating = tile != null &&
                                !ground.HasTile(cell + Vector3Int.up) &&
                                !ground.HasTile(cell + Vector3Int.down);

                if (floating)
                {
                    run.Add(tile);
                    continue;
                }

                if (run.Count >= 3 && run.Count <= 8)
                    return run.ToArray();
                run.Clear();
            }

            if (run.Count >= 3 && run.Count <= 8)
                return run.ToArray();
        }

        return null;
    }

    private static bool HasClearance(Tilemap ground, Tilemap route, int minX, int maxX, int y)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int checkY = y; checkY <= y + 2; checkY++)
            {
                Vector3Int cell = new Vector3Int(x, checkY, 0);
                if (ground.HasTile(cell) || route.HasTile(cell))
                    return false;
            }
        }

        return true;
    }

    private static void CreateAtmosphere(Transform parent, Tilemap ground, BoundsInt cells)
    {
        GameObject atmosphere = CreateChild(parent, "04_Atmosphere_And_Foreground");
        Sprite blockSprite = GetBlockSprite();
        if (blockSprite == null)
            return;

        int seed = 7319;
        System.Random random = new System.Random(seed);

        for (int i = 0; i < 28; i++)
        {
            int cellX = cells.xMin + 6 + (i * Math.Max(1, (cells.size.x - 12) / 28));
            float x = ground.CellToWorld(new Vector3Int(cellX, 0, 0)).x;
            float surfaceY = GetSurfaceWorldY(ground, cellX);
            float y = surfaceY + 1.2f + (float)random.NextDouble() * 4.5f;

            GameObject leaf = CreateChild(atmosphere.transform, "Drifting_Leaf_" + (i + 1));
            leaf.transform.position = new Vector3(x, y, 0f);
            leaf.transform.localScale = new Vector3(0.16f + (float)random.NextDouble() * 0.16f, 0.08f, 1f);
            leaf.transform.rotation = Quaternion.Euler(0f, 0f, random.Next(-35, 36));

            SpriteRenderer leafRenderer = leaf.AddComponent<SpriteRenderer>();
            leafRenderer.sprite = blockSprite;
            leafRenderer.sortingOrder = 18;
            leafRenderer.color = i % 3 == 0
                ? new Color(0.95f, 0.36f, 0.08f, 0.85f)
                : i % 3 == 1
                    ? new Color(0.86f, 0.62f, 0.08f, 0.8f)
                    : new Color(0.58f, 0.16f, 0.08f, 0.8f);

            AutumnLeafDrift drift = leaf.AddComponent<AutumnLeafDrift>();
            drift.swayAmplitude = 0.35f + (float)random.NextDouble() * 0.55f;
            drift.swayFrequency = 0.35f + (float)random.NextDouble() * 0.45f;
            drift.spinSpeed = random.Next(25, 75);
        }

        float mapWidth = cells.size.x * ground.layoutGrid.cellSize.x;
        float minWorldX = ground.CellToWorld(new Vector3Int(cells.xMin, 0, 0)).x;
        for (int i = 0; i < 5; i++)
        {
            int cellX = cells.xMin + Mathf.RoundToInt(cells.size.x * ((i + 0.5f) / 5f));
            float y = GetSurfaceWorldY(ground, cellX) + 0.65f;
            GameObject fog = CreateChild(atmosphere.transform, "Low_Autumn_Mist_" + (i + 1));
            fog.transform.position = new Vector3(minWorldX + mapWidth * ((i + 0.5f) / 5f), y, 0f);
            fog.transform.localScale = new Vector3(mapWidth / 4.2f, 0.7f, 1f);

            SpriteRenderer fogRenderer = fog.AddComponent<SpriteRenderer>();
            fogRenderer.sprite = blockSprite;
            fogRenderer.sortingOrder = -8;
            fogRenderer.color = new Color(0.72f, 0.34f, 0.16f, 0.08f);

            AutumnFogEffect effect = fog.AddComponent<AutumnFogEffect>();
            effect.driftRange = 2.2f;
            effect.minAlpha = 0.035f;
            effect.maxAlpha = 0.1f;
        }
    }

    private static void ConfigureStealthEnemies()
    {
        foreach (StealthEnemy enemy in UnityEngine.Object.FindObjectsOfType<StealthEnemy>(true))
        {
            SerializedObject serializedEnemy = new SerializedObject(enemy);
            SetFloat(serializedEnemy, "stealthAlpha", 0.28f);
            SetFloat(serializedEnemy, "revealRange", 6f);
            SetFloat(serializedEnemy, "fullRevealRange", 3f);
            SetFloat(serializedEnemy, "fadeSpeed", 14f);
            SetFloat(serializedEnemy, "visibleAlpha", 1f);
            SetFloat(serializedEnemy, "minimumGravityScale", 3f);
            SerializedProperty groundLayers = serializedEnemy.FindProperty("groundLayers");
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayers != null && groundLayer >= 0)
                groundLayers.intValue = 1 << groundLayer;
            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();

            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = Mathf.Max(3f, body.gravityScale);
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                EditorUtility.SetDirty(body);
            }
            EditorUtility.SetDirty(enemy);
        }
    }

    private static void RenameLegacySeasonObjects()
    {
        GameObject springBossRoot = GameObject.Find("[BossSpring]");
        if (springBossRoot != null)
            springBossRoot.name = "[BossAutumnGuardian]";
    }

    private static void ReplaceBossWithShadowDragon(Tilemap ground)
    {
        GameObject bossRoot = GameObject.Find("[BossAutumnGuardian]");
        if (bossRoot == null)
            bossRoot = GameObject.Find("[BossAutumnShadowDragon]");
        if (bossRoot == null)
            return;

        RuntimeAnimatorController controller = BuildDragonAnimatorController();
        GameObject shadowBallPrefab = BuildShadowBallPrefab();

        Collider2D oldCollider = bossRoot.GetComponent<Collider2D>();
        float bossX = oldCollider != null ? oldCollider.bounds.center.x : bossRoot.transform.position.x;

        for (int i = bossRoot.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(bossRoot.transform.GetChild(i).gameObject);

        BossController legacyBoss = bossRoot.GetComponent<BossController>();
        if (legacyBoss != null)
            Undo.DestroyObjectImmediate(legacyBoss);

        bossRoot.name = "[BossAutumnShadowDragon]";
        bossRoot.layer = LayerMask.NameToLayer("Enemy");
        bossRoot.transform.rotation = Quaternion.identity;

        Vector2 arenaFloor = FindBossArenaFloor(ground, bossX);
        bossX = arenaFloor.x;
        float groundY = arenaFloor.y;
        bossRoot.transform.position = new Vector3(bossX, groundY + 1.4f, 0f);

        Health health = bossRoot.GetComponent<Health>();
        if (health == null)
            health = Undo.AddComponent<Health>(bossRoot);
        health.maxHealth = 180;
        health.health = health.maxHealth;

        Rigidbody2D body = bossRoot.GetComponent<Rigidbody2D>();
        if (body == null)
            body = Undo.AddComponent<Rigidbody2D>(bossRoot);
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 4f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D box = bossRoot.GetComponent<BoxCollider2D>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider2D>(bossRoot);
        box.isTrigger = false;
        box.offset = Vector2.zero;
        box.size = new Vector2(3.6f, 2.8f);

        GameObject visual = CreateChild(bossRoot.transform, "Shadow_Demon_Dragon_Visual");
        visual.layer = bossRoot.layer;
        visual.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        visual.transform.localScale = Vector3.one * 2.3f;

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadFirstDragonSprite("Idle_Left/Idle_left.png");
        renderer.sortingOrder = 20;
        renderer.color = Color.white;

        Animator animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        GameObject firePointObject = CreateChild(bossRoot.transform, "Shadow_FirePoint");
        firePointObject.transform.localPosition = new Vector3(-1.65f, 0.45f, 0f);

        ShadowDragonBoss boss = bossRoot.GetComponent<ShadowDragonBoss>();
        if (boss == null)
            boss = Undo.AddComponent<ShadowDragonBoss>(bossRoot);
        boss.bossName = "Autumn Shadow Dragon";
        boss.stanceDrop = StanceManager.StanceType.Autumn;
        boss.aggroRange = 18f;
        boss.attackRange = 2.6f;
        boss.moveSpeed = 3.4f;
        boss.attackDamage = 18;
        boss.attackCooldown = 1.6f;
        boss.spriteRenderer = renderer;
        boss.anim = animator;
        boss.health = health;
        boss.shadowBallPrefab = shadowBallPrefab;
        boss.firePoint = firePointObject.transform;
        boss.projectileSpeed = 8f;
        boss.projectileDamage = 10;
        boss.summonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Mushroom_Enemy.prefab");
        boss.attackSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sprites/NhanVat/Shadow_Demon_Dragon/Audio/dragon_attack.mp3");
        boss.hitSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sprites/NhanVat/Shadow_Demon_Dragon/Audio/dragon_hit.mp3");
        boss.deathSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sprites/NhanVat/Shadow_Demon_Dragon/Audio/dragon_death.mp3");
        boss.idleSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sprites/NhanVat/Shadow_Demon_Dragon/Audio/dragon_idle.mp3");

        BossDefeatLevelEnd levelEnd = bossRoot.GetComponent<BossDefeatLevelEnd>();
        if (levelEnd == null)
            levelEnd = Undo.AddComponent<BossDefeatLevelEnd>(bossRoot);
        levelEnd.nextSceneName = "MainMenu";
        levelEnd.delay = 1.25f;

        EditorUtility.SetDirty(bossRoot);
        EditorUtility.SetDirty(health);
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(box);
        EditorUtility.SetDirty(boss);
        EditorUtility.SetDirty(levelEnd);
    }

    private static RuntimeAnimatorController BuildDragonAnimatorController()
    {
        EnsureAssetFolder("Assets/Animations", "Enemy");
        EnsureAssetFolder("Assets/Animations/Enemy", "ShadowDragonAutumn");

        Dictionary<string, string> sheets = new Dictionary<string, string>
        {
            { "Idle", "Idle_Left/Idle_left.png" },
            { "Walk", "Walk_Left/Walk_left.png" },
            { "Attack", "Attack_Left/Attack_left.png" },
            { "Hit", "Hit_Left/Hit_left.png" },
            { "Death", "Death_Left/Death_left.png" }
        };

        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        foreach (KeyValuePair<string, string> pair in sheets)
        {
            string path = DragonSheetPath(pair.Value);
            SliceDragonSheet(path, pair.Key);
            clips[pair.Key] = CreateDragonClip(pair.Key, path, pair.Key == "Idle" || pair.Key == "Walk");
        }

        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DragonControllerPath) != null)
            AssetDatabase.DeleteAsset(DragonControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(DragonControllerPath);
        controller.AddParameter("isRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isAttacking", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("isDamaged", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("isDead", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = machine.AddState("Idle");
        AnimatorState walk = machine.AddState("Walk");
        AnimatorState attack = machine.AddState("Attack");
        AnimatorState hit = machine.AddState("Hit");
        AnimatorState death = machine.AddState("Death");
        idle.motion = clips["Idle"];
        walk.motion = clips["Walk"];
        attack.motion = clips["Attack"];
        hit.motion = clips["Hit"];
        death.motion = clips["Death"];
        machine.defaultState = idle;

        AddBoolTransition(idle, walk, "isRunning", true);
        AddBoolTransition(walk, idle, "isRunning", false);
        AddTriggerTransition(machine, attack, "isAttacking");
        AddTriggerTransition(machine, hit, "isDamaged");
        AddTriggerTransition(machine, death, "isDead");
        AddExitTransition(attack, idle, 0.88f);
        AddExitTransition(hit, idle, 0.9f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void SliceDragonSheet(string path, string prefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Dragon sprite sheet is missing: " + path);

        const int columns = 5;
        const int frameWidth = 1408;
        const int frameHeight = 792;
        int rows = prefix == "Hit" ? 6 : prefix == "Death" ? 4 : 2;
        int sourceHeight = rows * frameHeight;
        List<SpriteMetaData> frames = new List<SpriteMetaData>(columns * rows);

        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                frames.Add(new SpriteMetaData
                {
                    name = prefix + "_" + index.ToString("D2"),
                    rect = new Rect(column * frameWidth, sourceHeight - (row + 1) * frameHeight, frameWidth, frameHeight),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
                index++;
            }
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 300f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 8192;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
#pragma warning disable CS0618
        importer.spritesheet = frames.ToArray();
#pragma warning restore CS0618
        importer.SaveAndReimport();
    }

    private static AnimationClip CreateDragonClip(string name, string sheetPath, bool loop)
    {
        string clipPath = DragonAnimationFolder + "/ShadowDragon_" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            AssetDatabase.DeleteAsset(clipPath);

        List<Sprite> sprites = new List<Sprite>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
            if (asset is Sprite sprite)
                sprites.Add(sprite);
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        AnimationClip clip = new AnimationClip { frameRate = 12f };
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = loop });
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState to, string parameter)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = 0.08f;
    }

    private static Sprite LoadFirstDragonSprite(string relativePath)
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(DragonSheetPath(relativePath)))
            if (asset is Sprite sprite)
                return sprite;
        return null;
    }

    private static string DragonSheetPath(string relativePath)
    {
        return "Assets/Sprites/NhanVat/Shadow_Demon_Dragon/Animations/" + relativePath;
    }

    private static GameObject BuildShadowBallPrefab()
    {
        EnsureAssetFolder("Assets/Prefab", "Autumn");
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShadowBallPrefabPath);
        if (existing != null)
            return existing;

        GameObject projectileObject = new GameObject("Autumn_Shadow_Ball");
        projectileObject.layer = LayerMask.NameToLayer("Enemy");
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        renderer.color = new Color(0.28f, 0.08f, 0.42f, 1f);
        renderer.sortingOrder = 22;
        projectileObject.transform.localScale = Vector3.one * 0.42f;

        CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;
        Projectile projectile = projectileObject.AddComponent<Projectile>();
        projectile.lifetime = 5f;
        projectile.destroyOnGroundHit = true;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectileObject, ShadowBallPrefabPath);
        UnityEngine.Object.DestroyImmediate(projectileObject);
        return prefab;
    }

    private static void CreateAutumnCollectibles(Transform parent, Tilemap ground, BoundsInt cells)
    {
        GameObject collectibleRoot = CreateChild(parent, "05_Autumn_Runes_And_Relics");
        GameObject keyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/key.prefab");
        GameObject healthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/HealthPS.prefab");

        CreatePrefabCollectible(collectibleRoot.transform, ground, cells, keyPrefab, 0.18f, "Autumn_Amber_Rune_01", new Color(1f, 0.52f, 0.12f, 1f), 0.55f);
        CreatePrefabCollectible(collectibleRoot.transform, ground, cells, keyPrefab, 0.51f, "Autumn_Amber_Rune_02", new Color(1f, 0.35f, 0.08f, 1f), 0.55f);
        CreatePrefabCollectible(collectibleRoot.transform, ground, cells, keyPrefab, 0.76f, "Autumn_Amber_Rune_03", new Color(0.86f, 0.22f, 0.08f, 1f), 0.55f);
        CreatePrefabCollectible(collectibleRoot.transform, ground, cells, healthPrefab, 0.33f, "Autumn_Regen_Fruit_01", Color.white, 1f);
        CreatePrefabCollectible(collectibleRoot.transform, ground, cells, healthPrefab, 0.68f, "Autumn_Regen_Fruit_02", Color.white, 1f);

        CreateBuffCollectible(collectibleRoot.transform, ground, cells, 0.43f, "Crimson_Leaf_Rage", BuffReceiver.BuffType.DoubleDamage, 10f,
            "Assets/Sprites/HieuUng/Rogulite icon pack/PNG_icons/png_withGLOW/Rage1.png", new Color(1f, 0.62f, 0.38f, 1f));
        CreateBuffCollectible(collectibleRoot.transform, ground, cells, 0.84f, "Ancient_Autumn_Ward", BuffReceiver.BuffType.Invincible, 5f,
            "Assets/Sprites/HieuUng/Rogulite icon pack/PNG_icons/png_withGLOW/Invincible1.png", Color.white);

        Player player = UnityEngine.Object.FindObjectOfType<Player>();
        if (player != null && player.GetComponent<BuffReceiver>() == null)
            Undo.AddComponent<BuffReceiver>(player.gameObject);
    }

    private static void CreatePrefabCollectible(
        Transform parent, Tilemap ground, BoundsInt cells, GameObject prefab, float fraction,
        string name, Color tint, float scaleMultiplier)
    {
        if (prefab == null)
            return;

        GameObject item = PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene) as GameObject;
        if (item == null)
            return;
        item.name = name;
        item.transform.SetParent(parent, true);
        int cellX = cells.xMin + Mathf.RoundToInt(cells.size.x * fraction);
        float x = ground.GetCellCenterWorld(new Vector3Int(cellX, 0, 0)).x;
        item.transform.position = new Vector3(x, GetSurfaceWorldY(ground, cellX) + 1.25f, 0f);
        item.transform.localScale *= scaleMultiplier;
        foreach (SpriteRenderer renderer in item.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.color *= tint;
            renderer.sortingOrder = 15;
        }
    }

    private static void CreateBuffCollectible(
        Transform parent, Tilemap ground, BoundsInt cells, float fraction, string name,
        BuffReceiver.BuffType type, float duration, string spritePath, Color tint)
    {
        Sprite sprite = LoadFirstSpriteAtPath(spritePath);
        if (sprite == null)
            return;

        int cellX = cells.xMin + Mathf.RoundToInt(cells.size.x * fraction);
        GameObject item = CreateChild(parent, name);
        item.transform.position = new Vector3(
            ground.GetCellCenterWorld(new Vector3Int(cellX, 0, 0)).x,
            GetSurfaceWorldY(ground, cellX) + 1.4f,
            0f);
        float scale = 0.85f / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        item.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingOrder = 16;
        CircleCollider2D trigger = item.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = Mathf.Max(sprite.bounds.extents.x, sprite.bounds.extents.y);
        TemporaryBuff buff = item.AddComponent<TemporaryBuff>();
        buff.buffType = type;
        buff.duration = duration;
        buff.spriteRenderer = renderer;
    }

    private static void EnsureAssetFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static Sprite LoadFirstSpriteAtPath(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            return sprite;

        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            if (asset is Sprite subSprite)
                return subSprite;

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return null;

        // Some bundled icons were marked Multiple without any slice data, so Unity exposed no Sprite.
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void SetFloat(SerializedObject target, string propertyName, float value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static int FindSurfaceCellY(Tilemap ground, int cellX)
    {
        BoundsInt bounds = ground.cellBounds;
        int upperLimit = Mathf.Min(bounds.yMax - 1, 10);

        for (int y = upperLimit; y >= bounds.yMin; y--)
        {
            if (ground.HasTile(new Vector3Int(cellX, y, 0)))
                return y;
        }

        for (int offset = 1; offset <= 4; offset++)
        {
            for (int y = upperLimit; y >= bounds.yMin; y--)
            {
                if (ground.HasTile(new Vector3Int(cellX - offset, y, 0)) ||
                    ground.HasTile(new Vector3Int(cellX + offset, y, 0)))
                    return y;
            }
        }

        return -3;
    }

    private static float GetSurfaceWorldY(Tilemap ground, int cellX)
    {
        int surfaceCell = FindSurfaceCellY(ground, cellX);
        return ground.GetCellCenterWorld(new Vector3Int(cellX, surfaceCell, 0)).y + 0.5f;
    }

    private static float GetSolidFloorWorldY(Tilemap ground, int cellX)
    {
        BoundsInt bounds = ground.cellBounds;
        int upperLimit = Mathf.Min(bounds.yMax - 1, 10);
        for (int y = upperLimit; y >= bounds.yMin + 1; y--)
        {
            Vector3Int cell = new Vector3Int(cellX, y, 0);
            if (ground.HasTile(cell) && ground.HasTile(cell + Vector3Int.down))
                return ground.GetCellCenterWorld(cell).y + 0.5f;
        }

        return GetSurfaceWorldY(ground, cellX);
    }

    private static Vector2 FindBossArenaFloor(Tilemap ground, float preferredWorldX)
    {
        BoundsInt bounds = ground.cellBounds;
        int preferredCellX = ground.WorldToCell(new Vector3(preferredWorldX, 0f, 0f)).x;
        int highestArenaY = Mathf.Min(2, bounds.yMax - 1);
        const int minimumArenaWidth = 16;
        int bestCenterX = int.MinValue;
        int bestY = int.MinValue;
        int bestDistance = int.MaxValue;

        for (int y = highestArenaY; y >= bounds.yMin + 1; y--)
        {
            int runStart = int.MinValue;
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                bool surface = x < bounds.xMax && IsSolidSurfaceCell(ground, x, y);
                if (surface && runStart == int.MinValue)
                {
                    runStart = x;
                    continue;
                }

                if (surface || runStart == int.MinValue)
                    continue;

                int runEnd = x - 1;
                int runLength = runEnd - runStart + 1;
                if (runLength >= minimumArenaWidth)
                {
                    int centerX = (runStart + runEnd) / 2;
                    int distance = Mathf.Abs(centerX - preferredCellX);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestCenterX = centerX;
                        bestY = y;
                    }
                }

                runStart = int.MinValue;
            }
        }

        if (bestCenterX != int.MinValue)
        {
            Vector3 center = ground.GetCellCenterWorld(new Vector3Int(bestCenterX, bestY, 0));
            return new Vector2(center.x, center.y + 0.5f);
        }

        int fallbackCellX = ground.WorldToCell(new Vector3(preferredWorldX, 0f, 0f)).x;
        return new Vector2(preferredWorldX, GetSolidFloorWorldY(ground, fallbackCellX));
    }

    private static bool IsSolidSurfaceCell(Tilemap ground, int x, int y)
    {
        Vector3Int cell = new Vector3Int(x, y, 0);
        return ground.HasTile(cell) &&
               ground.HasTile(cell + Vector3Int.down) &&
               !ground.HasTile(cell + Vector3Int.up);
    }

    private static TileBase LoadTile(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<TileBase>(path);
    }

    private static Sprite LoadSpriteByName(string assetPath, string spriteName)
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            if (asset is Sprite sprite && sprite.name == spriteName)
                return sprite;
        return null;
    }

    private static Transform FindTransformByName(string objectName)
    {
        foreach (Transform transform in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            if (transform.name == objectName)
                return transform;
        return null;
    }

    private static GameObject CreateVictoryPanel(Transform canvasTransform)
    {
        GameObject panel = new GameObject(
            VictoryPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = LayerMask.NameToLayer("UI");
        panel.transform.SetParent(canvasTransform, false);
        panel.transform.SetAsLastSibling();
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image overlay = panel.GetComponent<Image>();
        overlay.color = new Color(0.025f, 0.018f, 0.03f, 0.82f);
        overlay.raycastTarget = true;

        GameObject band = new GameObject(
            "Victory_Band", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        band.layer = panel.layer;
        band.transform.SetParent(panel.transform, false);
        RectTransform bandRect = band.GetComponent<RectTransform>();
        bandRect.anchorMin = new Vector2(0.5f, 0.5f);
        bandRect.anchorMax = new Vector2(0.5f, 0.5f);
        bandRect.sizeDelta = new Vector2(820f, 250f);
        bandRect.anchoredPosition = Vector2.zero;
        Image bandImage = band.GetComponent<Image>();
        bandImage.color = new Color(0.16f, 0.055f, 0.075f, 0.94f);
        bandImage.raycastTarget = false;

        CreateVictoryText(
            band.transform,
            "Victory_Title",
            "CHIẾN THẮNG!",
            new Vector2(0f, 43f),
            new Vector2(760f, 82f),
            50f,
            new Color(1f, 0.78f, 0.3f, 1f),
            FontStyles.Bold);
        CreateVictoryText(
            band.transform,
            "Victory_Subtitle",
            "Lõi Cân Bằng Mùa Thu đã được khôi phục",
            new Vector2(0f, -48f),
            new Vector2(720f, 72f),
            27f,
            new Color(0.9f, 0.94f, 0.88f, 1f),
            FontStyles.Normal);
        return panel;
    }

    private static void CreateVictoryText(
        Transform parent,
        string name,
        string content,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color,
        FontStyles style)
    {
        GameObject textObject = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void CreateBlock(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color,
        int sortingOrder,
        float rotation)
    {
        Sprite sprite = GetBlockSprite();
        if (sprite == null)
            return;

        GameObject block = CreateChild(parent, name);
        block.transform.position = new Vector3(position.x, position.y, 0f);
        block.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
        block.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite GetBlockSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private sealed class TraversalReport
    {
        public bool BossReachable;
        public int SurfaceCount;
        public int ReachableCount;
        public int MaxJumpCells;
        public int MaxRiseCells;
        public float FrontierWorldX;
        public int TrapSurfaceCount;
        public HashSet<Vector2Int> Surfaces;
        public HashSet<Vector2Int> Reachable;
        public HashSet<Vector2Int> TrapSurfaces;
    }

    private static TraversalReport BuildTraversalReport(
        Tilemap ground,
        IReadOnlyList<Tilemap> routes,
        Vector3 playerWorldPosition,
        Vector3 bossWorldPosition)
    {
        BoundsInt bounds = ground.cellBounds;
        Vector3Int startCell = ground.WorldToCell(playerWorldPosition);
        Vector3Int bossCell = ground.WorldToCell(bossWorldPosition);
        int minX = Mathf.Max(bounds.xMin, startCell.x - 4);
        int maxX = Mathf.Min(bounds.xMax - 1, bossCell.x + 8);
        int minY = bounds.yMin - 6;
        int maxY = bounds.yMax + 12;

        float cellWidth = Mathf.Abs(
            ground.CellToWorld(new Vector3Int(1, 0, 0)).x -
            ground.CellToWorld(Vector3Int.zero).x);
        float cellHeight = Mathf.Abs(
            ground.CellToWorld(new Vector3Int(0, 1, 0)).y -
            ground.CellToWorld(Vector3Int.zero).y);
        int maxJumpCells = Mathf.Max(3, Mathf.FloorToInt(5.2f / Mathf.Max(0.01f, cellWidth)));
        int maxRiseCells = Mathf.Max(2, Mathf.FloorToInt(3.6f / Mathf.Max(0.01f, cellHeight)));
        int maxDropCells = Mathf.Max(12, Mathf.CeilToInt(20f / Mathf.Max(0.01f, cellHeight)));

        HashSet<Vector2Int> surfaces = new HashSet<Vector2Int>();
        Dictionary<int, List<Vector2Int>> surfacesByX = new Dictionary<int, List<Vector2Int>>();
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int tile = new Vector3Int(x, y, 0);
                if (!IsTraversalOccupied(ground, routes, tile) ||
                    IsTraversalOccupied(ground, routes, tile + Vector3Int.up) ||
                    IsTraversalOccupied(ground, routes, tile + Vector3Int.up * 2))
                    continue;

                Vector2Int surface = new Vector2Int(x, y);
                surfaces.Add(surface);
                if (!surfacesByX.TryGetValue(x, out List<Vector2Int> column))
                {
                    column = new List<Vector2Int>();
                    surfacesByX.Add(x, column);
                }
                column.Add(surface);
            }
        }

        Vector2Int start = FindNearestSurface(surfaces, startCell);
        HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        if (surfaces.Contains(start))
        {
            reachable.Add(start);
            queue.Enqueue(start);
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            for (int x = current.x - maxJumpCells; x <= current.x + maxJumpCells; x++)
            {
                if (!surfacesByX.TryGetValue(x, out List<Vector2Int> candidates))
                    continue;

                foreach (Vector2Int candidate in candidates)
                {
                    if (reachable.Contains(candidate) || candidate == current)
                        continue;
                    int deltaX = Mathf.Abs(candidate.x - current.x);
                    int deltaY = candidate.y - current.y;
                    bool walkableStep = deltaX <= 1 && deltaY >= -2 && deltaY <= 1;
                    bool jumpable = deltaX <= maxJumpCells && deltaY <= maxRiseCells && deltaY >= -maxDropCells;
                    if (!walkableStep && !jumpable)
                        continue;

                    reachable.Add(candidate);
                    queue.Enqueue(candidate);
                }
            }
        }

        float frontierWorldX = playerWorldPosition.x;
        HashSet<Vector2Int> goalSurfaces = new HashSet<Vector2Int>();
        foreach (Vector2Int surface in reachable)
        {
            Vector3 world = ground.GetCellCenterWorld(new Vector3Int(surface.x, surface.y, 0));
            frontierWorldX = Mathf.Max(frontierWorldX, world.x);
            if (Mathf.Abs(world.x - bossWorldPosition.x) <= 4f &&
                Mathf.Abs((world.y + 0.5f) - (bossWorldPosition.y - 1.4f)) <= 3f)
                goalSurfaces.Add(surface);
        }

        HashSet<Vector2Int> canReachGoal = new HashSet<Vector2Int>(goalSurfaces);
        Queue<Vector2Int> reverseQueue = new Queue<Vector2Int>(goalSurfaces);
        while (reverseQueue.Count > 0)
        {
            Vector2Int destination = reverseQueue.Dequeue();
            foreach (Vector2Int candidate in reachable)
            {
                if (canReachGoal.Contains(candidate) ||
                    !IsTraversalStepAllowed(candidate, destination, maxJumpCells, maxRiseCells, maxDropCells))
                    continue;
                canReachGoal.Add(candidate);
                reverseQueue.Enqueue(candidate);
            }
        }
        HashSet<Vector2Int> trapSurfaces = new HashSet<Vector2Int>(reachable);
        trapSurfaces.ExceptWith(canReachGoal);
        RemoveSpikePitSurfacesFromTrapAudit(ground, trapSurfaces);

        return new TraversalReport
        {
            BossReachable = goalSurfaces.Count > 0,
            SurfaceCount = surfaces.Count,
            ReachableCount = reachable.Count,
            MaxJumpCells = maxJumpCells,
            MaxRiseCells = maxRiseCells,
            FrontierWorldX = frontierWorldX,
            TrapSurfaceCount = trapSurfaces.Count,
            Surfaces = surfaces,
            Reachable = reachable,
            TrapSurfaces = trapSurfaces
        };
    }

    private static void RemoveSpikePitSurfacesFromTrapAudit(
        Tilemap ground,
        HashSet<Vector2Int> trapSurfaces)
    {
        List<Bounds> lethalBounds = new List<Bounds>();
        string[] pitNames = { "Spike_01", "Spike_02" };
        foreach (string pitName in pitNames)
        {
            GameObject pit = GameObject.Find(pitName);
            if (pit == null)
                continue;

            foreach (Renderer renderer in pit.GetComponentsInChildren<Renderer>(true))
            {
                Bounds spikeBounds = renderer.bounds;
                spikeBounds.Expand(new Vector3(0.8f, 0f, 0f));
                // The floor directly below each spike is intentionally lethal.
                spikeBounds.min = new Vector3(spikeBounds.min.x, spikeBounds.min.y - 8f, spikeBounds.min.z);
                spikeBounds.max = new Vector3(spikeBounds.max.x, spikeBounds.max.y + 1.2f, spikeBounds.max.z);
                lethalBounds.Add(spikeBounds);
            }
        }

        trapSurfaces.RemoveWhere(surface =>
        {
            Vector3 world = ground.GetCellCenterWorld(new Vector3Int(surface.x, surface.y, 0));
            world.y += 0.5f;
            foreach (Bounds bounds in lethalBounds)
                if (world.x >= bounds.min.x && world.x <= bounds.max.x &&
                    world.y >= bounds.min.y && world.y <= bounds.max.y)
                    return true;
            return false;
        });
    }

    private static Vector2Int FindRightmostReachable(
        HashSet<Vector2Int> reachable,
        Tilemap ground,
        float bossWorldX)
    {
        Vector2Int best = new Vector2Int(int.MinValue, int.MinValue);
        float bestWorldX = float.MinValue;
        foreach (Vector2Int surface in reachable)
        {
            float worldX = ground.GetCellCenterWorld(new Vector3Int(surface.x, surface.y, 0)).x;
            if (worldX > bossWorldX + 2f || worldX <= bestWorldX)
                continue;
            bestWorldX = worldX;
            best = surface;
        }
        return best;
    }

    private static Vector2Int FindNextTraversalTarget(
        HashSet<Vector2Int> surfaces,
        HashSet<Vector2Int> reachable,
        Vector2Int frontier)
    {
        Vector2Int best = new Vector2Int(int.MinValue, int.MinValue);
        float bestScore = float.MaxValue;
        foreach (Vector2Int surface in surfaces)
        {
            if (reachable.Contains(surface) || surface.x <= frontier.x)
                continue;

            int deltaX = surface.x - frontier.x;
            int deltaY = Mathf.Abs(surface.y - frontier.y);
            float score = deltaX + deltaY * 0.8f;
            if (score >= bestScore)
                continue;
            bestScore = score;
            best = surface;
        }
        return best;
    }

    private static void AddTraversalConnection(
        Tilemap route,
        Tilemap ground,
        Vector2Int from,
        Vector2Int to,
        TileBase[] pattern,
        TileBase fallback,
        bool continuous)
    {
        int direction = to.x >= from.x ? 1 : -1;
        int deltaX = Mathf.Max(1, Mathf.Abs(to.x - from.x));
        int deltaY = to.y - from.y;

        if (deltaX <= 2 && deltaY > 2)
        {
            // Keep this crossing high enough that the spike art remains fully visible below it.
            int stepX = from.x - direction * 2;
            int stepY = FindFreeTraversalPlatformY(ground, route, stepX, from.y + 4);
            PlaceTraversalPlatform(route, ground, stepX, stepY, 3, pattern, fallback);
            return;
        }

        int horizontalStep = continuous ? 1 : 3;
        int steps = Mathf.Max(
            Mathf.CeilToInt(deltaX / (float)horizontalStep),
            Mathf.CeilToInt(Mathf.Abs(deltaY) / 2f));
        steps = Mathf.Max(2, steps);

        int lastX = from.x;
        int lastY = from.y;
        for (int step = 1; step < steps; step++)
        {
            float t = step / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
            int desiredY = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));
            int y = Mathf.Clamp(desiredY, lastY - 2, lastY + 2);
            if (direction * (x - lastX) <= 0)
                x = lastX + direction;
            if (direction * (x - to.x) >= 0)
                break;

            y = FindFreeTraversalPlatformY(ground, route, x, y);
            PlaceTraversalPlatform(route, ground, x, y, 3, pattern, fallback);
            lastX = x;
            lastY = y;
        }
    }

    private static Vector2Int FindTrapClusterCenter(HashSet<Vector2Int> traps)
    {
        Vector2Int seed = new Vector2Int(int.MaxValue, int.MinValue);
        foreach (Vector2Int trap in traps)
            if (trap.x < seed.x)
                seed = trap;

        List<Vector2Int> cluster = new List<Vector2Int>();
        foreach (Vector2Int trap in traps)
            if (trap.y == seed.y && trap.x >= seed.x && trap.x <= seed.x + 12)
                cluster.Add(trap);
        cluster.Sort((a, b) => a.x.CompareTo(b.x));
        return cluster[cluster.Count / 2];
    }

    private static Vector2Int FindNearestSafeSurface(
        HashSet<Vector2Int> reachable,
        HashSet<Vector2Int> traps,
        Vector2Int origin)
    {
        Vector2Int best = new Vector2Int(int.MinValue, int.MinValue);
        float bestScore = float.MaxValue;
        foreach (Vector2Int surface in reachable)
        {
            if (traps.Contains(surface) || surface == origin)
                continue;
            int deltaX = Mathf.Abs(surface.x - origin.x);
            int deltaY = surface.y - origin.y;
            if (deltaX == 0 || deltaX > 18 || deltaY < 0)
                continue;
            float score = deltaX + Mathf.Abs(deltaY) * 0.75f;
            if (score >= bestScore)
                continue;
            bestScore = score;
            best = surface;
        }
        return best;
    }

    private static void PlaceTrapEscapePlatform(
        Tilemap route,
        Tilemap ground,
        HashSet<Vector2Int> traps,
        Vector2Int clusterCenter,
        Vector2Int safeTarget,
        TileBase[] pattern,
        TileBase fallback)
    {
        List<Vector2Int> cluster = new List<Vector2Int>();
        foreach (Vector2Int trap in traps)
            if (trap.y == clusterCenter.y && Mathf.Abs(trap.x - clusterCenter.x) <= 8)
                cluster.Add(trap);
        cluster.Sort((a, b) => a.x.CompareTo(b.x));
        if (cluster.Count == 0)
            return;

        int width = Mathf.Min(3, cluster.Count);
        bool exitIsLeft = safeTarget.x < clusterCenter.x;
        int centerX = exitIsLeft
            ? cluster[0].x + Mathf.Min(2, cluster.Count - 1)
            : cluster[cluster.Count - 1].x - Mathf.Min(2, cluster.Count - 1);
        int startX = centerX - width / 2;
        int platformY = FindFreeTraversalPlatformY(ground, route, centerX, clusterCenter.y + 3);

        for (int i = 0; i < width; i++)
        {
            Vector3Int cell = new Vector3Int(startX + i, platformY, 0);
            if (ground.HasTile(cell))
                continue;
            route.SetTile(cell, PickPlatformTile(pattern, fallback, i, width));
        }
    }

    private static int FindFreeTraversalPlatformY(Tilemap ground, Tilemap route, int centerX, int desiredY)
    {
        int y = desiredY;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            bool blocked = false;
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (ground.HasTile(cell) || ground.HasTile(cell + Vector3Int.up) || route.HasTile(cell + Vector3Int.up))
                {
                    blocked = true;
                    break;
                }
            }
            if (!blocked)
                return y;
            y++;
        }
        return y;
    }

    private static int FindFreeSingleTraversalY(Tilemap ground, Tilemap route, int cellX, int desiredY)
    {
        int y = desiredY;
        for (int attempt = 0; attempt < 12; attempt++, y++)
        {
            Vector3Int cell = new Vector3Int(cellX, y, 0);
            if (!ground.HasTile(cell) && !route.HasTile(cell) &&
                !ground.HasTile(cell + Vector3Int.up) && !route.HasTile(cell + Vector3Int.up))
                return y;
        }
        return y;
    }

    private static void PlaceTraversalPlatform(
        Tilemap route,
        Tilemap ground,
        int centerX,
        int y,
        int width,
        TileBase[] pattern,
        TileBase fallback)
    {
        int startX = centerX - width / 2;
        for (int i = 0; i < width; i++)
        {
            Vector3Int cell = new Vector3Int(startX + i, y, 0);
            if (ground.HasTile(cell))
                continue;
            route.SetTile(cell, PickPlatformTile(pattern, fallback, i, width));
        }
    }

    private static bool IsTraversalOccupied(
        Tilemap ground, IReadOnlyList<Tilemap> routes, Vector3Int cell)
    {
        if (ground.HasTile(cell))
            return true;
        foreach (Tilemap route in routes)
            if (route != null && route.HasTile(cell))
                return true;
        return false;
    }

    private static bool IsTraversalRouteName(string objectName)
    {
        return objectName == "Autumn_Archery_Route_Tilemap" ||
               objectName == "[Autumn_Distinct_Terrain_Pass]";
    }

    private static Tilemap FindTraversalRoute()
    {
        Tilemap fallback = null;
        foreach (Tilemap tilemap in UnityEngine.Object.FindObjectsOfType<Tilemap>(true))
        {
            if (tilemap.gameObject.name == "Autumn_Archery_Route_Tilemap")
                return tilemap;
            if (tilemap.gameObject.name == "[Autumn_Distinct_Terrain_Pass]")
                fallback = tilemap;
        }
        return fallback;
    }

    private static List<Tilemap> FindTraversalRoutes()
    {
        List<Tilemap> routes = new List<Tilemap>();
        foreach (Tilemap tilemap in UnityEngine.Object.FindObjectsOfType<Tilemap>(true))
            if (IsTraversalRouteName(tilemap.gameObject.name))
                routes.Add(tilemap);
        return routes;
    }

    private static bool IsTraversalStepAllowed(
        Vector2Int from,
        Vector2Int to,
        int maxJumpCells,
        int maxRiseCells,
        int maxDropCells)
    {
        int deltaX = Mathf.Abs(to.x - from.x);
        int deltaY = to.y - from.y;
        bool walkableStep = deltaX <= 1 && deltaY >= -2 && deltaY <= 1;
        bool jumpable = deltaX <= maxJumpCells && deltaY <= maxRiseCells && deltaY >= -maxDropCells;
        return walkableStep || jumpable;
    }

    private static Vector2Int FindNearestSurface(HashSet<Vector2Int> surfaces, Vector3Int target)
    {
        Vector2Int nearest = new Vector2Int(int.MinValue, int.MinValue);
        float bestDistance = float.MaxValue;
        foreach (Vector2Int surface in surfaces)
        {
            float distance = Mathf.Abs(surface.x - target.x) + Mathf.Abs(surface.y - target.y) * 0.5f;
            if (distance >= bestDistance)
                continue;
            bestDistance = distance;
            nearest = surface;
        }
        return nearest;
    }
}
