using System.Collections.Generic;
using UnityEngine;

// The player's inventory. Each entry in the list is one slot, and stackable items share a slot up to maxStack.
public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    public System.Action onItemChangedCallback;

    public int space = 10;   // number of slots
    public List<InventoryEntry> items = new List<InventoryEntry>();

    void Awake()
    {
        instance = this;
    }

    // Adds as many as will fit and returns how many actually went in
    public int Add(Item item, int amount = 1)
    {
        if (item == null || amount <= 0 || !item.showInInventory)
            return 0;

        int remaining = amount;

        // Top up stacks we already have first
        if (item.isStackable)
        {
            foreach (InventoryEntry entry in items)
            {
                if (remaining <= 0)
                    break;

                if (entry.item != item || entry.IsFull)
                    continue;

                int toAdd = Mathf.Min(item.maxStack - entry.quantity, remaining);
                entry.quantity += toAdd;
                remaining -= toAdd;
            }
        }

        // Then use empty slots for whatever is left
        while (remaining > 0 && items.Count < space)
        {
            int stackAmount = item.isStackable ? Mathf.Min(item.maxStack, remaining) : 1;
            items.Add(new InventoryEntry(item, stackAmount));
            remaining -= stackAmount;
        }

        if (remaining > 0)
            Debug.Log("Not enough room.");

        int added = amount - remaining;
        if (added > 0)
            onItemChangedCallback?.Invoke();

        return added;
    }

    // Returns true if all of them were removed
    public bool Remove(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int remaining = amount;

        // Go backwards so removing empty slots doesn't mess up the loop
        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventoryEntry entry = items[i];
            if (entry.item != item)
                continue;

            int removed = Mathf.Min(entry.quantity, remaining);
            entry.quantity -= removed;
            remaining -= removed;

            if (entry.quantity <= 0)
                items.RemoveAt(i);
        }

        if (remaining < amount)
            onItemChangedCallback?.Invoke();

        return remaining == 0;
    }

    public int GetItemCount(Item item)
    {
        int count = 0;

        foreach (InventoryEntry entry in items)
        {
            if (entry.item == item)
                count += entry.quantity;
        }

        return count;
    }
}
