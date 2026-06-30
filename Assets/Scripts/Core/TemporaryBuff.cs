using UnityEngine;

/// <summary>
/// Vật phẩm buff tạm thời (đặt trên màn chơi hoặc rơi từ rương/quái).
/// Player chạm vào → nhận buff → vật phẩm tự huỷ.
/// </summary>
public class TemporaryBuff : MonoBehaviour
{
    [Header("Buff Settings")]
    public BuffReceiver.BuffType buffType = BuffReceiver.BuffType.DoubleDamage;
    public float duration = 10f;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    [Header("Float Animation")]
    public float floatAmplitude = 0.15f;
    public float floatFrequency = 1.5f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Lơ lửng nhẹ
        float y = _startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        BuffReceiver receiver = other.GetComponent<BuffReceiver>();
        if (receiver != null)
        {
            receiver.ApplyBuff(buffType, duration);
            Debug.Log($"[TemporaryBuff] Player received {buffType} for {duration}s");
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = buffType == BuffReceiver.BuffType.DoubleDamage ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
