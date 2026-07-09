using UnityEngine;

public class Archer : Player
{
    [Header("Archer Gravity")]
    public float normalGravity = 1f;
    public float fallGravity = 2.5f;

    protected override void Awake()
    {
        StateMachine = new PlayerStateMachine();
        
        // Khởi tạo các trạng thái bằng State của Player nhưng truyền tên bool Animation của Archer
        IdleState = new PlayerIdleState(this, StateMachine, "isIdle");
        MoveState = new PlayerMoveState(this, StateMachine, "isWalking");
        JumpState = new PlayerJumpState(this, StateMachine, "isJumping");
        RollState = new PlayerRollState(this, StateMachine, "isSliding");
        
        // Nếu sau này Archer có đánh (Attack), nó sẽ tự động dùng "isAttacking"
        AttackState = new PlayerAttackState(this, StateMachine, "isAttacking");
    }

    protected override void Update()
    {
        base.Update();
        
        // Cập nhật thêm isGrounded cho Animator vì file Animator cũ của Archer cần biến này
        if (anim != null)
        {
            anim.SetBool("isGrounded", isGrounded);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        ApplyVariableGravity();
    }

    void ApplyVariableGravity()
    {
        if (rb != null)
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
    }
}