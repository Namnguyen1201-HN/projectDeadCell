using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody2D rb;
    public float moveSpeed;
    public float jumpForce; // Lực nhảy
    
    [Header("Ground Check")]
    public Transform groundCheck; // Vị trí dưới chân nhân vật để kiểm tra chạm đất
    public float groundCheckRadius; // Bán kính vòng tròn kiểm tra
    public LayerMask groundLayer; // Layer của mặt đất
    
    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isGrounded;

    [Header("Animation")]
    public Animator anim;

    private void Update()
    {
        // 1. Kiểm tra chạm đất
        // Phải đảm bảo groundCheck đã được gán để không bị lỗi
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // 2. Lấy input di chuyển
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 3. Xử lý nhảy (Nút Space hoặc nút Jump mặc định)
        // Chỉ nhảy khi vừa nhấn nút và đang ở trên mặt đất
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        // 4. Lật hình ảnh nhân vật
        Flip();

        // 5. Xử lý animation
        HandaleAnimations();
    }

    private void FixedUpdate()
    {
        // Thiết lập vận tốc mới cho Rigidbody2D, giữ nguyên vận tốc trục Y
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
    }

    private void Flip()
    {
        // Nếu nhân vật đang quay sang phải nhưng di chuyển sang trái, hoặc ngược lại
        if (isFacingRight && horizontalInput < 0f || !isFacingRight && horizontalInput > 0f)
        {
            isFacingRight = !isFacingRight; // Đổi trạng thái hướng mặt
            
            // Đảo ngược trục x của localScale
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    void HandaleAnimations()
    {
        anim.SetBool("isJumping", rb.velocity.y > .1f);
        anim.SetFloat("yVelocity", rb.velocity.y);

        anim.SetBool("isStand", Mathf.Abs(rb.velocity.x) < .1f && isGrounded);
        anim.SetBool("isRunning", Mathf.Abs(rb.velocity.x) > .1f && isGrounded);
        
    }

    // Vẽ vòng tròn Ground Check ra Scene để dễ dàng căn chỉnh
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
