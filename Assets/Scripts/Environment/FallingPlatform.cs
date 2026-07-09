using UnityEngine;
using System.Collections;

/// <summary>
/// Nền đất mục/cành cây gãy (Đặc trưng mùa Thu).
/// Rung lắc khi có người chơi đứng lên, sau đó rơi xuống và tự hồi sinh.
/// </summary>
public class FallingPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public float shakeDelay = 0.5f;   // Thời gian rung trước khi rơi
    public float fallDelay = 1f;      // Thời gian rơi (từ lúc chạm)
    public float respawnDelay = 3f;   // Thời gian hồi sinh
    public float shakeMagnitude = 0.05f;
    
    private Vector3 originalPosition;
    private bool isFalling = false;
    
    private Rigidbody2D rb;

    private void Awake()
    {
        originalPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFalling && collision.gameObject.CompareTag("Player"))
        {
            // Chỉ kích hoạt nếu player đáp từ trên xuống
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) // Player is on top
                {
                    StartCoroutine(FallSequence());
                    break;
                }
            }
        }
    }

    private IEnumerator FallSequence()
    {
        isFalling = true;
        
        // 1. Rung lắc
        float elapsed = 0f;
        while (elapsed < shakeDelay)
        {
            transform.position = originalPosition + (Vector3)Random.insideUnitCircle * shakeMagnitude;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 2. Chờ thêm (tổng cộng bằng fallDelay)
        yield return new WaitForSeconds(fallDelay - shakeDelay);
        
        // 3. Rơi tự do
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        
        // 4. Chờ respawn
        yield return new WaitForSeconds(respawnDelay);
        
        // 5. Hồi sinh
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
        }
        transform.position = originalPosition;
        isFalling = false;
    }
}
