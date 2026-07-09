using UnityEngine;

/// <summary>
/// Nền tảng di chuyển qua lại giữa 2 điểm (ví dụ: cành cây đung đưa, tảng đá nổi).
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;
    
    private bool movingToB = true;

    private void Update()
    {
        if (pointA == null || pointB == null) return;

        Transform target = movingToB ? pointB : pointA;
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            movingToB = !movingToB;
        }
    }

    // Giúp người chơi bám vào platform khi nó di chuyển
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}
