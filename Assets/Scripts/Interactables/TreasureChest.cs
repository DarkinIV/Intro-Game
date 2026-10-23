using System.Collections;
using UnityEngine;

[System.Serializable]
public class ChestItemEntry
{
    public Item item;
    [Min(1)] public int quantity = 1;
}

// Plays the open animation and then gives its loot to the player.
// Anything that doesn't fit stays in the chest so the player can come back for it.
public class TreasureChest : Interactable
{
    public ChestItemEntry[] loot;

    Animator animator;
    bool isOpen;
    bool isOpening;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Can interact while it's closed, or open with loot still left in it
    public override bool CanInteract(Transform player)
    {
        return !isOpening && (!isOpen || HasLoot()) && base.CanInteract(player);
    }

    public override void Interact()
    {
        if (isOpen)
            TakeLoot();
        else if (!isOpening)
            StartCoroutine(Open());
    }

    IEnumerator Open()
    {
        isOpening = true;
        animator.SetTrigger("Open");

        // Wait for the lid to open
        yield return new WaitForSeconds(1f);

        isOpening = false;
        isOpen = true;
        TakeLoot();
    }

    void TakeLoot()
    {
        foreach (ChestItemEntry entry in loot)
        {
            if (entry.quantity > 0)
                entry.quantity -= Inventory.instance.Add(entry.item, entry.quantity);
        }
    }

    bool HasLoot()
    {
        foreach (ChestItemEntry entry in loot)
        {
            if (entry.quantity > 0)
                return true;
        }

        return false;
    }
}
