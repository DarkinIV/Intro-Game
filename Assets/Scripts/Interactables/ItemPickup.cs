using UnityEngine;

// An item lying in the world. Interacting puts it in the inventory.
public class ItemPickup : Interactable
{
    public Item item;
    [Min(1)] public int quantity = 1;

    public override void Interact()
    {
        int pickedUp = Inventory.instance.Add(item, quantity);
        if (pickedUp == 0)
            return;

        Debug.Log("Picked up " + item.name + " x" + pickedUp);
        quantity -= pickedUp;

        // If the inventory was too full for all of them, the rest stay on the ground
        if (quantity <= 0)
            Destroy(gameObject);
    }
}
