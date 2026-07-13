using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class AutumnDistinctVisualPassTool
{
    private const string ScenePath = "Assets/Scenes/AutumnRuins.unity";
    private const string RootName = "[Autumn_Distinct_Visual_Pass]";
    private const string TerrainRootName = "[Autumn_Distinct_Terrain_Pass]";
    private const string GroundReskinName = "[Autumn_Ground_Visual_Reskin]";
    private const string EmeraldGrassName = "[Autumn_Emerald_Grass_Visual]";
    private const string ExtraEnemiesName = "[Autumn_Extra_Enemies]";
    private const string MushroomEnemyPrefabPath = "Assets/Prefab/Mushroom_Enemy.prefab";
    private const string EmeraldGrassShaderPath = "Assets/Shaders/AutumnEmeraldGrass.shader";
    private const string EmeraldGrassMaterialPath = "Assets/Materials/AutumnEmeraldGrass.mat";
    private const string QueuedGroundReskinPath = "Temp/AutumnGroundReskinApply.request";
    private const string QueuedGroundReskinDonePath = "Temp/AutumnGroundReskinApply.done";
    private const string QueuedTerrainCommandPath = "Temp/AutumnTerrainCommand.request";
    private const string QueuedTerrainCommandDonePath = "Temp/AutumnTerrainCommand.done";
    private static bool isProcessingGroundReskinRequest;
    private const string LegacyCaveGatePath = "Assets/Sprites/MoiTruong/treasure cave entrance.png";
    private const string CaveGatePath = LegacyCaveGatePath;
    private const string ReplacementCaveGatePath =
        "Assets/Sprites/MoiTruong/Xuan/craftpix-net-926878-free-platformer-game-tileset-pixel-art/PNG/cave_entrance.png";
    private const string CaveReplacementChildName = "[Autumn_Cave_Replacement_Visual]";

    static AutumnDistinctVisualPassTool()
    {
        EditorApplication.update -= ProcessQueuedGroundReskin;
        EditorApplication.update += ProcessQueuedGroundReskin;
    }

    private static void ProcessQueuedGroundReskin()
    {
        if (isProcessingGroundReskinRequest)
            return;

        string groundRequest = ProjectPath(QueuedGroundReskinPath);
        string terrainRequest = ProjectPath(QueuedTerrainCommandPath);
        if (File.Exists(terrainRequest))
        {
            isProcessingGroundReskinRequest = true;
            string command = File.ReadAllText(terrainRequest).Trim();
            File.Delete(terrainRequest);
            EditorApplication.delayCall += () =>
            {
                try
                {
                    if (command == "log-islands")
                        LogGroundIslandCandidates();
                    else if (command == "apply")
                        ApplyTerrainPieces();
                    else
                        throw new InvalidOperationException("Unknown Autumn terrain command: " + command);
                    File.WriteAllText(ProjectPath(QueuedTerrainCommandDonePath), DateTime.Now.ToString("O"));
                }
                catch (Exception exception)
                {
                    File.WriteAllText(ProjectPath(QueuedTerrainCommandDonePath), exception.ToString());
                    Debug.LogException(exception);
                }
                finally
                {
                    isProcessingGroundReskinRequest = false;
                }
            };
            return;
        }

        if (!File.Exists(groundRequest))
            return;

        isProcessingGroundReskinRequest = true;
        File.Delete(groundRequest);
        EditorApplication.delayCall += () =>
        {
            try
            {
                ApplyGroundReskinAndExtraEnemies();
                File.WriteAllText(ProjectPath(QueuedGroundReskinDonePath), DateTime.Now.ToString("O"));
            }
            catch (Exception exception)
            {
                File.WriteAllText(ProjectPath(QueuedGroundReskinDonePath), exception.ToString());
                Debug.LogException(exception);
            }
            finally
            {
                isProcessingGroundReskinRequest = false;
            }
        };
    }

    private static string ProjectPath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed class ZoneStyle
    {
        public string Name;
        public float Start;
        public float End;
        public string BackgroundPath;
        public Color Tint;
        public float BackgroundY;
    }

    private sealed class TerrainStamp
    {
        public readonly List<Vector3Int> Cells = new List<Vector3Int>();
        public int Width;
        public int Height;
        public int SourceMinX;
        public int SourceMinY;
    }

    [MenuItem("Tools/Autumn/Log Ground Island Candidates")]
    public static void LogGroundIslandCandidates()
    {
        OpenAutumnScene();
        Tilemap ground = FindTilemap("Ground");
        if (ground == null)
            throw new InvalidOperationException("Ground tilemap is missing.");

        List<TerrainStamp> stamps = FindTerrainStampCandidates(ground);
        string message = "[AutumnTerrainCandidates] count=" + stamps.Count;
        for (int i = 0; i < Mathf.Min(20, stamps.Count); i++)
        {
            TerrainStamp stamp = stamps[i];
            message += $" | #{i + 1}: tiles={stamp.Cells.Count}, size={stamp.Width}x{stamp.Height}, " +
                       $"source=({stamp.SourceMinX},{stamp.SourceMinY}), shape={GetStampShape(stamp)}";
        }
        Tilemap placedTerrain = FindTilemap(TerrainRootName);
        if (placedTerrain != null)
        {
            List<TerrainStamp> placed = FindTerrainStampCandidates(placedTerrain);
            message += " | placed=" + placed.Count;
            foreach (TerrainStamp island in placed)
            {
                Vector3 world = placedTerrain.GetCellCenterWorld(new Vector3Int(
                    island.SourceMinX + island.Width / 2,
                    island.SourceMinY + island.Height - 1,
                    0));
                message += $" ({island.SourceMinX},{island.SourceMinY})@({world.x:0.0},{world.y:0.0})";
            }
        }
        Debug.Log(message);
    }

    private static string GetStampShape(TerrainStamp stamp)
    {
        HashSet<Vector3Int> cells = new HashSet<Vector3Int>(stamp.Cells);
        string shape = string.Empty;
        for (int y = stamp.Height - 1; y >= 0; y--)
        {
            if (shape.Length > 0)
                shape += "/";
            for (int x = 0; x < stamp.Width; x++)
            {
                Vector3Int source = new Vector3Int(stamp.SourceMinX + x, stamp.SourceMinY + y, 0);
                shape += cells.Contains(source) ? "#" : ".";
            }
        }
        return shape;
    }

    [MenuItem("Tools/Autumn/Apply Distinct Visual Pass")]
    public static void Apply()
    {
        OpenAutumnScene();
        Tilemap ground = FindTilemap("Ground");
        if (ground == null)
            throw new InvalidOperationException("Autumn Ground tilemap is missing.");

        RemoveExistingPass();

        GameObject root = new GameObject(RootName);
        GameObject backgrounds = CreateChild(root.transform, "01_Four_Visual_Biomes");
        GameObject landmarks = CreateChild(root.transform, "02_Ruin_And_Cave_Landmarks");

        BoundsInt cells = ground.cellBounds;
        float minX = ground.CellToWorld(new Vector3Int(cells.xMin, 0, 0)).x;
        float maxX = ground.CellToWorld(new Vector3Int(cells.xMax, 0, 0)).x;
        float centerY = Camera.main != null ? Camera.main.transform.position.y + 1.2f : 1.8f;

        ZoneStyle[] zones = BuildZoneStyles(centerY);
        for (int i = 0; i < zones.Length; i++)
            CreateVisualBiome(backgrounds.transform, zones[i], minX, maxX, i);

        CreateLandmarks(landmarks.transform, ground, minX, maxX);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Validate();
        Debug.Log(
            "[AutumnDistinctVisual] PASS | zones=4 | visual biomes=Golden Grove, Crimson Ruins, " +
            "Shadow Ravine, Dragon Dusk | gameplay colliders added=0");
    }

    [MenuItem("Tools/Autumn/Remove Distinct Visual Pass")]
    public static void Remove()
    {
        OpenAutumnScene();
        RemoveExistingPass();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[AutumnDistinctVisual] Visual pass removed. Gameplay objects were untouched.");
    }

    [MenuItem("Tools/Autumn/Apply Distinct Terrain Pieces")]
    public static void ApplyTerrainPieces()
    {
        OpenAutumnScene();
        Tilemap ground = FindTilemap("Ground");
        Tilemap existingRoute = FindTilemap("Autumn_Archery_Route_Tilemap");
        if (ground == null)
            throw new InvalidOperationException("Ground tilemap is missing.");

        RemoveTerrainPass();
        TerrainStamp fullStamp = FindTerrainStampCandidates(ground).Find(candidate =>
            candidate.Width == 6 && candidate.Height == 2 && candidate.Cells.Count == 12);
        if (fullStamp == null)
            throw new InvalidOperationException("No complete 6x2 terrain island could be reused.");
        TerrainStamp bridgeStamp = CropTerrainStamp(fullStamp, 1, 0, 4, 2);

        GameObject terrainObject = new GameObject(TerrainRootName);
        terrainObject.layer = LayerMask.NameToLayer("Ground");
        terrainObject.tag = "Ground";
        terrainObject.transform.SetParent(ground.transform.parent, false);
        terrainObject.transform.localPosition = ground.transform.localPosition;
        terrainObject.transform.localRotation = ground.transform.localRotation;
        terrainObject.transform.localScale = ground.transform.localScale;

        Tilemap terrain = terrainObject.AddComponent<Tilemap>();
        TilemapRenderer terrainRenderer = terrainObject.AddComponent<TilemapRenderer>();
        terrainRenderer.sortingOrder = 2;
        terrainRenderer.sharedMaterial = GetOrCreateEmeraldGrassMaterial();
        TilemapCollider2D terrainCollider = terrainObject.AddComponent<TilemapCollider2D>();
        terrainCollider.usedByEffector = true;
        PlatformEffector2D effector = terrainObject.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 160f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;

        List<Bounds> blockedBounds = CollectTerrainBlockedBounds();
        int placed = TryPlaceTerrainStampAt(
            ground, existingRoute, terrain, bridgeStamp, blockedBounds,
            targetMinX: 35, targetMinY: -6, floorY: -7) ? 1 : 0;
        if (placed == 0)
            throw new InvalidOperationException("Critical 4x2 spike-side bridge could not be placed.");
        if (TryPlaceTerrainStamp(
                ground, existingRoute, terrain, fullStamp, blockedBounds,
                desiredCenterX: 121, desiredTopOffset: 4))
            placed++;
        if (TryPlaceTerrainStamp(
                ground, existingRoute, terrain, fullStamp, blockedBounds,
                desiredCenterX: 199, desiredTopOffset: 3))
            placed++;

        if (placed != 3)
            throw new InvalidOperationException(
                $"Only {placed}/3 logical terrain islands could be placed.");

        terrain.RefreshAllTiles();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        ValidateTerrainPieces();
        Debug.Log(
            $"[AutumnDistinctTerrain] PASS | completeIslands={placed} | islandShapes=4x2,6x2,6x2 | " +
            "oneWay=True | cavesAndSpikesClear=True");
    }

    [MenuItem("Tools/Autumn/Remove Distinct Terrain Pieces")]
    public static void RemoveTerrainPieces()
    {
        OpenAutumnScene();
        RemoveTerrainPass();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[AutumnDistinctTerrain] Terrain pass removed.");
    }

    [MenuItem("Tools/Autumn/Apply Ground Reskin And Extra Enemies")]
    public static void ApplyGroundReskinAndExtraEnemies()
    {
        OpenAutumnScene();
        Tilemap ground = FindTilemap("Ground");
        if (ground == null)
            throw new InvalidOperationException("Ground tilemap is missing.");

        RemoveGroundReskinAndExtraEnemies();
        CreateGroundVisualReskin(ground);
        ApplyEmeraldMaterialToAutumnPlatforms();
        CreateExtraEnemies(ground);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        ValidateGroundReskinAndExtraEnemies(ground);
    }

    [MenuItem("Tools/Autumn/Remove Ground Reskin And Extra Enemies")]
    public static void RemoveGroundReskinAndExtraEnemiesFromMenu()
    {
        OpenAutumnScene();
        RemoveGroundReskinAndExtraEnemies();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[AutumnGroundReskin] Removed. Original Ground renderer restored.");
    }

    private static void CreateGroundVisualReskin(Tilemap ground)
    {
        TilemapRenderer originalRenderer = ground.GetComponent<TilemapRenderer>();
        if (originalRenderer == null)
            throw new InvalidOperationException("Ground TilemapRenderer is missing.");

        GameObject visualObject = new GameObject(GroundReskinName);
        visualObject.transform.SetParent(ground.transform.parent, false);
        visualObject.transform.localPosition = ground.transform.localPosition;
        visualObject.transform.localRotation = ground.transform.localRotation;
        visualObject.transform.localScale = ground.transform.localScale;

        Tilemap visual = visualObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = visualObject.AddComponent<TilemapRenderer>();
        renderer.sortingLayerID = originalRenderer.sortingLayerID;
        renderer.sortingOrder = originalRenderer.sortingOrder;
        renderer.sharedMaterial = originalRenderer.sharedMaterial;

        GameObject emeraldObject = new GameObject(EmeraldGrassName);
        emeraldObject.transform.SetParent(ground.transform.parent, false);
        emeraldObject.transform.localPosition = ground.transform.localPosition;
        emeraldObject.transform.localRotation = ground.transform.localRotation;
        emeraldObject.transform.localScale = ground.transform.localScale;
        Tilemap emeraldVisual = emeraldObject.AddComponent<Tilemap>();
        TilemapRenderer emeraldRenderer = emeraldObject.AddComponent<TilemapRenderer>();
        emeraldRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        emeraldRenderer.sortingOrder = originalRenderer.sortingOrder;
        emeraldRenderer.sharedMaterial = GetOrCreateEmeraldGrassMaterial();

        BoundsInt bounds = ground.cellBounds;
        HashSet<Vector3Int> emeraldSurfaceCells = BuildEmeraldSurfaceCells(ground, bounds);
        int copied = 0;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = ground.GetTile(cell);
            if (tile == null)
                continue;

            Tilemap target = emeraldSurfaceCells.Contains(cell) ? emeraldVisual : visual;
            target.SetTile(cell, tile);
            target.SetTransformMatrix(cell, ground.GetTransformMatrix(cell));
            copied++;
        }

        visual.CompressBounds();
        visual.RefreshAllTiles();
        emeraldVisual.CompressBounds();
        emeraldVisual.RefreshAllTiles();
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (visual.HasTile(cell))
            {
                visual.SetTileFlags(cell, TileFlags.None);
                visual.SetColor(cell, GetAutumnGroundColor(cell, bounds));
            }
            if (emeraldVisual.HasTile(cell))
            {
                emeraldVisual.SetTileFlags(cell, TileFlags.None);
                emeraldVisual.SetColor(cell, Color.white);
            }
        }
        originalRenderer.enabled = false;
        EditorUtility.SetDirty(originalRenderer);
        Debug.Log($"[AutumnGroundReskin] Copied {copied} cells; original collider preserved.");
    }

    private static HashSet<Vector3Int> BuildEmeraldSurfaceCells(Tilemap ground, BoundsInt bounds)
    {
        HashSet<Vector3Int> emerald = new HashSet<Vector3Int>();
        foreach (Vector3Int cell in bounds.allPositionsWithin)
            if (ground.HasTile(cell))
                emerald.Add(cell);
        return emerald;
    }

    private static void ApplyEmeraldMaterialToAutumnPlatforms()
    {
        Material material = GetOrCreateEmeraldGrassMaterial();
        string[] platformNames = { "Autumn_Archery_Route_Tilemap", TerrainRootName };
        foreach (string platformName in platformNames)
        {
            Tilemap platform = FindTilemap(platformName);
            TilemapRenderer renderer = platform != null ? platform.GetComponent<TilemapRenderer>() : null;
            if (renderer == null)
                continue;
            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static Material GetOrCreateEmeraldGrassMaterial()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(EmeraldGrassShaderPath);
        if (shader == null)
            throw new InvalidOperationException("Emerald grass shader is missing: " + EmeraldGrassShaderPath);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(EmeraldGrassMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "AutumnEmeraldGrass" };
            AssetDatabase.CreateAsset(material, EmeraldGrassMaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    private static Color GetAutumnGroundColor(Vector3Int cell, BoundsInt bounds)
    {
        float progress = Mathf.InverseLerp(bounds.xMin, bounds.xMax - 1, cell.x);
        Color zoneColor;
        if (progress < 0.28f)
            zoneColor = new Color(1f, 0.86f, 0.54f, 1f);
        else if (progress < 0.56f)
            zoneColor = new Color(1f, 0.67f, 0.42f, 1f);
        else if (progress < 0.79f)
            zoneColor = new Color(0.78f, 0.57f, 0.5f, 1f);
        else
            zoneColor = new Color(0.68f, 0.46f, 0.54f, 1f);

        int variation = Mathf.Abs(cell.x * 17 + cell.y * 31) % 5;
        float brightness = 0.9f + variation * 0.025f;
        return new Color(
            Mathf.Clamp01(zoneColor.r * brightness),
            Mathf.Clamp01(zoneColor.g * brightness),
            Mathf.Clamp01(zoneColor.b * brightness),
            1f);
    }

    private static void CreateExtraEnemies(Tilemap ground)
    {
        StealthEnemy stealthSource = UnityEngine.Object.FindObjectOfType<StealthEnemy>(true);
        GameObject mushroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MushroomEnemyPrefabPath);
        if (stealthSource == null || mushroomPrefab == null)
            throw new InvalidOperationException("Existing Autumn stealth enemy or mushroom prefab is missing.");

        GameObject root = new GameObject(ExtraEnemiesName);
        BoundsInt bounds = ground.cellBounds;
        List<Bounds> hazards = CollectHazardBounds();
        float[] stealthFractions = { 0.37f, 0.67f };
        float[] mushroomFractions = { 0.48f, 0.76f };

        for (int i = 0; i < stealthFractions.Length; i++)
        {
            int cellX = FindSafeEnemyColumn(ground, hazards, bounds, stealthFractions[i]);
            float surfaceY = FindSurfaceY(ground, cellX);
            StealthEnemy enemy = UnityEngine.Object.Instantiate(stealthSource, root.transform);
            enemy.name = "StealthEnemy_Autumn_Extra_" + (i + 1);
            enemy.gameObject.SetActive(true);
            PlaceColliderBottomAt(enemy.gameObject, ground.GetCellCenterWorld(new Vector3Int(cellX, 0, 0)).x, surfaceY);
        }

        for (int i = 0; i < mushroomFractions.Length; i++)
        {
            int cellX = FindSafeEnemyColumn(ground, hazards, bounds, mushroomFractions[i]);
            float surfaceY = FindSurfaceY(ground, cellX);
            GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(mushroomPrefab, root.transform);
            enemy.name = "Mushroom_Autumn_Extra_" + (i + 1);
            enemy.SetActive(true);
            PlaceColliderBottomAt(
                enemy,
                ground.GetCellCenterWorld(new Vector3Int(cellX, 0, 0)).x,
                surfaceY);
        }
    }

    private static int FindSafeEnemyColumn(
        Tilemap ground, List<Bounds> hazards, BoundsInt bounds, float fraction)
    {
        int desired = bounds.xMin + Mathf.RoundToInt(bounds.size.x * fraction);
        for (int radius = 0; radius <= 16; radius++)
        {
            int[] candidates = radius == 0
                ? new[] { desired }
                : new[] { desired + radius, desired - radius };
            foreach (int x in candidates)
            {
                float surfaceY = FindSurfaceY(ground, x);
                float worldX = ground.GetCellCenterWorld(new Vector3Int(x, 0, 0)).x;
                bool nearHazard = hazards.Exists(h =>
                    worldX >= h.min.x - 1.8f && worldX <= h.max.x + 1.8f &&
                    surfaceY >= h.min.y - 1f && surfaceY <= h.max.y + 2f);
                Vector3Int topCell = new Vector3Int(x, ground.WorldToCell(new Vector3(worldX, surfaceY, 0f)).y - 1, 0);
                if (!nearHazard && ground.HasTile(topCell) && !ground.HasTile(topCell + Vector3Int.up))
                    return x;
            }
        }
        throw new InvalidOperationException("Could not find a safe ground column for an extra enemy.");
    }

    private static void PlaceColliderBottomAt(GameObject target, float x, float surfaceY)
    {
        target.transform.position = new Vector3(x, surfaceY + 2f, target.transform.position.z);
        Physics2D.SyncTransforms();
        Collider2D collider = target.GetComponent<Collider2D>();
        if (collider == null)
            throw new InvalidOperationException("Extra ground enemy has no Collider2D: " + target.name);
        target.transform.position += Vector3.up * (surfaceY - collider.bounds.min.y + 0.03f);
        Rigidbody2D body = target.GetComponent<Rigidbody2D>();
        if (body != null)
            body.velocity = Vector2.zero;
    }

    private static void ValidateGroundReskinAndExtraEnemies(Tilemap ground)
    {
        GameObject visualObject = GameObject.Find(GroundReskinName);
        GameObject emeraldObject = GameObject.Find(EmeraldGrassName);
        GameObject enemies = GameObject.Find(ExtraEnemiesName);
        Tilemap visual = visualObject != null ? visualObject.GetComponent<Tilemap>() : null;
        Tilemap emeraldVisual = emeraldObject != null ? emeraldObject.GetComponent<Tilemap>() : null;
        TilemapRenderer originalRenderer = ground.GetComponent<TilemapRenderer>();
        if (visual == null || emeraldVisual == null ||
            visual.GetComponent<Collider2D>() != null || emeraldVisual.GetComponent<Collider2D>() != null ||
            originalRenderer.enabled)
            throw new InvalidOperationException("Ground reskin isolation validation failed.");

        Material emeraldMaterial = emeraldVisual.GetComponent<TilemapRenderer>().sharedMaterial;
        if (emeraldMaterial == null || emeraldMaterial.shader == null ||
            emeraldMaterial.shader.name != "ProjectDeadCell/Autumn Emerald Grass")
            throw new InvalidOperationException("Emerald grass material is not configured correctly.");

        int sourceCells = 0;
        int visualCells = 0;
        foreach (Vector3Int cell in ground.cellBounds.allPositionsWithin)
        {
            if (ground.HasTile(cell)) sourceCells++;
            bool hasBaseVisual = visual.HasTile(cell);
            bool hasEmeraldVisual = emeraldVisual.HasTile(cell);
            if (hasBaseVisual && hasEmeraldVisual)
                throw new InvalidOperationException("Ground visual layers overlap at " + cell);
            if (hasBaseVisual || hasEmeraldVisual) visualCells++;
        }
        if (sourceCells != visualCells)
            throw new InvalidOperationException($"Ground reskin cell mismatch: {sourceCells} != {visualCells}");

        int stealthCount = enemies != null ? enemies.GetComponentsInChildren<StealthEnemy>(true).Length : 0;
        int mushroomCount = enemies != null ? enemies.GetComponentsInChildren<EnemyController>(true).Length : 0;
        if (stealthCount != 2 || mushroomCount != 2)
            throw new InvalidOperationException("Extra Autumn enemy count validation failed.");

        int emeraldTileCells = 0;
        foreach (Vector3Int cell in ground.cellBounds.allPositionsWithin)
            if (emeraldVisual.HasTile(cell))
                emeraldTileCells++;
        if (emeraldTileCells != sourceCells)
            throw new InvalidOperationException(
                $"Emerald shader must cover every Ground tile: {emeraldTileCells}/{sourceCells} cells.");

        string[] platformNames = { "Autumn_Archery_Route_Tilemap", TerrainRootName };
        foreach (string platformName in platformNames)
        {
            Tilemap platform = FindTilemap(platformName);
            TilemapRenderer platformRenderer = platform != null ? platform.GetComponent<TilemapRenderer>() : null;
            if (platformRenderer != null &&
                (platformRenderer.sharedMaterial == null ||
                 platformRenderer.sharedMaterial.shader == null ||
                 platformRenderer.sharedMaterial.shader.name != "ProjectDeadCell/Autumn Emerald Grass"))
                throw new InvalidOperationException(platformName + " is not using the emerald grass material.");
        }

        Debug.Log(
            $"[AutumnGroundReskinValidation] PASS | cells={sourceCells} | colliderChanges=0 | " +
            $"emeraldGroundTiles={emeraldTileCells}/{sourceCells} | extraStealth={stealthCount} | " +
            $"extraMushrooms={mushroomCount}");
    }

    private static void RemoveGroundReskinAndExtraEnemies()
    {
        GameObject visual = GameObject.Find(GroundReskinName);
        if (visual != null)
            UnityEngine.Object.DestroyImmediate(visual);
        GameObject emeraldVisual = GameObject.Find(EmeraldGrassName);
        if (emeraldVisual != null)
            UnityEngine.Object.DestroyImmediate(emeraldVisual);
        GameObject enemies = GameObject.Find(ExtraEnemiesName);
        if (enemies != null)
            UnityEngine.Object.DestroyImmediate(enemies);

        Tilemap ground = FindTilemap("Ground");
        TilemapRenderer renderer = ground != null ? ground.GetComponent<TilemapRenderer>() : null;
        if (renderer != null)
        {
            renderer.enabled = true;
            EditorUtility.SetDirty(renderer);
        }
    }

    [MenuItem("Tools/Autumn/Replace Legacy Cave Visuals")]
    public static void ReplaceLegacyCaveVisuals()
    {
        OpenAutumnScene();
        Sprite replacement = AssetDatabase.LoadAssetAtPath<Sprite>(ReplacementCaveGatePath);
        if (replacement == null)
            throw new InvalidOperationException("Replacement cave sprite is missing: " + ReplacementCaveGatePath);

        List<SpriteRenderer> legacyRenderers = new List<SpriteRenderer>();
        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
            if (renderer.sprite != null && AssetDatabase.GetAssetPath(renderer.sprite) == LegacyCaveGatePath)
                legacyRenderers.Add(renderer);

        int replaced = 0;
        foreach (SpriteRenderer legacyRenderer in legacyRenderers)
        {
            Transform existing = legacyRenderer.transform.Find(CaveReplacementChildName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            legacyRenderer.enabled = true;
            Bounds oldBounds = legacyRenderer.bounds;

            GameObject visual = CreateChild(legacyRenderer.transform, CaveReplacementChildName);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            float parentScaleY = Mathf.Max(0.001f, Mathf.Abs(legacyRenderer.transform.lossyScale.y));
            float targetHeight = Mathf.Min(oldBounds.size.y, 3.8f);
            float scale = targetHeight /
                          Mathf.Max(0.001f, replacement.bounds.size.y * parentScaleY);
            visual.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer newRenderer = visual.AddComponent<SpriteRenderer>();
            newRenderer.sprite = replacement;
            newRenderer.color = legacyRenderer.color;
            newRenderer.sharedMaterial = legacyRenderer.sharedMaterial;
            newRenderer.sortingLayerID = legacyRenderer.sortingLayerID;
            newRenderer.sortingOrder = legacyRenderer.sortingOrder;

            Physics2D.SyncTransforms();
            Bounds newBounds = newRenderer.bounds;
            visual.transform.position += new Vector3(
                oldBounds.center.x - newBounds.center.x,
                oldBounds.min.y - newBounds.min.y,
                0f);

            legacyRenderer.enabled = false;
            EditorUtility.SetDirty(legacyRenderer);
            EditorUtility.SetDirty(newRenderer);
            replaced++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        ValidateCaveVisuals();
        Debug.Log($"[AutumnCaveReplacement] PASS | replacedLegacyCaves={replaced} | triggersPreserved=True");
    }

    [MenuItem("Tools/Autumn/Restore Legacy Cave Visuals")]
    public static void RestoreLegacyCaveVisuals()
    {
        OpenAutumnScene();
        Sprite legacySprite = AssetDatabase.LoadAssetAtPath<Sprite>(LegacyCaveGatePath);
        if (legacySprite == null)
            throw new InvalidOperationException("Legacy cave sprite is missing: " + LegacyCaveGatePath);

        int restoredCoreCaves = 0;
        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            if (renderer == null || renderer.sprite == null ||
                AssetDatabase.GetAssetPath(renderer.sprite) != LegacyCaveGatePath)
                continue;
            Transform replacement = renderer.transform.Find(CaveReplacementChildName);
            if (replacement != null)
                UnityEngine.Object.DestroyImmediate(replacement.gameObject);
            if (!renderer.enabled)
            {
                renderer.enabled = true;
                EditorUtility.SetDirty(renderer);
                restoredCoreCaves++;
            }
        }

        int restoredDecorativeGates = 0;
        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            if (renderer == null || renderer.sprite == null ||
                AssetDatabase.GetAssetPath(renderer.sprite) != ReplacementCaveGatePath ||
                !renderer.gameObject.name.StartsWith("Ruin_Gate_", StringComparison.Ordinal))
                continue;

            Bounds oldBounds = renderer.bounds;
            renderer.sprite = legacySprite;
            Physics2D.SyncTransforms();
            float heightRatio = oldBounds.size.y / Mathf.Max(0.001f, renderer.bounds.size.y);
            renderer.transform.localScale = new Vector3(
                renderer.transform.localScale.x * heightRatio,
                renderer.transform.localScale.y * heightRatio,
                renderer.transform.localScale.z);
            Physics2D.SyncTransforms();
            Bounds newBounds = renderer.bounds;
            renderer.transform.position += new Vector3(
                oldBounds.center.x - newBounds.center.x,
                oldBounds.min.y - newBounds.min.y,
                0f);
            EditorUtility.SetDirty(renderer);
            restoredDecorativeGates++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        ValidateLegacyCaveVisuals();
        Debug.Log(
            $"[AutumnCaveRollback] PASS | coreCaves={restoredCoreCaves} | " +
            $"decorativeGates={restoredDecorativeGates} | triggersPreserved=True");
    }

    public static void ValidateLegacyCaveVisuals()
    {
        OpenAutumnScene();
        int legacyVisible = 0;
        int replacementVisible = 0;
        int replacementChildren = 0;
        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            if (renderer.sprite == null || !renderer.enabled)
                continue;
            string path = AssetDatabase.GetAssetPath(renderer.sprite);
            if (path == LegacyCaveGatePath)
                legacyVisible++;
            if (path == ReplacementCaveGatePath)
                replacementVisible++;
        }
        foreach (Transform transform in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            if (transform.name == CaveReplacementChildName)
                replacementChildren++;

        if (legacyVisible < 4 || replacementVisible != 0 || replacementChildren != 0)
            throw new InvalidOperationException(
                $"Cave rollback is incomplete: legacy={legacyVisible}, replacement={replacementVisible}, " +
                $"replacementChildren={replacementChildren}");

        Debug.Log($"[AutumnCaveRollbackValidation] PASS | legacyVisible={legacyVisible} | replacementVisible=0");
    }

    public static void ValidateCaveVisuals()
    {
        OpenAutumnScene();
        int legacyVisible = 0;
        int replacements = 0;
        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            if (renderer.sprite == null)
                continue;
            string path = AssetDatabase.GetAssetPath(renderer.sprite);
            if (path == LegacyCaveGatePath && renderer.enabled)
                legacyVisible++;
            if (path == ReplacementCaveGatePath && renderer.enabled)
                replacements++;
        }

        if (legacyVisible != 0)
            throw new InvalidOperationException("A legacy moss cave renderer is still visible.");
        if (replacements < 5)
            throw new InvalidOperationException("Not all Autumn cave visuals were replaced: " + replacements);

        foreach (Transform transform in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            if (transform.name == CaveReplacementChildName &&
                transform.GetComponent<Collider2D>() != null)
                throw new InvalidOperationException("Replacement cave child must remain visual-only.");

        Debug.Log($"[AutumnCaveValidation] PASS | newCaveRenderers={replacements} | legacyVisible=0");
    }

    public static void Validate()
    {
        OpenAutumnScene();
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            throw new InvalidOperationException("Distinct Autumn visual root is missing.");

        Transform zoneRoot = root.transform.Find("01_Four_Visual_Biomes");
        if (zoneRoot == null || zoneRoot.childCount != 4)
            throw new InvalidOperationException("The four Autumn visual biomes were not created correctly.");

        int renderers = root.GetComponentsInChildren<SpriteRenderer>(true).Length;
        if (renderers < 12)
            throw new InvalidOperationException("Autumn visual pass does not contain enough layered renderers.");
        if (root.GetComponentsInChildren<Collider2D>(true).Length != 0 ||
            root.GetComponentsInChildren<Rigidbody2D>(true).Length != 0)
            throw new InvalidOperationException("Visual-only Autumn pass contains a gameplay physics component.");

        string[] expectedZones = { "Golden_Grove", "Crimson_Ruins", "Shadow_Ravine", "Dragon_Dusk" };
        foreach (string expectedZone in expectedZones)
            if (zoneRoot.Find(expectedZone) == null)
                throw new InvalidOperationException("Missing Autumn visual biome: " + expectedZone);

        Debug.Log($"[AutumnDistinctVisualValidation] PASS | zones=4 | renderers={renderers} | colliders=0");
    }

    public static void ValidateTerrainPieces()
    {
        OpenAutumnScene();
        Tilemap terrain = FindTilemap(TerrainRootName);
        Tilemap ground = FindTilemap("Ground");
        if (terrain == null)
            throw new InvalidOperationException("Distinct Autumn terrain tilemap is missing.");
        if (ground == null)
            throw new InvalidOperationException("Ground tilemap is missing during terrain validation.");

        TilemapCollider2D collider = terrain.GetComponent<TilemapCollider2D>();
        PlatformEffector2D effector = terrain.GetComponent<PlatformEffector2D>();
        if (collider == null || effector == null || !collider.usedByEffector || !effector.useOneWay)
            throw new InvalidOperationException("Distinct terrain is not configured as one-way ground.");

        int tileCount = 0;
        foreach (Vector3Int cell in terrain.cellBounds.allPositionsWithin)
            if (terrain.HasTile(cell))
                tileCount++;
        if (tileCount != 32)
            throw new InvalidOperationException(
                "Distinct terrain must contain one 4x2 bridge and two 6x2 islands: " + tileCount);

        List<TerrainStamp> islands = FindTerrainStampCandidates(terrain);
        if (islands.Count != 3)
            throw new InvalidOperationException("Distinct terrain must contain exactly three separated islands: " + islands.Count);
        int narrowBridges = 0;
        int wideIslands = 0;
        foreach (TerrainStamp island in islands)
        {
            bool isNarrowBridge = island.Width == 4 && island.Height == 2 && island.Cells.Count == 8;
            bool isWideIsland = island.Width == 6 && island.Height == 2 && island.Cells.Count == 12;
            if (!isNarrowBridge && !isWideIsland)
                throw new InvalidOperationException(
                    $"Incomplete terrain island detected: {island.Width}x{island.Height}, tiles={island.Cells.Count}");
            if (isNarrowBridge) narrowBridges++;
            if (isWideIsland) wideIslands++;
        }
        if (narrowBridges != 1 || wideIslands != 2)
            throw new InvalidOperationException(
                $"Terrain shape balance is incorrect: narrow={narrowBridges}, wide={wideIslands}.");
        bool hasCriticalBridge = islands.Exists(island =>
            island.SourceMinX == 35 && island.SourceMinY == -6 && island.Width == 4);
        if (!hasCriticalBridge)
            throw new InvalidOperationException("The spike-side bridge is not in its validated position.");

        List<Bounds> blockedBounds = CollectTerrainBlockedBounds();
        Vector3 cellSize = Vector3.Scale(terrain.layoutGrid.cellSize, terrain.transform.lossyScale);
        foreach (Vector3Int cell in terrain.cellBounds.allPositionsWithin)
        {
            if (!terrain.HasTile(cell))
                continue;
            if (ground.HasTile(cell))
                throw new InvalidOperationException("Distinct terrain overlaps original Ground at " + cell);
            Bounds tileBounds = new Bounds(
                terrain.GetCellCenterWorld(cell),
                new Vector3(Mathf.Abs(cellSize.x) * 0.9f, Mathf.Abs(cellSize.y) * 0.9f, 0.1f));
            foreach (Bounds blocked in blockedBounds)
                if (tileBounds.Intersects(blocked))
                    throw new InvalidOperationException(
                        "Distinct terrain overlaps a cave/gate/hazard at " + blocked.center);
        }

        Debug.Log(
            $"[AutumnDistinctTerrainValidation] PASS | islands=3 | tiles={tileCount} | " +
            "shapes=4x2+6x2+6x2 | criticalGapBridged=True | cavesAndSpikesClear=True | colliders=one-way");
    }

    public static void CapturePreviews()
    {
        OpenAutumnScene();
        Tilemap ground = FindTilemap("Ground");
        Camera camera = Camera.main;
        if (ground == null || camera == null)
            throw new InvalidOperationException("Autumn Ground or Main Camera is missing.");

        List<SpriteRenderer> bossRenderers = new List<SpriteRenderer>();
        GameObject boss = GameObject.Find("[BossAutumnShadowDragon]");
        if (boss != null)
        {
            foreach (SpriteRenderer renderer in boss.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.enabled)
                    continue;
                renderer.enabled = false;
                bossRenderers.Add(renderer);
            }
        }

        BoundsInt cells = ground.cellBounds;
        float minX = ground.CellToWorld(new Vector3Int(cells.xMin, 0, 0)).x;
        float maxX = ground.CellToWorld(new Vector3Int(cells.xMax, 0, 0)).x;
        float[] fractions = { 0.12f, 0.37f, 0.64f, 0.88f };
        Vector3 originalPosition = camera.transform.position;
        RenderTexture originalTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        string folder = Path.GetFullPath("Logs/AutumnDistinctPreviews");
        Directory.CreateDirectory(folder);

        RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        for (int i = 0; i < fractions.Length; i++)
        {
            float x = Mathf.Lerp(minX, maxX, fractions[i]);
            int cellX = ground.WorldToCell(new Vector3(x, 0f, 0f)).x;
            float y = FindSurfaceY(ground, cellX) + 2.5f;
            camera.transform.position = new Vector3(x, y, -10f);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(folder, "Autumn_Distinct_Zone_" + (i + 1) + ".png"), image.EncodeToPNG());
        }

        camera.transform.position = originalPosition;
        camera.targetTexture = originalTarget;
        RenderTexture.active = previousActive;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        foreach (SpriteRenderer renderer in bossRenderers)
            renderer.enabled = true;

        Debug.Log("[AutumnDistinctVisual] Preview images written to " + folder);
    }

    public static void CaptureTerrainPreviews()
    {
        OpenAutumnScene();
        Tilemap terrain = FindTilemap(TerrainRootName);
        Camera camera = Camera.main;
        if (terrain == null || camera == null)
            throw new InvalidOperationException("Autumn terrain pass or Main Camera is missing.");

        List<TerrainStamp> islands = FindTerrainStampCandidates(terrain);
        islands.Sort((left, right) => left.SourceMinX.CompareTo(right.SourceMinX));
        string folder = Path.GetFullPath("Logs/AutumnTerrainPreviews");
        Directory.CreateDirectory(folder);
        Vector3 originalPosition = camera.transform.position;
        RenderTexture originalTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(960, 540, TextureFormat.RGB24, false);

        for (int i = 0; i < islands.Count; i++)
        {
            TerrainStamp island = islands[i];
            Vector3 center = terrain.GetCellCenterWorld(new Vector3Int(
                island.SourceMinX + island.Width / 2,
                island.SourceMinY + island.Height - 1,
                0));
            camera.transform.position = new Vector3(center.x, center.y + 0.8f, -10f);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(
                Path.Combine(folder, $"Autumn_Terrain_Island_{i + 1}.png"), image.EncodeToPNG());
        }

        camera.transform.position = originalPosition;
        camera.targetTexture = originalTarget;
        RenderTexture.active = previousActive;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        Debug.Log("[AutumnDistinctTerrain] Preview images written to " + folder);
    }

    private static ZoneStyle[] BuildZoneStyles(float centerY)
    {
        const string outdoor = "Assets/Sprites/MoiTruong/Thu/Background_Outdoor";
        const string craft = "Assets/Sprites/MoiTruong/Thu/CraftPix_Autumn_Backgrounds_PNG";
        const string cave = "Assets/Sprites/MoiTruong/Thu/Background_Cave";

        return new[]
        {
            new ZoneStyle
            {
                Name = "Golden_Grove", Start = 0f, End = 0.25f,
                BackgroundPath = outdoor + "/background 1.png",
                Tint = new Color(1f, 0.96f, 0.82f, 1f),
                BackgroundY = centerY
            },
            new ZoneStyle
            {
                Name = "Crimson_Ruins", Start = 0.24f, End = 0.52f,
                BackgroundPath = craft + "/background 2/background 2.png",
                Tint = new Color(1f, 0.76f, 0.62f, 1f),
                BackgroundY = centerY + 0.2f
            },
            new ZoneStyle
            {
                Name = "Shadow_Ravine", Start = 0.51f, End = 0.78f,
                BackgroundPath = cave + "/background 3.png",
                Tint = new Color(0.62f, 0.55f, 0.58f, 1f),
                BackgroundY = centerY + 0.35f
            },
            new ZoneStyle
            {
                Name = "Dragon_Dusk", Start = 0.77f, End = 1f,
                BackgroundPath = craft + "/background 4/background 4.png",
                Tint = new Color(1f, 0.72f, 0.48f, 1f),
                BackgroundY = centerY + 0.1f
            }
        };
    }

    private static void CreateVisualBiome(
        Transform backgroundParent,
        ZoneStyle style,
        float mapMinX,
        float mapMaxX,
        int zoneIndex)
    {
        float startX = Mathf.Lerp(mapMinX, mapMaxX, style.Start);
        float endX = Mathf.Lerp(mapMinX, mapMaxX, style.End);
        GameObject zone = CreateChild(backgroundParent, style.Name);

        CreateRepeatedSpriteStrip(
            zone.transform,
            "Backdrop",
            style.BackgroundPath,
            startX,
            endX,
            style.BackgroundY,
            28f,
            -16 + zoneIndex,
            style.Tint,
            0.015f + zoneIndex * 0.01f);
    }

    private static void CreateRepeatedSpriteStrip(
        Transform parent,
        string layerName,
        string assetPath,
        float minX,
        float maxX,
        float centerY,
        float targetHeight,
        int sortingOrder,
        Color tint,
        float parallax)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
            throw new InvalidOperationException("Missing existing Autumn sprite: " + assetPath);

        GameObject layer = CreateChild(parent, layerName);
        ParallaxBackground parallaxBackground = layer.AddComponent<ParallaxBackground>();
        parallaxBackground.parallaxMultiplier = parallax;

        float scale = targetHeight / Mathf.Max(0.01f, sprite.bounds.size.y);
        float tileWidth = sprite.bounds.size.x * scale;
        int count = Mathf.CeilToInt((maxX - minX) / tileWidth) + 2;
        float firstX = minX - tileWidth * 0.35f;

        for (int i = 0; i < count; i++)
        {
            GameObject tile = CreateChild(layer.transform, layerName + "_" + (i + 1));
            tile.transform.position = new Vector3(firstX + i * tileWidth * 0.995f, centerY, 10f);
            tile.transform.localScale = new Vector3(scale * 1.01f, scale * 1.01f, 1f);
            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private static void CreateLandmarks(Transform parent, Tilemap ground, float minX, float maxX)
    {
        Sprite caveGate = AssetDatabase.LoadAssetAtPath<Sprite>(CaveGatePath);
        if (caveGate == null)
            throw new InvalidOperationException("Existing cave gate sprite is missing: " + CaveGatePath);

        float[] fractions = { 0.31f, 0.73f };
        float[] heights = { 3.4f, 3.8f };
        Color[] colors =
        {
            new Color(0.72f, 0.44f, 0.22f, 0.9f),
            new Color(0.22f, 0.16f, 0.2f, 0.96f)
        };

        for (int i = 0; i < fractions.Length; i++)
        {
            float x = Mathf.Lerp(minX, maxX, fractions[i]);
            int cellX = ground.WorldToCell(new Vector3(x, 0f, 0f)).x;
            float surfaceY = FindSurfaceY(ground, cellX);
            float scale = heights[i] / Mathf.Max(0.01f, caveGate.bounds.size.y);

            GameObject gate = CreateChild(parent, "Ruin_Gate_" + (i + 1));
            gate.transform.position = new Vector3(
                x,
                surfaceY - caveGate.bounds.min.y * scale - 0.12f,
                0f);
            gate.transform.localScale = new Vector3(i % 2 == 0 ? scale : -scale, scale, 1f);

            SpriteRenderer renderer = gate.AddComponent<SpriteRenderer>();
            renderer.sprite = caveGate;
            renderer.color = colors[i];
            renderer.sortingOrder = -4;
        }
    }

    private static List<TerrainStamp> FindTerrainStampCandidates(Tilemap ground)
    {
        HashSet<Vector3Int> remaining = new HashSet<Vector3Int>();
        foreach (Vector3Int cell in ground.cellBounds.allPositionsWithin)
            if (ground.HasTile(cell))
                remaining.Add(cell);

        List<TerrainStamp> candidates = new List<TerrainStamp>();
        Vector3Int[] directions = { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };
        while (remaining.Count > 0)
        {
            Vector3Int seed = default;
            foreach (Vector3Int cell in remaining)
            {
                seed = cell;
                break;
            }

            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            List<Vector3Int> component = new List<Vector3Int>();
            queue.Enqueue(seed);
            remaining.Remove(seed);
            int minX = seed.x;
            int maxX = seed.x;
            int minY = seed.y;
            int maxY = seed.y;

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                component.Add(current);
                minX = Mathf.Min(minX, current.x);
                maxX = Mathf.Max(maxX, current.x);
                minY = Mathf.Min(minY, current.y);
                maxY = Mathf.Max(maxY, current.y);
                foreach (Vector3Int direction in directions)
                {
                    Vector3Int neighbor = current + direction;
                    if (!remaining.Remove(neighbor))
                        continue;
                    queue.Enqueue(neighbor);
                }
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            if (component.Count < 4 || component.Count > 80 ||
                width < 2 || width > 10 || height < 2 || height > 7)
                continue;

            TerrainStamp stamp = new TerrainStamp
            {
                Width = width,
                Height = height,
                SourceMinX = minX,
                SourceMinY = minY
            };
            stamp.Cells.AddRange(component);
            candidates.Add(stamp);
        }

        candidates.Sort((left, right) =>
        {
            float leftDensity = left.Cells.Count / (float)(left.Width * left.Height);
            float rightDensity = right.Cells.Count / (float)(right.Width * right.Height);
            int densityComparison = rightDensity.CompareTo(leftDensity);
            return densityComparison != 0
                ? densityComparison
                : right.Cells.Count.CompareTo(left.Cells.Count);
        });
        return candidates;
    }

    private static TerrainStamp CropTerrainStamp(
        TerrainStamp source, int localMinX, int localMinY, int width, int height)
    {
        TerrainStamp cropped = new TerrainStamp
        {
            Width = width,
            Height = height,
            SourceMinX = source.SourceMinX + localMinX,
            SourceMinY = source.SourceMinY + localMinY
        };
        foreach (Vector3Int cell in source.Cells)
        {
            int localX = cell.x - source.SourceMinX;
            int localY = cell.y - source.SourceMinY;
            if (localX >= localMinX && localX < localMinX + width &&
                localY >= localMinY && localY < localMinY + height)
                cropped.Cells.Add(cell);
        }
        if (cropped.Cells.Count != width * height)
            throw new InvalidOperationException("Cropped terrain stamp is not a complete rectangle.");
        return cropped;
    }

    private static bool TryPlaceTerrainStamp(
        Tilemap ground,
        Tilemap existingRoute,
        Tilemap terrain,
        TerrainStamp stamp,
        List<Bounds> blockedBounds,
        int desiredCenterX,
        int desiredTopOffset)
    {
        int[] horizontalSearch = { 0, 2, -2, 4, -4, 6, -6, 8, -8, 12, -12, 16, -16 };
        int[] heightSearch = { 0, -1, 1 };
        foreach (int xOffset in horizontalSearch)
        {
            int centerX = desiredCenterX + xOffset;
            int floorY = FindSolidSurfaceCellY(ground, centerX);
            Vector3Int floorCell = new Vector3Int(centerX, floorY, 0);
            if (!ground.HasTile(floorCell) || ground.HasTile(floorCell + Vector3Int.up))
                continue;

            foreach (int heightOffset in heightSearch)
            {
                int topY = floorY + desiredTopOffset + heightOffset;
                int targetMinX = centerX - stamp.Width / 2;
                int targetMinY = topY - stamp.Height + 1;
                if (!CanPlaceTerrainStamp(
                        ground, existingRoute, terrain, stamp, blockedBounds,
                        targetMinX, targetMinY, floorY))
                    continue;
                StampTerrainTiles(ground, terrain, stamp, targetMinX, targetMinY);
                return true;
            }
        }
        return false;
    }

    private static bool TryPlaceTerrainStampAt(
        Tilemap ground,
        Tilemap existingRoute,
        Tilemap terrain,
        TerrainStamp stamp,
        List<Bounds> blockedBounds,
        int targetMinX,
        int targetMinY,
        int floorY)
    {
        if (!CanPlaceTerrainStamp(
                ground, null, terrain, stamp, blockedBounds,
                targetMinX, targetMinY, floorY))
        {
            LogTerrainStampBlockers(
                ground, terrain, stamp, blockedBounds, targetMinX, targetMinY, floorY);
            return false;
        }
        if (existingRoute != null)
        {
            foreach (Vector3Int sourceCell in stamp.Cells)
            {
                int localX = sourceCell.x - stamp.SourceMinX;
                int localY = sourceCell.y - stamp.SourceMinY;
                existingRoute.SetTile(
                    new Vector3Int(targetMinX + localX, targetMinY + localY, 0), null);
            }
            existingRoute.RefreshAllTiles();
        }
        StampTerrainTiles(ground, terrain, stamp, targetMinX, targetMinY);
        return true;
    }

    private static void LogTerrainStampBlockers(
        Tilemap ground,
        Tilemap terrain,
        TerrainStamp stamp,
        List<Bounds> blockedBounds,
        int targetMinX,
        int targetMinY,
        int floorY)
    {
        List<string> reasons = new List<string>();
        int topY = targetMinY + stamp.Height - 1;
        int jumpHeight = topY - floorY;
        if (jumpHeight < 2 || jumpHeight > 4)
            reasons.Add("jumpHeight=" + jumpHeight);
        Vector3 cellSize = Vector3.Scale(ground.layoutGrid.cellSize, ground.transform.lossyScale);
        foreach (Vector3Int sourceCell in stamp.Cells)
        {
            Vector3Int target = new Vector3Int(
                targetMinX + sourceCell.x - stamp.SourceMinX,
                targetMinY + sourceCell.y - stamp.SourceMinY,
                0);
            if (ground.HasTile(target)) reasons.Add("Ground@" + target);
            if (terrain.HasTile(target)) reasons.Add("Terrain@" + target);
            Bounds tileBounds = new Bounds(
                ground.GetCellCenterWorld(target),
                new Vector3(Mathf.Abs(cellSize.x) * 0.92f, Mathf.Abs(cellSize.y) * 0.92f, 0.1f));
            foreach (Bounds blocked in blockedBounds)
                if (tileBounds.Intersects(blocked))
                    reasons.Add($"blocked@{target}->{blocked.center}");
        }
        Debug.LogWarning("[AutumnTerrainPlacementBlocked] " + string.Join(", ", reasons));
    }

    private static void StampTerrainTiles(
        Tilemap ground, Tilemap terrain, TerrainStamp stamp, int targetMinX, int targetMinY)
    {
        foreach (Vector3Int sourceCell in stamp.Cells)
        {
            int localX = sourceCell.x - stamp.SourceMinX;
            int localY = sourceCell.y - stamp.SourceMinY;
            Vector3Int targetCell = new Vector3Int(targetMinX + localX, targetMinY + localY, 0);
            terrain.SetTile(targetCell, ground.GetTile(sourceCell));
            terrain.SetTransformMatrix(targetCell, ground.GetTransformMatrix(sourceCell));
        }
    }

    private static bool CanPlaceTerrainStamp(
        Tilemap ground,
        Tilemap existingRoute,
        Tilemap terrain,
        TerrainStamp stamp,
        List<Bounds> blockedBounds,
        int targetMinX,
        int targetMinY,
        int floorY)
    {
        int targetTopY = targetMinY + stamp.Height - 1;
        int jumpHeight = targetTopY - floorY;
        if (jumpHeight < 2 || jumpHeight > 4)
            return false;

        Vector3 cellSize = Vector3.Scale(ground.layoutGrid.cellSize, ground.transform.lossyScale);
        foreach (Vector3Int sourceCell in stamp.Cells)
        {
            int localX = sourceCell.x - stamp.SourceMinX;
            int localY = sourceCell.y - stamp.SourceMinY;
            Vector3Int targetCell = new Vector3Int(targetMinX + localX, targetMinY + localY, 0);
            if (ground.HasTile(targetCell) || terrain.HasTile(targetCell) ||
                (existingRoute != null && existingRoute.HasTile(targetCell)))
                return false;

            for (int clearance = 1; clearance <= 2; clearance++)
                if (ground.HasTile(new Vector3Int(targetCell.x, targetTopY + clearance, 0)) ||
                    terrain.HasTile(new Vector3Int(targetCell.x, targetTopY + clearance, 0)) ||
                    (existingRoute != null &&
                     existingRoute.HasTile(new Vector3Int(targetCell.x, targetTopY + clearance, 0))))
                    return false;

            Bounds tileBounds = new Bounds(
                ground.GetCellCenterWorld(targetCell),
                new Vector3(Mathf.Abs(cellSize.x) * 0.92f, Mathf.Abs(cellSize.y) * 0.92f, 0.1f));
            foreach (Bounds blocked in blockedBounds)
                if (tileBounds.Intersects(blocked))
                    return false;
        }
        return true;
    }

    private static List<Bounds> CollectTerrainBlockedBounds()
    {
        List<Bounds> blocked = CollectHazardBounds();
        foreach (Renderer renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
        {
            if (!renderer.enabled || !HasBlockingTerrainName(renderer.transform))
                continue;
            Bounds bounds = renderer.bounds;
            bounds.Expand(new Vector3(1.2f, 0.8f, 0f));
            blocked.Add(bounds);
        }
        return blocked;
    }

    private static bool HasBlockingTerrainName(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            string lower = current.name.ToLowerInvariant();
            if (lower.Contains("cave") || lower.Contains("ruin_gate") ||
                lower.Contains("boss_gate"))
                return true;
            current = current.parent;
        }
        return false;
    }

    private static bool TryPlaceTerrainPiece(
        Tilemap ground,
        Tilemap existingRoute,
        Tilemap terrain,
        TileBase[] pattern,
        List<Bounds> hazards,
        int desiredX,
        int width,
        int height)
    {
        int[] xOffsets = { 0, 3, -3, 6, -6 };
        int[] yOffsets = { 0, 1, -1, 2 };
        foreach (int xOffset in xOffsets)
        {
            int centerX = desiredX + xOffset;
            int floorY = FindSolidSurfaceCellY(ground, centerX);
            foreach (int yOffset in yOffsets)
            {
                int platformY = floorY + height + yOffset;
                if (!CanPlaceTerrainPiece(
                        ground, existingRoute, terrain, hazards, centerX, platformY, width))
                    continue;

                int startX = centerX - width / 2;
                for (int i = 0; i < width; i++)
                {
                    TileBase tile = PickPatternTile(pattern, i, width);
                    terrain.SetTile(new Vector3Int(startX + i, platformY, 0), tile);
                }
                return true;
            }
        }
        return false;
    }

    private static bool CanPlaceTerrainPiece(
        Tilemap ground,
        Tilemap existingRoute,
        Tilemap terrain,
        List<Bounds> hazards,
        int centerX,
        int y,
        int width)
    {
        int startX = centerX - width / 2;
        Vector3 cellSize = Vector3.Scale(ground.layoutGrid.cellSize, ground.transform.lossyScale);
        for (int i = 0; i < width; i++)
        {
            int x = startX + i;
            Vector3Int cell = new Vector3Int(x, y, 0);
            for (int clearance = 0; clearance <= 2; clearance++)
            {
                Vector3Int check = cell + Vector3Int.up * clearance;
                if (ground.HasTile(check) ||
                    (existingRoute != null && existingRoute.HasTile(check)) ||
                    terrain.HasTile(check))
                    return false;
            }

            float worldX = ground.GetCellCenterWorld(cell).x;
            foreach (Bounds hazard in hazards)
                if (worldX >= hazard.min.x - 0.8f && worldX <= hazard.max.x + 0.8f)
                    return false;

            Bounds tileBounds = new Bounds(
                ground.GetCellCenterWorld(cell),
                new Vector3(Mathf.Abs(cellSize.x) * 0.9f, Mathf.Abs(cellSize.y) * 0.9f, 0.1f));
            foreach (Bounds hazard in hazards)
                if (tileBounds.Intersects(hazard))
                    return false;
        }
        return true;
    }

    private static int FindSolidSurfaceCellY(Tilemap ground, int cellX)
    {
        BoundsInt bounds = ground.cellBounds;
        for (int y = bounds.yMax - 1; y > bounds.yMin; y--)
        {
            Vector3Int cell = new Vector3Int(cellX, y, 0);
            if (ground.HasTile(cell) && ground.HasTile(cell + Vector3Int.down) &&
                !ground.HasTile(cell + Vector3Int.up))
                return y;
        }
        return ground.WorldToCell(new Vector3(ground.GetCellCenterWorld(new Vector3Int(cellX, -3, 0)).x, -3f, 0f)).y;
    }

    private static int FindSurfaceCellYBelow(Tilemap ground, int cellX, int exclusiveMaxY)
    {
        BoundsInt bounds = ground.cellBounds;
        for (int y = Mathf.Min(exclusiveMaxY - 1, bounds.yMax - 1); y >= bounds.yMin; y--)
        {
            Vector3Int cell = new Vector3Int(cellX, y, 0);
            if (ground.HasTile(cell) && !ground.HasTile(cell + Vector3Int.up))
                return y;
        }
        return bounds.yMin - 1;
    }

    private static TileBase[] FindPlatformPattern(Tilemap route, Tilemap ground)
    {
        TileBase[] fromRoute = route != null ? FindTileRun(route) : Array.Empty<TileBase>();
        return fromRoute.Length > 0 ? fromRoute : FindTileRun(ground);
    }

    private static TileBase[] FindTileRun(Tilemap tilemap)
    {
        BoundsInt bounds = tilemap.cellBounds;
        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
        {
            List<TileBase> run = new List<TileBase>();
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                TileBase tile = tilemap.GetTile(new Vector3Int(x, y, 0));
                if (tile != null)
                {
                    run.Add(tile);
                    continue;
                }
                if (run.Count >= 3 && run.Count <= 6)
                    return run.ToArray();
                run.Clear();
            }
            if (run.Count >= 3 && run.Count <= 6)
                return run.ToArray();
        }
        return Array.Empty<TileBase>();
    }

    private static TileBase PickPatternTile(TileBase[] pattern, int index, int length)
    {
        if (pattern.Length == 1)
            return pattern[0];
        if (index == 0)
            return pattern[0];
        if (index == length - 1)
            return pattern[pattern.Length - 1];
        return pattern[Mathf.Min(1, pattern.Length - 1)];
    }

    private static List<Bounds> CollectHazardBounds()
    {
        List<Bounds> hazards = new List<Bounds>();
        foreach (Transform transform in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            string lowerName = transform.name.ToLowerInvariant();
            if (!lowerName.Contains("spike") && !lowerName.Contains("thorn"))
                continue;
            Renderer renderer = transform.GetComponentInChildren<Renderer>();
            Collider2D collider = transform.GetComponentInChildren<Collider2D>();
            if (renderer != null)
                hazards.Add(renderer.bounds);
            else if (collider != null)
                hazards.Add(collider.bounds);
        }
        return hazards;
    }

    private static float FindSurfaceY(Tilemap ground, int cellX)
    {
        BoundsInt bounds = ground.cellBounds;
        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
        {
            Vector3Int cell = new Vector3Int(cellX, y, 0);
            if (ground.HasTile(cell) && !ground.HasTile(cell + Vector3Int.up))
                return ground.CellToWorld(cell + Vector3Int.up).y;
        }
        return ground.CellToWorld(new Vector3Int(cellX, -3, 0)).y;
    }

    private static Tilemap FindTilemap(string objectName)
    {
        foreach (Tilemap tilemap in UnityEngine.Object.FindObjectsOfType<Tilemap>(true))
            if (tilemap.gameObject.name == objectName)
                return tilemap;
        return null;
    }

    private static void OpenAutumnScene()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void RemoveExistingPass()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    private static void RemoveTerrainPass()
    {
        GameObject existing = GameObject.Find(TerrainRootName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }
}
