using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    #region Singleton

    public static Inventory instance;

    void Awake()
    {
        instance = this;
    }

    #endregion

    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    public int space = 10;  // Amount of item spaces

    // Each entry is one occupied inventory slot.
    public List<InventoryEntry> items = new List<InventoryEntry>();

        // Add a new item if enough room
        public bool Add(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (!item.showInInventory)
            return false;

        int remaining = amount;

        if (item.isStackable)
        {
            for (int i = 0; i < items.Count && remaining > 0; i++)
            {
                InventoryEntry entry = items[i];

                if (entry.item != item || entry.IsFull)
                    continue;

                int freeSpace = Mathf.Max(0, item.maxStack - entry.quantity);
                int toAdd = Mathf.Min(freeSpace, remaining);
                entry.quantity += toAdd;
                remaining -= toAdd;
            }
        }

        while (remaining > 0)
        {
            if (items.Count >= space)
            {
                Debug.Log("Not enough room.");
                if (remaining != amount)
                    NotifyInventoryChanged();

                return false;
            }

            int stackAmount = item.isStackable
                ? Mathf.Min(item.maxStack, remaining)
                : 1;

            items.Add(new InventoryEntry(item, stackAmount));
            remaining -= stackAmount;
        }

        NotifyInventoryChanged();

        return true;
    }

    // Remove an item
    public bool Remove(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int remaining = amount;

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

        bool removedAny = remaining < amount;

        if (removedAny)
            NotifyInventoryChanged();

        return remaining == 0;
    }

    public bool HasItem(Item item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    public int GetItemCount(Item item)
    {
        if (item == null)
            return 0;

        int count = 0;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryEntry entry = items[i];
            if (entry.item == item)
                count += entry.quantity;
        }

        return count;
    }

    public bool CanAdd(Item item, int amount = 1)
    {
        if (item == null || amount <= 0 || !item.showInInventory)
            return false;

        int remaining = amount;

        if (item.isStackable)
        {
            for (int i = 0; i < items.Count && remaining > 0; i++)
            {
                InventoryEntry entry = items[i];

                if (entry.item != item || entry.IsFull)
                    continue;

                remaining -= Mathf.Max(0, item.maxStack - entry.quantity);
            }
        }

        if (remaining <= 0)
            return true;

        int freeSlots = Mathf.Max(0, space - items.Count);
        int perSlotCapacity = item.isStackable ? item.maxStack : 1;
        int slotsNeeded = Mathf.CeilToInt(remaining / (float)perSlotCapacity);

        return freeSlots >= slotsNeeded;
    }

    void NotifyInventoryChanged()
    {
        onItemChangedCallback?.Invoke();
    }
}
