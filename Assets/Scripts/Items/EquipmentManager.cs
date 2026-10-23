using UnityEngine;

// Keeps track of what the player has equipped.
// PlayerStats (for the stat bonuses) and WeaponInventory (for the model) listen for changes.
// It runs early so it's ready before the player gets spawned.
[DefaultExecutionOrder(-100)]
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager instance;

    public event System.Action<Equipment, Equipment> onEquipmentChanged;   // new item, old item

    public Equipment CurrentWeapon { get; private set; }

    void Awake()
    {
        instance = this;
    }

    // Returns false if the item couldn't be equipped
    public bool Equip(Equipment newItem, bool returnOldToInventory = true)
    {
        if (newItem == null)
            return false;

        Equipment oldItem = CurrentWeapon;

        // The old weapon goes back in the inventory, but only if there's room
        if (oldItem != null && returnOldToInventory && Inventory.instance.Add(oldItem) == 0)
        {
            Debug.Log("No room for " + oldItem.name + ", can't swap");
            return false;
        }

        CurrentWeapon = newItem;
        onEquipmentChanged?.Invoke(newItem, oldItem);
        return true;
    }
}
