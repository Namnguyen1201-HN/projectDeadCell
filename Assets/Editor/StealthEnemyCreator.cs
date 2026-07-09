using UnityEngine;
using UnityEditor;

public class StealthEnemyCreator : EditorWindow
{
    [MenuItem("Tools/Create Stealth Enemy in Scene")]
    public static void CreateStealthEnemy()
    {
        // Tìm sprite của Mushroom_Enemy prefab để dùng cho quái tàng hình
        string mushroomPrefabPath = "Assets/Prefab/Mushroom_Enemy.prefab";
        GameObject mushroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mushroomPrefabPath);

        // Tạo GameObject gốc
        GameObject stealthGO = new GameObject("StealthEnemy");
        stealthGO.tag = "Enemy";

        // Set layer Enemy nếu tồn tại
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            stealthGO.layer = enemyLayer;

        // --- SpriteRenderer ---
        GameObject spriteChild = new GameObject("Sprite");
        spriteChild.transform.SetParent(stealthGO.transform);
        spriteChild.transform.localPosition = Vector3.zero;

        SpriteRenderer sr = spriteChild.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        // Lấy sprite từ Mushroom prefab nếu có
        if (mushroomPrefab != null)
        {
            SpriteRenderer mushroomSR = mushroomPrefab.GetComponentInChildren<SpriteRenderer>();
            if (mushroomSR != null)
            {
                sr.sprite = mushroomSR.sprite;
                Debug.Log("[StealthEnemyCreator] Dùng sprite từ Mushroom_Enemy.");
            }
        }

        // Nếu không có sprite, tạo một sprite pixel đơn giản tím/đen (màu tàng hình)
        if (sr.sprite == null)
        {
            Texture2D tex = new Texture2D(16, 24, TextureFormat.RGBA32, false);
            Color purple = new Color(0.4f, 0.0f, 0.6f, 1f);
            Color[] pixels = new Color[16 * 24];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = purple;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 24), new Vector2(0.5f, 0.5f), 16);
            Debug.Log("[StealthEnemyCreator] Dùng sprite tím tự tạo vì không tìm thấy sprite quái.");
        }

        // Màu tím trong suốt để nhìn thấy trong editor nhưng game sẽ tàng hình
        sr.color = new Color(0.5f, 0f, 1f, 0.8f);

        // --- Rigidbody2D ---
        Rigidbody2D rb = stealthGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // --- BoxCollider2D ---
        BoxCollider2D col = stealthGO.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 1.0f);
        col.offset = new Vector2(0, 0);

        // --- Health ---
        Health health = stealthGO.AddComponent<Health>();
        // Health thường có maxHealth field
        SerializedObject so = new SerializedObject(health);
        SerializedProperty maxHp = so.FindProperty("maxHealth");
        if (maxHp != null) { maxHp.intValue = 60; so.ApplyModifiedProperties(); }

        // --- StealthEnemy script ---
        StealthEnemy stealthScript = stealthGO.AddComponent<StealthEnemy>();
        stealthScript.spriteRenderer = sr;

        // Đặt vị trí vào giữa scene view hoặc (0,2,0) nếu không có editor camera
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 sceneCenter = SceneView.lastActiveSceneView.camera.transform.position;
            stealthGO.transform.position = new Vector3(sceneCenter.x + 5f, sceneCenter.y, 0f);
        }
        else
        {
            stealthGO.transform.position = new Vector3(5f, 2f, 0f);
        }

        // Đăng ký Undo để user có thể Ctrl+Z
        Undo.RegisterCreatedObjectUndo(stealthGO, "Create Stealth Enemy");

        // Chọn object vừa tạo trong Hierarchy
        Selection.activeGameObject = stealthGO;

        Debug.Log("[StealthEnemyCreator] Đã tạo StealthEnemy trong scene! Hãy kéo nó đến đúng vị trí trên mặt đất.");
    }
}
