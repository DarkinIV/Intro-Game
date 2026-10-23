using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One slot in the inventory UI. The slot's buttons call UseItem and RemoveItem.
public class InventorySlot : MonoBehaviour
{
    public Image icon;
    public Button removeButton;
    public TMP_Text quantityText;   // optional

    InventoryEntry entry;

    public void AddItem(InventoryEntry newEntry)
    {
        entry = newEntry;

        icon.sprite = entry.item.icon;
        icon.enabled = entry.item.icon != null;
        removeButton.interactable = true;

        // Only show the number for stacks of more than one
        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(entry.item.isStackable && entry.quantity > 1);
            quantityText.text = entry.quantity.ToString();
        }
    }

    public void ClearSlot()
    {
        entry = null;

        icon.sprite = null;
        icon.enabled = false;
        removeButton.interactable = false;

        if (quantityText != null)
            quantityText.gameObject.SetActive(false);
    }

    public void RemoveItem()
    {
        if (entry != null)
            Inventory.instance.Remove(entry.item, 1);
    }

    public void UseItem()
    {
        if (entry != null)
            entry.item.Use();
    }
}
