using UnityEngine;

// An item the player can equip. Right now there's only a weapon slot.
[CreateAssetMenu(fileName = "New Equip", menuName = "Inventory/Equipment")]
public class Equipment : Item
{
    public EquipmentSlot equipSlot = EquipmentSlot.Weapon;
    public int damageModifier;

    public override void Use()
    {
        RemoveFromInventory();

        // If it couldn't be equipped, put it back in the inventory
        if (!EquipmentManager.instance.Equip(this))
            Inventory.instance.Add(this);
    }

    protected override void OnValidate()
    {
        isStackable = false;
        base.OnValidate();
    }
}

public enum EquipmentSlot { Weapon }
