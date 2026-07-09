using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Non-destructive enhancer for the Autumn (Màn 3) map.
///
/// Adds handcrafted seasonal set-dressing on top of whatever is already in
/// AutumnRuins.unity: layered autumn trees, fallen-leaf piles, drifting leaves,
/// rocks, bushes, grass tufts, warm sun shafts, ground moss/leaf scatter and
/// atmospheric mist bands. Everything it creates is parented under a single
/// root object ("Autumn_Enhancements") and every generated object carries a
/// name prefix, so a second run cleans up its own previous output first and
/// NEVER touches existing gameplay objects, terrain, enemies or hazards.
///
/// Menu: Tools > Autumn > Enhance Autumn Map (Visual Only)
/// </summary>
public static class AutumnMapEnhancer
{
    private const string ScenePath = "Assets/Scenes/AutumnRuins.unity";
    private const string RootName = "Autumn_Enhancements";
    private const string Prefix = "AE_"; // marks every generated object

    // Autumn palette ------------------------------------------------------
    private static readonly Color TrunkDark = new Color(0.26f, 0.16f, 0.10f);
    private static readonly Color TrunkMid = new Color(0.34f, 0.22f, 0.13f);
    private static readonly Color[] FoliageColors =
    {
        new Color(0.86f, 0.35f, 0.10f), // burnt orange
        new Color(0.93f, 0.55f, 0.13f), // amber
        new Color(0.74f, 0.20f, 0.08f), // deep red
        new Color(0.90f, 0.70f, 0.18f), // golden yellow
        new Color(0.62f, 0.30f, 0.12f), // russet
    };
    private static readonly Color[] LeafColors =
    {
        new Color(0.95f, 0.48f, 0.13f),
        new Color(0.80f, 0.24f, 0.08f),
        new Color(0.97f, 0.72f, 0.20f),
        new Color(0.66f, 0.34f, 0.12f),
        new Color(0.55f, 0.18f, 0.06f),
    };
    private static readonly Color RockColor = new Color(0.34f, 0.30f, 0.27f);
    private static readonly Color RockShade = new Color(0.24f, 0.21f, 0.19f);
    private static readonly Color BushColor = new Color(0.55f, 0.28f, 0.11f);
    private static readonly Color GrassColor = new Color(0.62f, 0.46f, 0.16f);

