using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

/* Sits on all InventorySlots. */

public class InventorySlot : MonoBehaviour {

	public Image icon;
	public Button removeButton;
	public TMP_Text quantityText;

	InventoryEntry entry;

	public InventoryEntry Entry => entry;
	public Item Item => entry != null ? entry.item : null;
	public int Quantity => entry != null ? entry.quantity : 0;

	public void AddItem (InventoryEntry newEntry)
	{
		entry = newEntry;

		if (entry == null || entry.IsEmpty)
		{
			ClearSlot();
			return;
		}

		if (icon != null)
		{
			icon.sprite = entry.item.icon;
			icon.enabled = entry.item.icon != null;
		}
		else
		{
			Debug.LogWarning($"InventorySlot '{name}' has no icon Image assigned; cannot render '{entry.item.name}'.", this);
		}

		if (removeButton != null)
		{
			removeButton.interactable = true;
		}

		if (quantityText != null)
		{
			quantityText.gameObject.SetActive(entry.item.isStackable && entry.quantity > 1);
			quantityText.text = entry.quantity.ToString();
		}
	}

	public void ClearSlot ()
	{
		entry = null;

		if (icon != null)
		{
			icon.sprite = null;
			icon.enabled = false;
		}

		if (removeButton != null)
		{
			removeButton.interactable = false;
		}

		if (quantityText != null)
		{
			quantityText.gameObject.SetActive(false);
			quantityText.text = string.Empty;
		}
	}

	public void RemoveItemFromInventory ()
	{
		if (entry == null || entry.IsEmpty)
			return;

		if (Inventory.instance == null)
			return;

		Inventory.instance.Remove(entry.item, 1);
	}

	public void UseItem ()
	{
		if (entry == null || entry.IsEmpty)
			return;

		Item item = entry.item;
		if (item != null)
		{
			item.Use();
		}
	}

}
