using UnityEngine;

/// <summary>
/// Dải sương mù trôi ngang — đặc trưng không khí mùa Thu.
/// Gắn vào một SpriteRenderer hình chữ nhật bán trong suốt.
/// </summary>
public class AutumnFogEffect : MonoBehaviour
{
    [Header("Drift")]
    public float driftSpeed = 0.18f;
    public float driftRange = 3.5f;

    [Header("Fade Pulse")]
    public float minAlpha = 0.04f;
    public float maxAlpha = 0.18f;
    public float pulseSpeed = 0.35f;

    private Vector3 _origin;
    private SpriteRenderer _sr;
    private float _timeOffset;

    private void Awake()
    {
        _origin = transform.position;
        _sr = GetComponent<SpriteRenderer>();
        _timeOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Time.time + _timeOffset;
        transform.position = new Vector3(
            _origin.x + Mathf.Sin(t * driftSpeed) * driftRange,
            _origin.y + Mathf.Sin(t * driftSpeed * 0.4f) * 0.25f,
            _origin.z);

        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f);
            _sr.color = c;
        }
    }
}
