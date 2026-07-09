using UnityEngine;

/// <summary>
/// Khu vực hang động – khi Player bước vào sẽ đổi background.
/// Gắn lên một Empty GameObject có BoxCollider2D (isTrigger = true)
/// bao phủ toàn bộ khu vực hang.
/// </summary>
public class CaveZone : MonoBehaviour
{
    [Header("Background References")]
    [Tooltip("GameObject chứa các layer background ngoài trời (sẽ bị ẩn khi vào hang)")]
    public GameObject outdoorBackground;

    [Tooltip("GameObject chứa các layer background trong hang (sẽ hiện khi vào hang)")]
    public GameObject caveBackground;

    [Header("Lighting (Optional)")]
    [Tooltip("Giảm sáng khi vào hang")]
    public Color caveTint = new Color(0.6f, 0.6f, 0.7f, 1f);
    private Color originalCameraColor;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
            originalCameraColor = mainCamera.backgroundColor;
    }

    private void Start()
    {
        // Mặc định ẩn nền hang
        if (caveBackground != null) caveBackground.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Đổi sang nền hang
        if (outdoorBackground != null) outdoorBackground.SetActive(false);
        if (caveBackground != null) caveBackground.SetActive(true);

        if (mainCamera != null)
            mainCamera.backgroundColor = caveTint;

        Debug.Log("[CaveZone] Player entered cave area.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Quay lại nền ngoài trời
        if (outdoorBackground != null) outdoorBackground.SetActive(true);
        if (caveBackground != null) caveBackground.SetActive(false);

        if (mainCamera != null)
            mainCamera.backgroundColor = originalCameraColor;

        Debug.Log("[CaveZone] Player exited cave area.");
    }
}
