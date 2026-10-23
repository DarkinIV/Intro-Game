using System;

// One slot in the inventory: which item and how many
[Serializable]
public class InventoryEntry
{
    public Item item;
    public int quantity;

    public InventoryEntry(Item item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public bool IsFull => quantity >= item.maxStack;
}
