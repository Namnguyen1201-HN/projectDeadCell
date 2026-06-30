using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Nhận và quản lý các buff tạm thời theo GDD:
/// - x2 Sát thương trong N giây
/// - Bất tử trong N giây
/// Attach vào Player GameObject.
/// </summary>
public class BuffReceiver : MonoBehaviour
{
    public event Action<BuffType, float> onBuffApplied;
    public event Action<BuffType>        onBuffExpired;

    public enum BuffType { DoubleDamage, Invincible }

    [Header("Current Buff States")]
    public bool hasDoubleDamage = false;
    public bool isInvincible    = false;

    [Header("Timers (readonly)")]
    [SerializeField] private float doubleDamageRemaining = 0f;
    [SerializeField] private float invincibleRemaining   = 0f;

    private Coroutine _doubleDmgRoutine;
    private Coroutine _invincibleRoutine;

    // ─────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────

    public void ApplyBuff(BuffType type, float duration)
    {
        switch (type)
        {
            case BuffType.DoubleDamage:
                if (_doubleDmgRoutine != null) StopCoroutine(_doubleDmgRoutine);
                _doubleDmgRoutine = StartCoroutine(BuffRoutine(
                    type, duration,
                    () => { hasDoubleDamage = true;  doubleDamageRemaining = duration; },
                    () => { hasDoubleDamage = false; doubleDamageRemaining = 0f; }
                ));
                break;

            case BuffType.Invincible:
                if (_invincibleRoutine != null) StopCoroutine(_invincibleRoutine);
                _invincibleRoutine = StartCoroutine(BuffRoutine(
                    type, duration,
                    () => { isInvincible = true;  invincibleRemaining = duration; },
                    () => { isInvincible = false; invincibleRemaining = 0f; }
                ));
                break;
        }

        onBuffApplied?.Invoke(type, duration);
        Debug.Log($"[BuffReceiver] Applied {type} for {duration}s");
    }

    /// <summary>Hệ số nhân sát thương (dùng trong Combat.cs).</summary>
    public int GetDamageMultiplier() => hasDoubleDamage ? 2 : 1;

    // ─────────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator BuffRoutine(BuffType type, float duration,
        Action onStart, Action onEnd)
    {
        onStart?.Invoke();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Cập nhật timer hiển thị
            if (type == BuffType.DoubleDamage) doubleDamageRemaining = duration - elapsed;
            else                               invincibleRemaining   = duration - elapsed;
            yield return null;
        }

        onEnd?.Invoke();
        onBuffExpired?.Invoke(type);
        Debug.Log($"[BuffReceiver] {type} expired.");
    }

    private void Update()
    {
        // Đảm bảo timers không âm
        doubleDamageRemaining = Mathf.Max(0f, doubleDamageRemaining);
        invincibleRemaining   = Mathf.Max(0f, invincibleRemaining);
    }
}
