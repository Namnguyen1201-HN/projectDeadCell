using UnityEngine;
using UnityEditor;

public class ArrowPrefabCreator : EditorWindow
{
    [MenuItem("Tools/Create Arrow Prefab")]
    public static void CreateArrowPrefab()
    {
        // Load from the clean path copy
        string spritePath = "Assets/Sprites/NhanVat/Arrow01_32x32.png";
        Sprite arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        
        if (arrowSprite == null)
        {
            // Try loading as Texture2D then get sprite
            Texture2D tex2d = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (tex2d != null)
            {
                arrowSprite = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), new Vector2(0.5f, 0.5f), 32);
                Debug.Log("Loaded arrow as Texture2D from: " + spritePath);
            }
        }
        else
        {
            Debug.Log("Found arrow sprite at: " + spritePath);
        }

        if (arrowSprite == null)
        {
            Debug.LogError("Could not find Arrow01(32x32) sprite anywhere in the project. Creating a fallback arrow.");
            // Create a simple white pixel sprite as fallback
            Texture2D tex = new Texture2D(32, 8, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 8];
            // Draw a simple arrow shape
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (x < 24 && y >= 2 && y <= 5)
                        pixels[y * 32 + x] = new Color(0.6f, 0.4f, 0.2f); // brown shaft
                    else if (x >= 24 && y >= 1 && y <= 6)
                        pixels[y * 32 + x] = new Color(0.7f, 0.7f, 0.7f); // gray tip
                    else
                        pixels[y * 32 + x] = Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            arrowSprite = Sprite.Create(tex, new Rect(0, 0, 32, 8), new Vector2(0.5f, 0.5f), 32);
        }

        GameObject arrowObject = new GameObject("ArrowPrefab");
        
        SpriteRenderer sr = arrowObject.AddComponent<SpriteRenderer>();
        sr.sprite = arrowSprite;
        sr.sortingOrder = 30;

        // Scale up the arrow so it's visible relative to the player
        arrowObject.transform.localScale = new Vector3(5f, 5f, 1f);

        BoxCollider2D collider = arrowObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 0.3f);

        Rigidbody2D rb = arrowObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;

        arrowObject.AddComponent<PlayerProjectile>();

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string prefabPath = "Assets/Resources/ArrowPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(arrowObject, prefabPath);
        DestroyImmediate(arrowObject);

        Debug.Log("Successfully created ArrowPrefab at " + prefabPath);
    }
}