    [MenuItem("Tools/Autumn/Enhance Autumn Map (Visual Only)")]
    public static void EnhanceAutumnMap()
    {
        Scene scene = EnsureAutumnSceneOpen();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Autumn Enhancer",
                "Could not open " + ScenePath + ". Aborting so nothing is changed.", "OK");
            return;
        }

        // Deterministic layout so re-runs look identical.
        Random.State prevRandom = Random.state;
        Random.InitState(20261109);

        // Refresh tilemap cache for this run (stale after previous runs / edits).
        _tilemaps = null;
        CacheTilemaps();

        RemovePreviousEnhancements(scene);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Autumn Enhancements");

        GameObject bgTrees = NewGroup(root, Prefix + "Background_Trees");
        GameObject midProps = NewGroup(root, Prefix + "Midground_Props");
        GameObject groundScatter = NewGroup(root, Prefix + "Ground_Scatter");
        GameObject foreground = NewGroup(root, Prefix + "Foreground_Details");
        GameObject atmosphere = NewGroup(root, Prefix + "Atmosphere");

        BuildBackgroundTreeLine(bgTrees.transform);
        BuildMidgroundProps(midProps.transform);
        BuildGroundScatter(groundScatter.transform);
        BuildForegroundDetails(foreground.transform);
        BuildAtmosphere(atmosphere.transform);

        ApplyWarmRenderSettings();

        Random.state = prevRandom;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (_groundHitCount == 0)
        {
            Debug.LogWarning("[AutumnMapEnhancer] No ground tiles were sampled — decor that " +
                "snaps to terrain may be missing. Check that the ground Tilemap is present in the scene.");
        }

        Debug.Log("<color=orange>[AutumnMapEnhancer]</color> Added seasonal set-dressing under '" +
                  RootName + "' (" + _groundHitCount + " ground samples). Existing gameplay " +
                  "untouched. Re-run to regenerate; use 'Remove Autumn Enhancements' to clear.");
    }

    [MenuItem("Tools/Autumn/Remove Autumn Enhancements")]
    public static void RemoveAutumnEnhancements()
    {
        Scene scene = EnsureAutumnSceneOpen();
        if (!scene.IsValid()) return;
        int removed = RemovePreviousEnhancements(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("<color=orange>[AutumnMapEnhancer]</color> Removed " + removed + " enhancement root(s).");
    }

    // ---------------------------------------------------------------------
    // Scene helpers
    // ---------------------------------------------------------------------
    private static Scene EnsureAutumnSceneOpen()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.path == ScenePath)
            return active;

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static int RemovePreviousEnhancements(Scene scene)
    {
        int count = 0;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == RootName)
            {
                Object.DestroyImmediate(go);
                count++;
            }
        }
        return count;
    }

    private static GameObject NewGroup(GameObject parent, string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent.transform);
        g.transform.localPosition = Vector3.zero;
        return g;
    }

    // ---------------------------------------------------------------------
    // Terrain sampling: snap decor onto the real ground surface.
    // ---------------------------------------------------------------------
    private const float MapMinX = -16f;
    private const float MapMaxX = 268f;

    // Cached tilemaps used for deterministic edit-time ground sampling.
    private static UnityEngine.Tilemaps.Tilemap[] _tilemaps;
    private static int _groundHitCount;

    private static void CacheTilemaps()
    {
        _tilemaps = Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>(true);
        _groundHitCount = 0;
    }

    /// <summary>
    /// Finds the ground surface height at worldX by reading actual tilemap data
    /// (works reliably in edit mode, unlike physics queries). Returns false when
    /// there is a pit / no tile in that column, so props are skipped there.
    /// </summary>
    private static bool TryGetGroundY(float worldX, out float surfaceY)
    {
        surfaceY = 0f;
        bool found = false;
        float best = float.NegativeInfinity;

        if (_tilemaps == null)
            CacheTilemaps();

        foreach (var tm in _tilemaps)
        {
            if (tm == null) continue;

            // Convert the query column to this tilemap's cell X.
            int cellX = tm.WorldToCell(new Vector3(worldX, 0f, 0f)).x;

            BoundsInt bounds = tm.cellBounds;
            if (cellX < bounds.xMin || cellX >= bounds.xMax)
                continue;

            // Scan top-down for the highest occupied cell in this column.
            for (int cellY = bounds.yMax - 1; cellY >= bounds.yMin; cellY--)
            {
                if (tm.HasTile(new Vector3Int(cellX, cellY, 0)))
                {
                    // Top surface of that cell in world space.
                    Vector3 top = tm.CellToWorld(new Vector3Int(cellX, cellY + 1, 0));
                    if (top.y > best) best = top.y;
                    found = true;
                    break;
                }
            }
        }

        if (found)
        {
            surfaceY = best;
            _groundHitCount++;
        }
        return found;
    }

    // ---------------------------------------------------------------------
    // 1. Background tree line (far, parallax, behind gameplay)
    // ---------------------------------------------------------------------
    private static void BuildBackgroundTreeLine(Transform parent)
    {
        GameObject layer = new GameObject(Prefix + "TreeLine_Far");
        layer.transform.SetParent(parent);
        ParallaxBackground px = layer.AddComponent<ParallaxBackground>();
        px.parallaxMultiplier = 0.32f;

        int index = 0;
        for (float x = MapMinX; x <= MapMaxX; x += Random.Range(7f, 11f))
        {
            if (!TryGetGroundY(x, out float gy))
                continue;

            float scale = Random.Range(1.4f, 2.3f);
            BuildTree(layer.transform, "Tree_Far_" + index, new Vector2(x, gy), scale,
                sortingBase: -28, tint: 0.55f, z: 6f);
            index++;
        }
    }

    // ---------------------------------------------------------------------
    // 2. Midground props: closer trees, rocks, bushes near the play path
    // ---------------------------------------------------------------------
    private static void BuildMidgroundProps(Transform parent)
    {
        int treeIndex = 0;
        int rockIndex = 0;
        int bushIndex = 0;

        for (float x = MapMinX + 3f; x <= MapMaxX; x += Random.Range(5.5f, 9f))
        {
            if (!TryGetGroundY(x, out float gy))
                continue;

            float roll = Random.value;
            if (roll < 0.45f)
            {
                float scale = Random.Range(0.85f, 1.35f);
                BuildTree(parent, "Tree_Mid_" + treeIndex, new Vector2(x, gy), scale,
                    sortingBase: -8, tint: 0.85f, z: 3f);
                treeIndex++;
            }
            else if (roll < 0.75f)
            {
                BuildRock(parent, "Rock_" + rockIndex, new Vector2(x, gy), Random.Range(0.5f, 1.1f));
                rockIndex++;
            }
            else
            {
                BuildBush(parent, "Bush_" + bushIndex, new Vector2(x, gy), Random.Range(0.7f, 1.2f));
                bushIndex++;
            }
        }
    }

    // ---------------------------------------------------------------------
    // 3. Ground scatter: fallen-leaf carpets, grass tufts
    // ---------------------------------------------------------------------
    private static void BuildGroundScatter(Transform parent)
    {
        int leafIndex = 0;
        int grassIndex = 0;

        for (float x = MapMinX; x <= MapMaxX; x += Random.Range(2.2f, 4f))
        {
            if (!TryGetGroundY(x, out float gy))
                continue;

            BuildLeafCarpet(parent, "LeafCarpet_" + leafIndex, new Vector2(x, gy + 0.08f),
                Random.Range(4, 8));
            leafIndex++;

            if (Random.value < 0.55f)
            {
                BuildGrassTuft(parent, "Grass_" + grassIndex,
                    new Vector2(x + Random.Range(-1f, 1f), gy + 0.12f));
                grassIndex++;
            }
        }
    }

    // ---------------------------------------------------------------------
    // 4. Foreground details: near branches + drifting hero leaves
    // ---------------------------------------------------------------------
    private static void BuildForegroundDetails(Transform parent)
    {
        int branchIndex = 0;
        for (float x = MapMinX + 8f; x <= MapMaxX; x += Random.Range(18f, 30f))
        {
            if (!TryGetGroundY(x, out float gy))
                continue;
            float y = gy + Random.Range(6.5f, 8.5f);
            BuildOverhangBranch(parent, "Branch_" + branchIndex, new Vector2(x, y),
                flip: Random.value > 0.5f);
            branchIndex++;
        }

        int leafIndex = 0;
        for (float x = MapMinX + 5f; x <= MapMaxX; x += Random.Range(12f, 20f))
        {
            float y = Random.Range(0.5f, 4.5f);
            GameObject leaf = MakeSprite(parent, "DriftLeaf_" + leafIndex,
                new Vector3(x, y, -2f),
                new Vector2(0.4f, 0.24f),
                LeafColors[leafIndex % LeafColors.Length],
                sortingOrder: 60);
            leaf.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            AutumnLeafDrift drift = leaf.AddComponent<AutumnLeafDrift>();
            drift.swayAmplitude = Random.Range(0.4f, 0.9f);
            drift.swayFrequency = Random.Range(0.4f, 0.7f);
            drift.spinSpeed = Random.Range(-60f, 60f);
            leafIndex++;
        }
    }

    // ---------------------------------------------------------------------
    // 5. Atmosphere: warm overlay, sun shafts, mist bands, leaf particles
    // ---------------------------------------------------------------------
    private static void BuildAtmosphere(Transform parent)
    {
        MakeSprite(parent, "WarmOverlay",
            new Vector3((MapMinX + MapMaxX) * 0.5f, 1f, -3f),
            new Vector2(MapMaxX - MapMinX + 20f, 22f),
            new Color(1f, 0.52f, 0.16f, 0.07f),
            sortingOrder: 80);

        int shaftIndex = 0;
        for (float x = MapMinX + 6f; x <= MapMaxX; x += Random.Range(20f, 34f))
        {
            GameObject shaft = MakeSprite(parent, "SunShaft_" + shaftIndex,
                new Vector3(x, Random.Range(3f, 5f), -1f),
                new Vector2(Random.Range(1.2f, 2.2f), Random.Range(9f, 13f)),
                new Color(1f, 0.78f, 0.36f, 0.10f),
                sortingOrder: 70);
            shaft.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-22f, -14f));
            shaftIndex++;
        }

        int mistIndex = 0;
        for (float x = MapMinX; x <= MapMaxX; x += Random.Range(14f, 22f))
        {
            if (!TryGetGroundY(x, out float gy))
                gy = 0f;
            GameObject mist = MakeSprite(parent, "Mist_" + mistIndex,
                new Vector3(x, gy + Random.Range(0.6f, 1.6f), -1.5f),
                new Vector2(Random.Range(10f, 16f), Random.Range(1.4f, 2.6f)),
                new Color(0.92f, 0.68f, 0.42f, 0.12f),
                sortingOrder: 74);
            AutumnFogEffect fog = mist.AddComponent<AutumnFogEffect>();
            fog.driftSpeed = Random.Range(0.12f, 0.24f);
            fog.driftRange = Random.Range(2.5f, 4.5f);
            mistIndex++;
        }

        BuildFallingLeafParticles(parent);
    }

    private static void BuildFallingLeafParticles(Transform parent)
    {
        GameObject go = new GameObject(Prefix + "FX_FallingLeaves");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3((MapMinX + MapMaxX) * 0.5f, 9f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.duration = 14f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.54f, 0.10f, 0.8f), new Color(0.7f, 0.22f, 0.06f, 0.6f));
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.02f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 26f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(MapMaxX - MapMinX + 10f, 0.5f, 1f);

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.9f, -0.2f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.7f, -0.3f);

        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.4f;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 65;
    }

    // ---------------------------------------------------------------------
    // Primitive prop builders (all made from simple sprite boxes)
    // ---------------------------------------------------------------------

    /// <summary>
    /// A stylised autumn tree: tapered trunk + layered rounded canopy blobs.
    /// tint (0..1) darkens the whole thing for distance; z pushes it back.
    /// </summary>
    private static void BuildTree(Transform parent, string name, Vector2 basePos,
        float scale, int sortingBase, float tint, float z)
    {
        GameObject tree = new GameObject(Prefix + name);
        tree.transform.SetParent(parent);
        tree.transform.position = new Vector3(basePos.x, basePos.y, z);

        float trunkH = 2.2f * scale;
        float trunkW = 0.35f * scale;

        // Trunk
        MakeSprite(tree.transform, "Trunk",
            new Vector3(basePos.x, basePos.y + trunkH * 0.5f, z),
            new Vector2(trunkW, trunkH),
            Shade(TrunkDark, tint),
            sortingBase);

        // A couple of branches.
        GameObject bL = MakeSprite(tree.transform, "Branch_L",
            new Vector3(basePos.x - 0.3f * scale, basePos.y + trunkH * 0.7f, z),
            new Vector2(trunkW * 0.55f, trunkH * 0.5f),
            Shade(TrunkMid, tint), sortingBase);
        bL.transform.rotation = Quaternion.Euler(0f, 0f, 32f);
        GameObject bR = MakeSprite(tree.transform, "Branch_R",
            new Vector3(basePos.x + 0.3f * scale, basePos.y + trunkH * 0.75f, z),
            new Vector2(trunkW * 0.55f, trunkH * 0.5f),
            Shade(TrunkMid, tint), sortingBase);
        bR.transform.rotation = Quaternion.Euler(0f, 0f, -32f);

        // Canopy: 4-5 overlapping foliage blobs.
        float canopyY = basePos.y + trunkH;
        int blobs = Random.Range(4, 6);
        for (int i = 0; i < blobs; i++)
        {
            Color c = Shade(FoliageColors[Random.Range(0, FoliageColors.Length)], tint);
            float bx = basePos.x + Random.Range(-0.9f, 0.9f) * scale;
            float by = canopyY + Random.Range(-0.2f, 0.9f) * scale;
            float bs = Random.Range(1.1f, 1.8f) * scale;
            MakeSprite(tree.transform, "Foliage_" + i,
                new Vector3(bx, by, z),
                new Vector2(bs, bs * Random.Range(0.75f, 0.95f)),
                c, sortingBase + 1);
        }
    }

    private static void BuildRock(Transform parent, string name, Vector2 basePos, float scale)
    {
        GameObject rock = new GameObject(Prefix + name);
        rock.transform.SetParent(parent);
        rock.transform.position = new Vector3(basePos.x, basePos.y, 2f);

        MakeSprite(rock.transform, "Body",
            new Vector3(basePos.x, basePos.y + 0.3f * scale, 2f),
            new Vector2(1.3f * scale, 0.85f * scale),
            RockColor, -6);
        MakeSprite(rock.transform, "Shade",
            new Vector3(basePos.x + 0.2f * scale, basePos.y + 0.18f * scale, 2f),
            new Vector2(0.7f * scale, 0.5f * scale),
            RockShade, -5);
        // A little moss / leaf on top.
        MakeSprite(rock.transform, "Moss",
            new Vector3(basePos.x - 0.15f * scale, basePos.y + 0.62f * scale, 2f),
            new Vector2(0.55f * scale, 0.18f * scale),
            new Color(0.5f, 0.42f, 0.14f), -4);
    }

    private static void BuildBush(Transform parent, string name, Vector2 basePos, float scale)
    {
        GameObject bush = new GameObject(Prefix + name);
        bush.transform.SetParent(parent);
        bush.transform.position = new Vector3(basePos.x, basePos.y, 2f);

        int blobs = Random.Range(3, 5);
        for (int i = 0; i < blobs; i++)
        {
            Color c = BushColor;
            if (i % 2 == 0) c = FoliageColors[Random.Range(0, FoliageColors.Length)];
            float bx = basePos.x + Random.Range(-0.6f, 0.6f) * scale;
            float by = basePos.y + 0.25f * scale + Random.Range(-0.05f, 0.25f) * scale;
            float bs = Random.Range(0.55f, 0.95f) * scale;
            MakeSprite(bush.transform, "Blob_" + i,
                new Vector3(bx, by, 2f),
                new Vector2(bs, bs * 0.8f),
                c, -3);
        }
    }

    private static void BuildLeafCarpet(Transform parent, string name, Vector2 origin, int count)
    {
        GameObject carpet = new GameObject(Prefix + name);
        carpet.transform.SetParent(parent);
        carpet.transform.position = new Vector3(origin.x, origin.y, 1f);

        for (int i = 0; i < count; i++)
        {
            Color c = LeafColors[Random.Range(0, LeafColors.Length)];
            float x = origin.x + Random.Range(-1.4f, 1.4f);
            float y = origin.y + Random.Range(-0.05f, 0.12f);
            GameObject leaf = MakeSprite(carpet.transform, "Leaf_" + i,
                new Vector3(x, y, 1f),
                new Vector2(Random.Range(0.2f, 0.34f), Random.Range(0.1f, 0.16f)),
                c, 12);
            leaf.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-40f, 40f));
        }
    }

    private static void BuildGrassTuft(Transform parent, string name, Vector2 basePos)
    {
        GameObject tuft = new GameObject(Prefix + name);
        tuft.transform.SetParent(parent);
        tuft.transform.position = new Vector3(basePos.x, basePos.y, 1f);

        int blades = Random.Range(3, 6);
        for (int i = 0; i < blades; i++)
        {
            float x = basePos.x + Random.Range(-0.25f, 0.25f);
            float h = Random.Range(0.3f, 0.6f);
            GameObject blade = MakeSprite(tuft.transform, "Blade_" + i,
                new Vector3(x, basePos.y + h * 0.5f, 1f),
                new Vector2(0.08f, h),
                Color.Lerp(GrassColor, FoliageColors[i % FoliageColors.Length], 0.3f),
                11);
            blade.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));
        }
    }

    private static void BuildOverhangBranch(Transform parent, string name, Vector2 pos, bool flip)
    {
        GameObject branch = new GameObject(Prefix + name);
        branch.transform.SetParent(parent);
        branch.transform.position = new Vector3(pos.x, pos.y, -2.5f);
        float dir = flip ? -1f : 1f;

        GameObject limb = MakeSprite(branch.transform, "Limb",
            new Vector3(pos.x, pos.y, -2.5f),
            new Vector2(3.2f, 0.35f),
            new Color(0.16f, 0.09f, 0.05f),
            sortingOrder: 62);
        limb.transform.rotation = Quaternion.Euler(0f, 0f, dir * -12f);

        // Hanging foliage clumps.
        for (int i = 0; i < 4; i++)
        {
            Color c = FoliageColors[Random.Range(0, FoliageColors.Length)];
            float bx = pos.x + dir * (0.6f + i * 0.7f);
            float by = pos.y - 0.3f - Random.Range(0f, 0.3f);
            float bs = Random.Range(0.7f, 1.1f);
            MakeSprite(branch.transform, "Clump_" + i,
                new Vector3(bx, by, -2.5f),
                new Vector2(bs, bs * 0.8f),
                c, sortingOrder: 61);
        }
    }

    // ---------------------------------------------------------------------
    // Low-level sprite box helper
    // ---------------------------------------------------------------------
    private static GameObject MakeSprite(Transform parent, string name, Vector3 position,
        Vector2 size, Color color, int sortingOrder)
    {
        GameObject go = new GameObject(Prefix + name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetBuiltinSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    private static Color Shade(Color c, float tint)
    {
        return new Color(c.r * tint, c.g * tint, c.b * tint, c.a);
    }

    private static Sprite _builtinSprite;
    private static Sprite GetBuiltinSprite()
    {
        if (_builtinSprite == null)
            _builtinSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        return _builtinSprite;
    }

    // ---------------------------------------------------------------------
    // Warm ambient render settings (safe, easily reverted values)
    // ---------------------------------------------------------------------
    private static void ApplyWarmRenderSettings()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.44f, 0.28f, 1f);
    }
}
