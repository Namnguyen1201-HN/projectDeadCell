using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the team's current level flow when a scene is loaded:
/// Spring = Sword, Summer = Bow, Winter = Sword, Autumn = Bow.
/// Scene names can be English or Vietnamese keywords.
/// </summary>
public class LevelFlowManager : MonoBehaviour
{
    private const int SwordDamage = 10;
    private const int BowDamage = 8;

    private static LevelFlowManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject obj = new GameObject(nameof(LevelFlowManager));
        instance = obj.AddComponent<LevelFlowManager>();
        DontDestroyOnLoad(obj);

        SceneManager.sceneLoaded += instance.HandleSceneLoaded;
        instance.StartCoroutine(instance.ApplyLoadoutNextFrame(SceneManager.GetActiveScene()));
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyLoadoutNextFrame(scene));
    }

    private IEnumerator ApplyLoadoutNextFrame(Scene scene)
    {
        yield return null;

        if (!TryGetLevelWeapon(scene.name, out WeaponSystem.WeaponType weaponType, out string weaponName, out int damage))
            yield break;

        Player player = FindObjectOfType<Player>();
        if (player == null) yield break;

        WeaponSystem weaponSystem = player.GetComponent<WeaponSystem>();
        if (weaponSystem == null)
            weaponSystem = player.gameObject.AddComponent<WeaponSystem>();

        weaponSystem.ForcePrimaryWeapon(weaponType, weaponName, damage);

        if (player.weaponSystem == null)
            player.weaponSystem = weaponSystem;

        Debug.Log($"[LevelFlowManager] Scene '{scene.name}' uses {weaponName}.");
    }

    private static bool TryGetLevelWeapon(
        string sceneName,
        out WeaponSystem.WeaponType weaponType,
        out string weaponName,
        out int damage)
    {
        string key = sceneName.ToLowerInvariant();

        if (key.Contains("spring") || key.Contains("xuan") || key.Contains("xuân"))
        {
            weaponType = WeaponSystem.WeaponType.Sword;
            weaponName = "Spring Sword";
            damage = SwordDamage;
            return true;
        }

        if (key.Contains("summer") || key.Contains("ha") || key.Contains("hạ"))
        {
            weaponType = WeaponSystem.WeaponType.Bow;
            weaponName = "Summer Bow";
            damage = BowDamage;
            return true;
        }

        if (key.Contains("winter") || key.Contains("dong") || key.Contains("đông"))
        {
            weaponType = WeaponSystem.WeaponType.Sword;
            weaponName = "Winter Sword";
            damage = SwordDamage;
            return true;
        }

        if (key.Contains("autumn") || key.Contains("fall") || key.Contains("thu"))
        {
            weaponType = WeaponSystem.WeaponType.Bow;
            weaponName = "Autumn Bow";
            damage = BowDamage;
            return true;
        }

        weaponType = WeaponSystem.WeaponType.None;
        weaponName = "Empty";
        damage = 0;
        return false;
    }
}
