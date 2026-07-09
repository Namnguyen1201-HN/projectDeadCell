using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Quản lý hệ thống 4 Hệ Tộc (Stances) theo GDD.
/// Mỗi stance mang lại buff passive riêng sau khi hạ Boss tương ứng.
/// Attach vào Player GameObject.
/// </summary>
public class StanceManager : MonoBehaviour
{
    public event Action<StanceType> onStanceChanged;

    public enum StanceType { None, Spring, Summer, Autumn, Winter }

    [Header("Unlocked Stances")]
    public bool hasSpring  = false;
    public bool hasSummer  = false;
    public bool hasAutumn  = false;
    public bool hasWinter  = false;

    [Header("Current Stance")]
    public StanceType currentStance = StanceType.None;

    // ── AUTUMN: Tăng tốc & nhảy xa ────────────────────────────────────
    [Header("Autumn Stance - Speed & Jump")]
    public float autumnSpeedMultiplier = 1.4f;
    public float autumnJumpMultiplier  = 1.3f;

    // ── SPRING: Hồi máu chậm theo thời gian ───────────────────────────
    [Header("Spring Stance - Regen")]
    public float springHealInterval = 5f;   // Giây giữa mỗi lần hồi
    public int   springHealAmount   = 1;
    private Coroutine _regenRoutine;

    // ── SUMMER: Tỉ lệ gây cháy ────────────────────────────────────────
    [Header("Summer Stance - Burn")]
    [Range(0f, 1f)] public float summerBurnChance   = 0.25f;
    public int   summerBurnDamage   = 2;
    public float summerBurnDuration = 3f;

    // ── WINTER: Làm chậm quái ─────────────────────────────────────────
    [Header("Winter Stance - Slow")]
    public float winterSlowAmount   = 0.5f;   // Hệ số giảm tốc (0-1)
    public float winterSlowDuration = 2f;

    private Health _playerHealth;

    private void Awake()
    {
        _playerHealth = GetComponent<Health>();
    }

    // ─────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Mở khoá stance và tự động kích hoạt.</summary>
    public void UnlockStance(StanceType type)
    {
        switch (type)
        {
            case StanceType.Spring: hasSpring = true; break;
            case StanceType.Summer: hasSummer = true; break;
            case StanceType.Autumn: hasAutumn = true; break;
            case StanceType.Winter: hasWinter = true; break;
        }
        SetStance(type);
        Debug.Log("[StanceManager] Unlocked & activated: " + type);
    }

    /// <summary>Chuyển đổi stance (phải đã unlock).</summary>
    public void SetStance(StanceType type)
    {
        if (!IsUnlocked(type)) return;

        // Dừng buff của stance cũ
        StopCurrentStanceEffects();

        currentStance = type;
        onStanceChanged?.Invoke(type);

        // Khởi động buff mới
        StartCurrentStanceEffects();
    }

    // ─────────────────────────────────────────────────────────────────
    //  MULTIPLIERS (dùng trong Player States)
    // ─────────────────────────────────────────────────────────────────

    public float GetSpeedMultiplier()
        => currentStance == StanceType.Autumn ? autumnSpeedMultiplier : 1f;

    public float GetJumpMultiplier()
        => currentStance == StanceType.Autumn ? autumnJumpMultiplier : 1f;

    // ─────────────────────────────────────────────────────────────────
    //  SUMMER BURN (gọi từ Combat.cs)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Trả true nếu đòn đánh này gây cháy (Summer stance).</summary>
    public bool TryApplyBurn(Health targetHealth)
    {
        if (currentStance != StanceType.Summer) return false;
        if (UnityEngine.Random.value > summerBurnChance) return false;

        StartCoroutine(BurnRoutine(targetHealth));
        return true;
    }

    private IEnumerator BurnRoutine(Health target)
    {
        float elapsed = 0f;
        float tick    = 1f;
        float nextTick = tick;

        while (elapsed < summerBurnDuration)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= nextTick)
            {
                if (target != null && target.health > 0)
                    target.changeHealth(-summerBurnDamage);
                nextTick += tick;
            }
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  WINTER SLOW (gọi từ Combat.cs)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Áp dụng slow lên enemy (Winter stance).</summary>
    public void TryApplySlow(EnemyBase enemy)
    {
        if (currentStance != StanceType.Winter) return;
        if (enemy == null) return;
        enemy.ApplySlow(winterSlowAmount, winterSlowDuration);
    }

    // ─────────────────────────────────────────────────────────────────
    //  PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────────────

    private bool IsUnlocked(StanceType type)
    {
        switch (type)
        {
            case StanceType.Spring: return hasSpring;
            case StanceType.Summer: return hasSummer;
            case StanceType.Autumn: return hasAutumn;
            case StanceType.Winter: return hasWinter;
            default: return true; // None luôn hợp lệ
        }
    }

    private void StopCurrentStanceEffects()
    {
        if (_regenRoutine != null)
        {
            StopCoroutine(_regenRoutine);
            _regenRoutine = null;
        }
    }

    private void StartCurrentStanceEffects()
    {
        if (currentStance == StanceType.Spring)
            _regenRoutine = StartCoroutine(SpringRegenRoutine());
    }

    private IEnumerator SpringRegenRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(springHealInterval);
            if (_playerHealth != null && _playerHealth.health > 0)
                _playerHealth.changeHealth(springHealAmount);
        }
    }
}
