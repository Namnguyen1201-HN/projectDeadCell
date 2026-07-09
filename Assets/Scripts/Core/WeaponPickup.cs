using UnityEngine;

/// <summary>
/// Vũ khí rơi ra từ quái hoặc rương.
/// Player chạm vào → thử trang bị → tự huỷ nếu thành công.
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Data")]
    public WeaponSystem.WeaponType weaponType = WeaponSystem.WeaponType.Sword;
    public string weaponName  = "Iron Sword";
    public int    bonusDamage = 5;
    public Sprite weaponIcon;

    [Header("Float Animation")]
    public float floatAmplitude = 0.12f;
    public float floatFrequency = 1.2f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        float y = _startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        WeaponSystem ws = other.GetComponent<WeaponSystem>();
        if (ws == null) return;

        WeaponSystem.WeaponData data = new WeaponSystem.WeaponData
        {
            type   = weaponType,
            name   = weaponName,
            damage = bonusDamage,
            icon   = weaponIcon
        };

        if (ws.AutoEquip(data))
        {
            Debug.Log("[WeaponPickup] Player picked up: " + weaponName);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("[WeaponPickup] Cannot equip " + weaponName + " (constraint).");
        }
    }
}
