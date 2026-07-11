using UnityEngine;
using System;

public class Player : MonoBehaviour
{
    public event Action<string> onSkillUnlocked;

    // FSM
    public PlayerStateMachine StateMachine { get; protected set; }
    public PlayerIdleState IdleState { get; protected set; }
    public PlayerMoveState MoveState { get; protected set; }
    public PlayerJumpState JumpState { get; protected set; }
    public PlayerRollState RollState { get; protected set; }
    public PlayerAttackState AttackState { get; protected set; }

    [Header("Movement")]
    public Rigidbody2D rb;
    public float moveSpeed;
    public float jumpForce;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius;
    public LayerMask groundLayer;

    [Header("Ceiling Check")]
    public Transform ceilingCheck;
    public float ceilingCheckRadius;

    [Header("Combat")]
    public Combat combat;
    public bool isAttackFinished;

    [Header("Animation")]
    public Animator anim;

    [Header("Roll Setting")]
    public float rollDuration;
    public float rollSpeed;
    public float rollStopDuration;

    [Header("Collider Setting")]
    public CapsuleCollider2D coll;
    public Vector2 rollColliderSize;
    public Vector2 rollColliderOffset;

    public Vector2 normalColliderSize { get; private set; }
    public Vector2 normalColliderOffset { get; private set; }

    [Header("Abilities & Items")]
    public int keyCount = 0;
    public bool hasDoubleJump = false;
    public int jumpsRemaining = 1;
    public int strengthBuffAmount = 10;

    public float horizontalInput { get; private set; }
    public bool isFacingRight { get; private set; } = true;
    public bool isGrounded { get; private set; }
    public bool isCeilingHit { get; private set; }

   
    [Header("Core Systems")]
    public StanceManager stanceManager;
    public WeaponSystem weaponSystem;
    public BuffReceiver buffReceiver;

    protected virtual void Awake()
    {
        stanceManager = GetComponent<StanceManager>();
        weaponSystem = GetComponent<WeaponSystem>();
        buffReceiver = GetComponent<BuffReceiver>();

        StateMachine = new PlayerStateMachine();

        // Khởi tạo các trạng thái và truyền tên bool của Animation
        IdleState = new PlayerIdleState(this, StateMachine, "isStand");
        MoveState = new PlayerMoveState(this, StateMachine, "isRunning");
        JumpState = new PlayerJumpState(this, StateMachine, "isJumping");
        RollState = new PlayerRollState(this, StateMachine, "isRolling");
        AttackState = new PlayerAttackState(this, StateMachine, "isAttacking");

        // Thêm PhysicsMaterial2D không ma sát (Friction = 0) để tránh bị kẹt trên đầu quái hoặc dính tường
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction");
        noFriction.friction = 0f;
        noFriction.bounciness = 0f;
        if (rb != null) rb.sharedMaterial = noFriction;
        if (coll != null) coll.sharedMaterial = noFriction;
    }

    protected virtual void Start()
    {
        if (coll != null)
        {
            normalColliderSize = coll.size;
            normalColliderOffset = coll.offset;
        }

        // Bắt đầu FSM ở trạng thái đứng im
        StateMachine.Initialize(IdleState);
    }

    protected virtual void Update()
    {
        // Thu thập input và cờ trạng thái trước để các State dùng chung
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (ceilingCheck != null)
            isCeilingHit = Physics2D.OverlapCircle(ceilingCheck.position, ceilingCheckRadius, groundLayer);

        // Gọi logic của trạng thái hiện tại
        StateMachine.CurrentState.Update();

        if (StateMachine.CurrentState != RollState)
        {
            Flip();
        }
    }

    protected virtual void FixedUpdate()
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

    public bool CanAttack()
    {
        if (combat == null) return false;
        return Time.time >= combat.nextAttackTime;
    }

    public void AttackAnimationFinished()
    {
        isAttackFinished = true;
    }

    public void UnlockSkill(string skillName)
    {
        if (skillName == "DoubleJump")
        {
            hasDoubleJump = true;
        }
        else if (skillName == "Streng")
        {
            if (combat != null)
            {
                combat.attackDamage += strengthBuffAmount;
                Debug.Log("Đã Unlock Streng! Sát thương tăng lên thành: " + combat.attackDamage);
            }
        }
        // Thêm các logic unlock skill khác ở đây sau này nếu cần

        onSkillUnlocked?.Invoke(skillName);
    }

    protected virtual void OnDrawGizmosSelected()
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
