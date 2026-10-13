using UnityEngine;

/* The base item class. All items should derive from this. */

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject {

	new public string name = "New Item";	// Name of the item
	public Sprite icon = null;				// Item icon
        [TextArea] public string description;
        public ItemCategory category = ItemCategory.Misc;
	public bool showInInventory = true;
        public bool isStackable;
        [Min(1)] public int maxStack = 1;

	// Called when the item is pressed in the inventory
	public virtual void Use ()
	{
		// Use the item
		// Something may happen
	}

	// Call this method to remove the item from inventory
	public void RemoveFromInventory ()
	{
                if (Inventory.instance != null)
                        Inventory.instance.Remove(this);
	}

        protected virtual void OnValidate()
        {
                if (!isStackable)
                        maxStack = 1;
                else
                        maxStack = Mathf.Max(1, maxStack);
        }

}
