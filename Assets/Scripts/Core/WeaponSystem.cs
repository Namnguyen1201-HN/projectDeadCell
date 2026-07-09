using UnityEngine;
using System;

/// <summary>
/// Manages the player's two weapon slots.
/// Team flow uses a fixed primary weapon per level:
/// Spring = Sword, Summer = Bow, Winter = Sword, Autumn = Bow.
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
        public WeaponType type = WeaponType.None;
        public string name = "Empty";
        public int damage = 0;
        public Sprite icon;
    }

    private void Awake()
    {
        EnsureSlots();
    }

    public WeaponData GetActiveWeapon()
    {
        EnsureSlots();
        return activeSlot == 1 ? slot1 : slot2;
    }

    public void SwitchSlot()
    {
        EnsureSlots();
        activeSlot = activeSlot == 1 ? 2 : 1;
        onWeaponChanged?.Invoke();
        Debug.Log("[WeaponSystem] Switched to slot " + activeSlot + ": " + GetActiveWeapon().name);
    }

    public bool TryEquip(WeaponData newWeapon, int targetSlot)
    {
        EnsureSlots();

        if (newWeapon == null)
        {
            Debug.LogWarning("[WeaponSystem] Cannot equip null weapon.");
            return false;
        }

        WeaponData otherSlot = targetSlot == 1 ? slot2 : slot1;

        if (newWeapon.type == WeaponType.Bow && otherSlot.type == WeaponType.Shield)
        {
            Debug.LogWarning("[WeaponSystem] Cannot equip Bow while Shield is equipped.");
            return false;
        }

        if (newWeapon.type == WeaponType.Shield && otherSlot.type == WeaponType.Bow)
        {
            Debug.LogWarning("[WeaponSystem] Cannot equip Shield while Bow is equipped.");
            return false;
        }

        if (targetSlot == 1) slot1 = newWeapon;
        else slot2 = newWeapon;

        onWeaponChanged?.Invoke();
        Debug.Log("[WeaponSystem] Equipped " + newWeapon.name + " to slot " + targetSlot);
        return true;
    }

    public bool AutoEquip(WeaponData newWeapon)
    {
        EnsureSlots();

        if (slot1.type == WeaponType.None) return TryEquip(newWeapon, 1);
        if (slot2.type == WeaponType.None) return TryEquip(newWeapon, 2);
        return TryEquip(newWeapon, activeSlot);
    }

    public void ForcePrimaryWeapon(WeaponType type, string weaponName, int damage, Sprite icon = null)
    {
        EnsureSlots();

        slot1 = CreateWeapon(type, weaponName, damage, icon);
        slot2 = CreateWeapon(WeaponType.None, "Empty", 0);
        activeSlot = 1;

        onWeaponChanged?.Invoke();
        Debug.Log("[WeaponSystem] Level loadout forced: " + weaponName);
    }

    public bool IsShieldActive()
    {
        return GetActiveWeapon().type == WeaponType.Shield;
    }

    public int GetActiveDamage()
    {
        return GetActiveWeapon().damage;
    }

    public static WeaponData CreateWeapon(WeaponType type, string weaponName, int damage, Sprite icon = null)
    {
        return new WeaponData
        {
            type = type,
            name = weaponName,
            damage = damage,
            icon = icon
        };
    }

    private void EnsureSlots()
    {
        if (slot1 == null) slot1 = CreateWeapon(WeaponType.None, "Empty", 0);
        if (slot2 == null) slot2 = CreateWeapon(WeaponType.None, "Empty", 0);
        if (activeSlot != 1 && activeSlot != 2) activeSlot = 1;
    }
}
