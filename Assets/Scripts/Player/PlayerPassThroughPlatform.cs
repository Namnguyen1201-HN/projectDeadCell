using System.Collections;
using UnityEngine;

public class PlayerPassThroughPlatform : MonoBehaviour
{
    private Collider2D playerCollider;
    private GameObject currentOneWayPlatform;
    private Player player;

    [Header("Settings")]
    [SerializeField] private float passThroughTime = 0.5f;

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
        player = GetComponent<Player>();
    }

    private void Update()
    {
        // Nhấn nút Xuống và nút Nhảy cùng lúc
        if (Input.GetAxisRaw("Vertical") < -0.1f && Input.GetButtonDown("Jump"))
        {
            if (currentOneWayPlatform != null)
            {
                StartCoroutine(DropThroughRoutine());
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Kiểm tra xem bục đang đứng có PlatformEffector2D không
        if (collision.gameObject.TryGetComponent(out PlatformEffector2D effector))
        {
            currentOneWayPlatform = collision.gameObject;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject == currentOneWayPlatform)
        {
            currentOneWayPlatform = null;
        }
    }

    private IEnumerator DropThroughRoutine()
    {
        Collider2D platformCollider = currentOneWayPlatform.GetComponent<Collider2D>();
        
        // Tạm thời bỏ qua va chạm giữa nhân vật và bục
        Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
        
        yield return new WaitForSeconds(passThroughTime);
        
        // Bật lại va chạm sau khi đã rơi qua (kể cả khi đã rơi qua, ta vẫn trả lại trạng thái va chạm ban đầu)
        if (platformCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
    }
}
