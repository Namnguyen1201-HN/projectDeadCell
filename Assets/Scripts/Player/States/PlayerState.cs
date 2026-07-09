using UnityEngine;

public abstract class PlayerState
{
    protected Player player;
    protected PlayerStateMachine stateMachine;
    protected string animBoolName;

    public PlayerState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
    {
        this.player        = _player;
        this.stateMachine  = _stateMachine;
        this.animBoolName  = _animBoolName;
    }

    public virtual void Enter()
    {
        // FIXED: null-guard tránh crash khi Animator chưa gán (ví dụ: AutumnRuins bootstrap chưa xong)
        if (player.anim != null && !string.IsNullOrEmpty(animBoolName))
            player.anim.SetBool(animBoolName, true);
    }

    public virtual void Update()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public virtual void Exit()
    {
        // FIXED: null-guard tương tự
        if (player.anim != null && !string.IsNullOrEmpty(animBoolName))
            player.anim.SetBool(animBoolName, false);
    }
}
