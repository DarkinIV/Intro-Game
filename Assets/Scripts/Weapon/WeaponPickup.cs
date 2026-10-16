using UnityEngine;

public class WeaponPickup : Interactable
{
    public WeaponItem weapon;

    public override void Interact()
    {
        if (weapon == null || Player.instance == null) return;
        WeaponInventory inventory = Player.instance.GetComponent<WeaponInventory>();
        if (inventory == null) return;
        WeaponItem previous = inventory.PickupWeapon(weapon);
        if (previous != null) inventory.DropWeapon(previous);
        Destroy(gameObject);
    }
}
