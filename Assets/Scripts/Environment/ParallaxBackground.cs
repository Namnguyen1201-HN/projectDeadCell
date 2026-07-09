using UnityEngine;

/// <summary>
/// Hiệu ứng thị giác chiều sâu cho Background (Layer xa trôi chậm, layer gần trôi nhanh).
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    public float parallaxMultiplier = 0.5f; // 1 = Đi theo Camera, 0 = Đứng im
    
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    private void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            lastCameraPosition = cameraTransform.position;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        transform.position += new Vector3(deltaMovement.x * parallaxMultiplier, deltaMovement.y * parallaxMultiplier, 0);
        
        lastCameraPosition = cameraTransform.position;
    }
}
