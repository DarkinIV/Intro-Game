using UnityEngine;

[CreateAssetMenu(fileName = "New Equip", menuName = "Inventory/Equipment")]
public class Equipment : Item
{
	public EquipmentSlot equipSlot = EquipmentSlot.Weapon;
	public int damageModifier;

	public override void Use()
	{
		EquipmentManager.instance.Equip(this);
		RemoveFromInventory();
	}

        protected override void OnValidate()
        {
                base.OnValidate();
                category = equipSlot == EquipmentSlot.Weapon
                        ? ItemCategory.Weapon
                        : ItemCategory.Equipment;
                isStackable = false;
                maxStack = 1;
        }
}

public enum EquipmentSlot { Weapon }
