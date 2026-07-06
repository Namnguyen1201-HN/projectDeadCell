using System.Collections;
using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    private Health health;
    private SpriteRenderer spriteRenderer;
    private Player playerMovement;
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Hurt Settings")]
    public Color hurtColor = Color.red;
    public float hurtDuration = 0.2f;
    public float stunDuration = 0.3f; // Thời gian choáng của người chơi
    private Color originalColor;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerMovement = GetComponent<Player>();
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.onDamaged += HandleHurt;
            health.onDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onDamaged -= HandleHurt;
            health.onDeath -= HandleDeath;
        }
    }

    private void HandleHurt()
    {
        // 1. Chớp đỏ nhân vật
        if (spriteRenderer != null)
        {
            StopCoroutine(FlashHurtColor());
            StartCoroutine(FlashHurtColor());
        }

        // 2. Chạy animation hurt (nếu có)
        // Lưu ý: Bạn cần tạo một Trigger tên là "hurt" trong Animator
        if (anim != null)
        {
            anim.SetTrigger("hurt");
        }

        // 3. Làm choáng người chơi
        StopCoroutine("StunPlayerRoutine");
        StartCoroutine("StunPlayerRoutine");
    }

    private IEnumerator StunPlayerRoutine()
    {
        // Tạm tắt quyền điều khiển
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        if (rb != null)
        {
            rb.velocity = Vector2.zero; // Dừng lập tức
        }
        
        yield return new WaitForSeconds(stunDuration);
        
        // Nếu người chơi chưa chết thì bật lại
        if (health != null && health.health > 0 && playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    private IEnumerator FlashHurtColor()
    {
        spriteRenderer.color = hurtColor;
        yield return new WaitForSeconds(hurtDuration);
        spriteRenderer.color = originalColor;
    }

    private void HandleDeath()
    {
        // 1. Chạy animation death
        // Lưu ý: Bạn cần tạo một Bool tên là "isDead" trong Animator
        if (anim != null)
        {
            anim.SetBool("isDead", true);
        }

        // 2. Vô hiệu hoá script di chuyển
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // 3. Dừng di chuyển vật lý
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            // Tùy chọn: rb.simulated = false; // Bỏ comment nếu không muốn xác chết rơi xuống
        }
        
        Debug.Log("Player đã chết!");
    }

    public void Revive()
    {
        // 1. Reset lại Animator để chắc chắn thoát khỏi trạng thái chết
        if (anim != null)
        {
            anim.SetBool("isDead", false);
            anim.Rebind();
            anim.Update(0f);
        }

        // 2. Bật lại script điều khiển và reset State Machine về Idle
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            if (playerMovement.StateMachine != null && playerMovement.IdleState != null)
            {
                playerMovement.StateMachine.ChangeState(playerMovement.IdleState);
            }
        }
    }
}
