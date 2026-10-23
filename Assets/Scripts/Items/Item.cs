using UnityEngine;

// Base class for every item. Other item types (Consumable, Equipment) inherit from this.
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    new public string name = "New Item";
    public Sprite icon = null;
    [TextArea] public string description;
    public bool showInInventory = true;
    public bool isStackable;
    [Min(1)] public int maxStack = 1;

    // Called when the item is clicked in the inventory
    public virtual void Use()
    {
    }

    public void RemoveFromInventory()
    {
        Inventory.instance.Remove(this);
    }

    protected virtual void OnValidate()
    {
        if (!isStackable)
            maxStack = 1;
    }
}
