using UnityEngine;

public class WeaponPickup : Interactable
{
    public WeaponItem weapon;

    public override void Interact()
    {
        if (weapon == null || WeaponInventory.instance == null)
            return;

        WeaponItem dropped = WeaponInventory.instance.PickupWeapon(weapon, returnOldToInventory: false);

        if (dropped != null)
            WeaponInventory.instance.DropWeapon(dropped);

        Debug.Log("Equipped " + weapon.name);
        Destroy(gameObject);
    }
}