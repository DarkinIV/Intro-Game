using UnityEngine;

public class ItemPickup : Interactable
{
    public Item item;
    [Min(1)] public int quantity = 1;

    public override void Interact()
    {
        PickUp();
    }

    void PickUp()
    {
        if (item == null)
            return;

        bool wasPickedUp = Inventory.instance.Add(item, Mathf.Max(1, quantity));

        if (wasPickedUp)
        {
            Debug.Log("Picked up " + item.name + " x" + Mathf.Max(1, quantity));
            Destroy(gameObject);
        }
    }
}
