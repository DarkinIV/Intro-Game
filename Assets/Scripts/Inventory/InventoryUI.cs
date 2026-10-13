using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/* This object manages the inventory UI. */

public class InventoryUI : MonoBehaviour {

	public GameObject inventoryUI;	// The entire UI
	public Transform itemsParent;	// The parent object of all the items

	Inventory inventory;	// Our current inventory
	InventorySlot[] cachedSlots;

	void Awake()
	{
		CacheSlots(force: true);
	}

	void Start ()
	{
		inventory = Inventory.instance;
		if (inventory != null)
			inventory.onItemChangedCallback += UpdateUI;

		CacheSlots(force: true);
		UpdateUI();
	}

	void OnEnable()
	{
		CacheSlots(force: false);
		UpdateUI();
	}

	void OnDisable()
	{
	}

	void Update ()
	{
		
	}

	public void UpdateUI ()
	{
		CacheSlots(force: false);

		if (cachedSlots == null)
			return;

		int entryCount = inventory != null ? inventory.items.Count : 0;

		for (int i = 0; i < cachedSlots.Length; i++)
		{
			if (i < entryCount)
			{
				cachedSlots[i].AddItem(inventory.items[i]);
			}
			else
			{
				cachedSlots[i].ClearSlot();
			}
		}
	}

	void CacheSlots(bool force)
	{
		if (!force && cachedSlots != null && cachedSlots.Length > 0)
			return;

		Transform searchRoot = itemsParent != null ? itemsParent : transform;
		if (searchRoot == null)
		{
			cachedSlots = new InventorySlot[0];
			return;
		}

		cachedSlots = searchRoot.GetComponentsInChildren<InventorySlot>(includeInactive: true);
	}

	void OnDestroy()
	{
		if (inventory != null)
			inventory.onItemChangedCallback -= UpdateUI;
	}

}
