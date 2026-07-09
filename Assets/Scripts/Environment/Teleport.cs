using UnityEngine;
using Cinemachine;

/// <summary>
/// Dịch chuyển Player sang khu vực mới khi chạm vào trigger.
/// FIXED v2:
/// - Null-guard cho destination (tránh NullReferenceException crash)
/// - Null-guard cho Rigidbody2D (reset velocity sau teleport)
/// - Debug log hữu ích khi destination chưa gán
/// </summary>
public class Teleport : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Kéo thả một Empty GameObject ở vị trí đích vào đây.")]
    public Transform destination;

    [Header("Boss Settings (Tùy chọn)")]
    [Tooltip("Kéo thả Boss ở phòng mới vào đây để hiển thị thanh máu khi teleport tới.")]
    public Health targetBoss;

    [Header("Camera Bounds (Tùy chọn)")]
    [Tooltip("Kéo thả PolygonCollider2D chứa giới hạn của map mới vào đây.")]
    public Collider2D newCameraBounds;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // ── Guard: destination chưa gán ──
        if (destination == null)
        {
            Debug.LogWarning($"[Teleport] '{gameObject.name}' chưa gán Destination! " +
                             "Hãy kéo SpawnPoint vào field 'Destination' trong Inspector.");
            return;
        }

        // ── 1. Dịch chuyển Player ──
        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero; // Reset velocity trước khi teleport tránh trượt
            rb.position = destination.position;
        }
        collision.transform.position = destination.position;

        // ── 2. Cập nhật Camera Bounds (Cinemachine Confiner2D) ──
        if (newCameraBounds != null)
        {
            CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                CinemachineConfiner2D confiner = vcam.GetComponent<CinemachineConfiner2D>();
                if (confiner != null)
                {
                    confiner.m_BoundingShape2D = newCameraBounds;
                    confiner.InvalidateCache();
                }
            }
        }

        // ── 3. Hiển thị UI máu Boss ──
        if (targetBoss != null && UIManager.Instance != null)
        {
            UIManager.Instance.ShowBossHealth(targetBoss);
        }

        Debug.Log($"[Teleport] Player teleported → {destination.name} @ {destination.position}");
    }
}
