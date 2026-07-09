using UnityEngine;

/// <summary>
/// Khu vực có gió thổi, tác dụng lực lên các vật thể có Rigidbody2D (bao gồm cả Player).
/// Đặc trưng Màn 3 - Mùa Thu.
/// </summary>
public class AutumnWindZone : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector2 windDirection = Vector2.right;
    public float windForce = 15f;
    
    [Header("Gust Settings")]
    public bool isConstant = true; // Gió thổi liên tục hay từng cơn
    public float gustInterval = 2f;
    public float gustDuration = 1f;
    
    private float timer = 0f;
    private bool isGusting = false;

    private void Update()
    {
        if (!isConstant)
        {
            timer += Time.deltaTime;
            if (!isGusting && timer >= gustInterval)
            {
                isGusting = true;
                timer = 0f;
            }
            else if (isGusting && timer >= gustDuration)
            {
                isGusting = false;
                timer = 0f;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isConstant && !isGusting) return;
        
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Áp dụng lực gió
            rb.AddForce(windDirection.normalized * windForce);
        }
    }
}
