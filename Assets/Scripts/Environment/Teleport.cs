using UnityEngine;
using Cinemachine;

public class Teleport : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Kéo thả một Empty GameObject ở vị trí đích (bên map mới) vào đây.")]
    public Transform destination;

    [Header("Boss Settings (Tùy chọn)")]
    [Tooltip("Kéo thả Boss ở phòng mới vào đây để hiển thị thanh máu khi teleport tới.")]
    public Health targetBoss;

    [Header("Camera Bounds (Tùy chọn)")]
    [Tooltip("Kéo thả PolygonCollider2D chứa giới hạn của map mới vào đây.")]
    public Collider2D newCameraBounds;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đối tượng chạm vào có phải là Player không
        if (collision.CompareTag("Player"))
        {
            // 1. Dịch chuyển Player tới vị trí đích
            collision.transform.position = destination.position;

            // 2. Cập nhật lại giới hạn Camera (nếu có dùng CinemachineConfiner2D)
            if (newCameraBounds != null)
            {
                // Tìm Virtual Camera trong Scene
                var vcam = FindObjectOfType<CinemachineVirtualCamera>();
                if (vcam != null)
                {
                    // Lấy thành phần Confiner2D
                    var confiner = vcam.GetComponent<CinemachineConfiner2D>();
                    if (confiner != null)
                    {
                        // Đổi ranh giới sang map mới
                        confiner.m_BoundingShape2D = newCameraBounds;
                        
                        // Xóa cache cũ để camera cập nhật ranh giới ngay lập tức
                        confiner.InvalidateCache(); 
                    }
                }
            }

            // 3. Hiển thị UI máu Boss nếu có Boss ở khu vực này
            if (targetBoss != null && UIManager.Instance != null)
            {
                UIManager.Instance.ShowBossHealth(targetBoss);
            }
        }
    }
}
