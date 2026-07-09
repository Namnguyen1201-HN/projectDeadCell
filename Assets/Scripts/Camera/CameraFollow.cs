using UnityEngine;

/// <summary>
/// Camera đơn giản — Follow Player theo trục X và Y.
/// Gắn vào GameObject Main Camera.
///
/// FIXED v2:
/// - Tự tìm Player nếu target chưa gán (tránh "No cameras rendering").
/// - Bounds mặc định khớp với map AutumnRuins (172 tiles).
/// - Thêm debug log khi mất target.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Kéo Player vào đây (hoặc để trống — tự tìm)

    [Header("Follow Settings")]
    public float smoothSpeed = 6f;
    public Vector3 offset = new Vector3(2f, 2f, -10f);

    [Header("X Bounds (Giới hạn camera theo chiều ngang)")]
    public float minX = -5f;
    public float maxX = 180f;  // AutumnRuins: EXIT_X ~172 tiles

    [Header("Y Bounds (Giới hạn camera theo chiều dọc)")]
    public float minY = -3f;
    public float maxY = 12f;

    private bool _warnedMissingTarget = false;

    private void Start()
    {
        TryFindPlayer();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            TryFindPlayer();
            if (target == null)
            {
                if (!_warnedMissingTarget)
                {
                    Debug.LogWarning("[CameraFollow] Chưa có target. Hãy gán Player vào field 'target', " +
                                     "hoặc đảm bảo Player có tag 'Player'.");
                    _warnedMissingTarget = true;
                }
                return;
            }
        }

        _warnedMissingTarget = false;

        // Vị trí mong muốn = Player + offset
        Vector3 desired = target.position + offset;

        // Clamp trong giới hạn map
        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);
        desired.z = offset.z; // Giữ z cố định

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }

    private void TryFindPlayer()
    {
        if (target != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            Debug.Log("[CameraFollow] Tự động gán target = " + playerObj.name);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Hiện khung giới hạn camera trong Scene view
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size   = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
