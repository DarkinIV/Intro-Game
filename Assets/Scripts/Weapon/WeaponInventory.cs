using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    [SerializeField] Transform weaponHolder;
    [SerializeField] WeaponItem startingWeapon;
    [SerializeField] Transform weaponDropSpawnPoint;
    [SerializeField] GameObject weaponPickupPrefab;

    public WeaponItem CurrentWeapon { get; private set; }
    public GameObject WeaponObject { get; private set; }
    public WeaponHitbox Hitbox { get; private set; }
    public Transform Muzzle { get; private set; }

    public event System.Action<WeaponItem> OnWeaponChanged;

    void Start()
    {
        if (startingWeapon != null)
            Equip(startingWeapon);
    }

    public void Equip(WeaponItem weapon)
    {
        if (WeaponObject != null)
            Destroy(WeaponObject);

        WeaponObject = null;
        Hitbox = null;
        Muzzle = null;
        CurrentWeapon = weapon;

        if (weapon != null && weapon.weaponPrefab != null && weaponHolder != null)
        {
            WeaponObject = Instantiate(weapon.weaponPrefab, weaponHolder);
            WeaponObject.transform.localPosition = Vector3.zero;
            WeaponObject.transform.localRotation = Quaternion.identity;

            Hitbox = WeaponObject.GetComponentInChildren<WeaponHitbox>(true);
            WeaponMuzzle muzzle = WeaponObject.GetComponentInChildren<WeaponMuzzle>(true);
            Muzzle = muzzle != null ? muzzle.transform : weaponHolder;

            if (Hitbox != null)
                Hitbox.Setup(GetComponent<CharacterCombat>());
        }
        else if (weaponHolder != null)
        {
            Muzzle = weaponHolder;
        }

        OnWeaponChanged?.Invoke(CurrentWeapon);
    }

    // Preserves the pickup script's old method signature.
    public WeaponItem PickupWeapon(WeaponItem weapon, bool returnOldToInventory = false)
    {
        if (weapon == null) return null;
        WeaponItem previous = CurrentWeapon;
        Equip(weapon);
        if (returnOldToInventory && previous != null && Inventory.instance != null)
            Inventory.instance.Add(previous);
        return previous;
    }

    public void DropWeapon(WeaponItem weapon)
    {
        if (weapon == null || weaponPickupPrefab == null) return;
        Vector3 point = weaponDropSpawnPoint != null ? weaponDropSpawnPoint.position : transform.position + transform.forward + Vector3.up * 0.3f;
        GameObject pickup = Instantiate(weaponPickupPrefab, point, Quaternion.identity);
        WeaponPickup component = pickup.GetComponent<WeaponPickup>();
        if (component != null) component.weapon = weapon;
    }
}
