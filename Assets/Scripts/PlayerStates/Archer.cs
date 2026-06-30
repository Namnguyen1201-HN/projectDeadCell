using SuperTiled2Unity;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Archer : MonoBehaviour
{
    [Header("Component")]
    public Rigidbody2D rb;
    public PlayerInput playerInput;
    public Animator anim;

    [Header("Movement Variables")]
    public float speed;
    public float jumpForce;

    private Vector2 moveInput;
    private bool jumpPressed; // Biến này sẽ lưu lệnh nhảy khi bạn TAP nút

    public int facingDirection = 1; // 1 for right, -1 for left
    public float normalGravity;
    public float fallGravity;
    public float jumplGravity;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius;
    public LayerMask groundLayer;
    private bool isGrounded;

    void Start()
    {
        rb.gravityScale = normalGravity;
    }

    void Update()
    {
        HandleAnimations();
        Flip();
        
        // Sử dụng Input cũ giống với bộ chuyển động của Player để đảm bảo Tap là nhảy ngay lập tức
        if (Input.GetButtonDown("Jump"))
        {
            CheckGrounded();
            if (isGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            }
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();
        HandleMovement();
        ApplyVariableGravity();
    }

    void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void HandleMovement()
    {
        float targetSpeed = moveInput.x * speed;
        rb.velocity = new Vector2(targetSpeed, rb.velocity.y);
    }

    void HandleAnimations()
    {
        anim.SetBool("isJumping", rb.velocity.y > .1f);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.velocity.y);
        anim.SetBool("isIdle", Mathf.Abs(moveInput.x) < .1f && isGrounded);
        anim.SetBool("isWalking", Mathf.Abs(moveInput.x) > .1f && isGrounded);
    }



    void ApplyVariableGravity()
    {
        if (rb.velocity.y < -0.1f)
        {
            rb.gravityScale = fallGravity; // Rơi xuống nhanh (cảm giác game mượt)
        }
        else
        {
            rb.gravityScale = normalGravity; // Đi lên hoặc đứng yên thì trọng lực bình thường
        }
    }

    // Nhận sự kiện di chuyển từ Player Input (Send Messages)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }



    void Flip()
    {
        if (moveInput.x > 0.1f)
        {
            facingDirection = 1;
        }
        else if (moveInput.x < -0.1f)
        {
            facingDirection = -1;
        }

        transform.localScale = new Vector3(facingDirection, 1, 1);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}