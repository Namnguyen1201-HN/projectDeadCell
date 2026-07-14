using UnityEngine;

/// <summary>
/// Chiếc lá mùa Thu bay nhẹ theo gió — chuyển động lắc lư tự nhiên.
/// Gắn vào bất kỳ SpriteRenderer nhỏ nào đại diện cho lá cây.
/// </summary>
public class AutumnLeafDrift : MonoBehaviour
{
    [Header("Sway")]
    public float swayAmplitude = 0.6f;
    public float swayFrequency = 0.55f;

    [Header("Spin")]
    public float spinSpeed = 45f;     // degrees per second

    [Header("Bob")]
    public float bobAmplitude = 0.12f;
    public float bobFrequency = 0.8f;

    private Vector3 _origin;
    private float _timeOffset;

    private void Awake()
    {
        _origin = transform.position;
        _timeOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Time.time + _timeOffset;
        transform.position = new Vector3(
            _origin.x + Mathf.Sin(t * swayFrequency) * swayAmplitude,
            _origin.y + Mathf.Sin(t * bobFrequency) * bobAmplitude,
            _origin.z);

        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }
}
