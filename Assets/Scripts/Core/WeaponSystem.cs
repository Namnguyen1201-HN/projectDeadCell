using UnityEngine;
using System;

/// <summary>
/// Hệ thống 2 ô trang bị vũ khí theo GDD.
/// Logic ràng buộc: Cung (Bow) và Khiên (Shield) không thể trang bị đồng thời.
/// Attach vào Player GameObject.
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    public event Action onWeaponChanged;

    public enum WeaponType { None, Sword, Bow, Shield }

    [Header("Equipment Slots")]
    public WeaponData slot1;
    public WeaponData slot2;

    [Header("Active Slot (1 or 2)")]
    public int activeSlot = 1;

    [System.Serializable]
    public class WeaponData
    {
        public WeaponType type  = WeaponType.None;
        public string     name  = "Empty";
        public int        damage = 0;
        public Sprite     icon;
    }

    // ─────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────

    public WeaponData GetActiveWeapon()
        => activeSlot == 1 ? slot1 : slot2;

    public void SwitchSlot()
    {
        activeSlot = activeSlot == 1 ? 2 : 1;
        onWeaponChanged?.Invoke();
        Debug.Log("[WeaponSystem] Switched to slot " + activeSlot + ": " + GetActiveWeapon().name);
    }

    /// <summary>
    /// Thử trang bị vũ khí vào targetSlot.
    /// Trả về false nếu vi phạm ràng buộc Cung ↔ Khiên.
    /// </summary>
    public bool TryEquip(WeaponData newWeapon, int targetSlot)
    {
        WeaponData otherSlot = targetSlot == 1 ? slot2 : slot1;

        // Kiểm tra ràng buộc
        if (newWeapon.type == WeaponType.Bow && otherSlot.type == WeaponType.Shield)
        {
            Debug.LogWarning("[WeaponSystem] Không thể trang bị Cung khi đang mang Khiên!");
            return false;
        }
        if (newWeapon.type == WeaponType.Shield && otherSlot.type == WeaponType.Bow)
        {
            Debug.LogWarning("[WeaponSystem] Không thể trang bị Khiên khi đang mang Cung!");
            return false;
        }

        if (targetSlot == 1) slot1 = newWeapon;
        else                 slot2 = newWeapon;

        onWeaponChanged?.Invoke();
        Debug.Log("[WeaponSystem] Equipped " + newWeapon.name + " to slot " + targetSlot);
        return true;
    }

    /// <summary>Trang bị tự động vào slot trống hoặc slot hiện tại.</summary>
    public bool AutoEquip(WeaponData newWeapon)
    {
        if (slot1.type == WeaponType.None) return TryEquip(newWeapon, 1);
        if (slot2.type == WeaponType.None) return TryEquip(newWeapon, 2);
        return TryEquip(newWeapon, activeSlot); // Thay thế slot đang dùng
    }

    /// <summary>Kiểm tra người chơi đang giơ Khiên (cho cơ chế Parry).</summary>
    public bool IsShieldActive()
        => GetActiveWeapon().type == WeaponType.Shield;

    /// <summary>Lấy tổng damage của vũ khí đang dùng (base + bonus).</summary>
    public int GetActiveDamage()
        => GetActiveWeapon().damage;
}
