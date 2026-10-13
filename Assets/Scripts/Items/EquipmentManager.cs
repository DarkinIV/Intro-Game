using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
	public static EquipmentManager instance;

	Equipment currentWeapon;

	public delegate void OnEquipmentChanged(Equipment newItem, Equipment oldItem);
	public event OnEquipmentChanged onEquipmentChanged;

	Inventory inventory;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		inventory = Inventory.instance;
	}

	public Equipment GetEquipment(EquipmentSlot slot)
	{
		return currentWeapon;
	}

	public void Equip(Equipment newItem, bool returnOldToInventory = true)
	{
		if (newItem == null)
			return;

		Equipment oldItem = currentWeapon;

		if (oldItem != null && returnOldToInventory && inventory != null)
		{
			bool wasAdded = inventory.Add(oldItem);
			if (!wasAdded)
			{
				Debug.LogWarning("Could not return " + oldItem.name + " to inventory before equipping " + newItem.name);
			}
		}

		onEquipmentChanged?.Invoke(newItem, oldItem);

		currentWeapon = newItem;
		Debug.Log(newItem.name + " equipped!");
	}

	public void UnequipCurrent()
	{
		if (currentWeapon == null) return;

		Equipment oldItem = currentWeapon;
		if (inventory != null)
			inventory.Add(oldItem);

		currentWeapon = null;
		onEquipmentChanged?.Invoke(null, oldItem);
	}
}
