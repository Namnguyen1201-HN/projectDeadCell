using UnityEngine;

public class Arrow : MonoBehaviour
{
    public int damage;
    public float lifetime = 3f;

    void Start()
    {
        // Tự hủy mũi tên sau khoảng thời gian lifetime để không bị rác game
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // 1. Nếu chạm vào chính Player hoặc các bộ phận con của Player (GroundCheck, AttackPoint...) thì bỏ qua
        if (col.CompareTag("Player") || col.transform.root.CompareTag("Player")) return;

        // 2. Nếu chạm vào các vùng quét ảo (isTrigger = true) mà không phải là quái thì cũng bỏ qua
        if (col.isTrigger && col.GetComponent<Health>() == null) return;

        // Tìm component Health trên vật thể vừa chạm vào
        Health enemyHealth = col.GetComponent<Health>();
        if (enemyHealth != null)
        {
            enemyHealth.changeHealth(-damage);
        }

        // Hủy mũi tên ngay khi chạm trúng vật cứng (đất, tường) hoặc quái
        Destroy(gameObject);
    }
}
