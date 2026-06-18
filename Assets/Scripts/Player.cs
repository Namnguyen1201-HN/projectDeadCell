using UnityEngine;

public class Player : MonoBehaviour
{
    // FSM
    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerRollState RollState { get; private set; }

    [Header("Movement")]
    public Rigidbody2D rb;
    public float moveSpeed = 7f;
    public float jumpForce = 25f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Ceiling Check")]
    public Transform ceilingCheck; // Vị trí đỉnh đầu để kiểm tra trần nhà
    public float ceilingCheckRadius = 0.2f;

    [Header("Animation")]
    public Animator anim;

    [Header("Roll Setting")]
    public float rollDuration = 0.6f;
    public float rollSpeed = 12f;
    public float rollStopDuration = 0.15f; 

    [Header("Collider Setting")]
    public CapsuleCollider2D coll;
    public Vector2 rollColliderSize = new Vector2(1.57f, 1f);
    public Vector2 rollColliderOffset = new Vector2(0.51f, -0.6f);
    
    public Vector2 normalColliderSize { get; private set; }
    public Vector2 normalColliderOffset { get; private set; }

    public float horizontalInput { get; private set; }
    public bool isFacingRight { get; private set; } = true;
    public bool isGrounded { get; private set; }
    public bool isCeilingHit { get; private set; }

    private void Awake()
    {
        StateMachine = new PlayerStateMachine();
        
        // Khởi tạo các trạng thái và truyền tên bool của Animation
        IdleState = new PlayerIdleState(this, StateMachine, "isStand");
        MoveState = new PlayerMoveState(this, StateMachine, "isRunning");
        JumpState = new PlayerJumpState(this, StateMachine, "isJumping");
        RollState = new PlayerRollState(this, StateMachine, "isRolling");
    }

    private void Start()
    {
        if (coll != null)
        {
            normalColliderSize = coll.size;
            normalColliderOffset = coll.offset;
        }

        // Bắt đầu FSM ở trạng thái đứng im
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        // Thu thập input và cờ trạng thái trước để các State dùng chung
        horizontalInput = Input.GetAxisRaw("Horizontal");
        
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (ceilingCheck != null)
            isCeilingHit = Physics2D.OverlapCircle(ceilingCheck.position, ceilingCheckRadius, groundLayer);

        // Gọi logic của trạng thái hiện tại
        StateMachine.CurrentState.Update();

        // Xử lý hướng mặt độc lập (không cho phép lật mặt khi đang lăn)
        if (StateMachine.CurrentState != RollState)
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.FixedUpdate();
    }

    public void Flip()
    {
        if (isFacingRight && horizontalInput < 0f || !isFacingRight && horizontalInput > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (ceilingCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(ceilingCheck.position, ceilingCheckRadius);
        }
    }
}
