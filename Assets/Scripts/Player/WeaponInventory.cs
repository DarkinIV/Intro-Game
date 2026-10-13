using UnityEngine;

[RequireComponent(typeof(EquipmentManager))]
public class WeaponInventory : MonoBehaviour
{
    public static WeaponInventory instance;

    [SerializeField] Transform weaponDropSpawnPoint;
    [SerializeField] GameObject weaponPickupPrefab;

    EquipmentManager equipmentManager;

    public WeaponItem CurrentWeapon { get; private set; }

    public event System.Action<WeaponItem> OnWeaponChanged;

    void Awake()
    {
        instance = this;
        equipmentManager = GetComponent<EquipmentManager>();
    }

    public WeaponItem PickupWeapon(WeaponItem newWeapon, bool returnOldToInventory = false)
    {
        if (newWeapon == null)
            return null;

        WeaponItem droppedWeapon = CurrentWeapon;

        equipmentManager.Equip(newWeapon, returnOldToInventory);

        CurrentWeapon = newWeapon;
        OnWeaponChanged?.Invoke(newWeapon);

        return droppedWeapon;
    }

    public void DropWeapon(WeaponItem weapon)
    {
        if (weapon == null || weaponPickupPrefab == null)
            return;

        Vector3 spawnPos = weaponDropSpawnPoint != null
            ? weaponDropSpawnPoint.position
            : transform.position + transform.forward * 0.8f + Vector3.up * 0.3f;

        GameObject dropGO = Instantiate(weaponPickupPrefab, spawnPos, Quaternion.identity);

        WeaponPickup wp = dropGO.GetComponent<WeaponPickup>();
        if (wp != null)
            wp.weapon = weapon;
    }
}