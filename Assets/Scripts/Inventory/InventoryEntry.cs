using System;

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

    public bool IsEmpty => item == null || quantity <= 0;
    public int MaxStack => item == null ? 1 : item.maxStack;
    public bool IsFull => quantity >= MaxStack;
}
