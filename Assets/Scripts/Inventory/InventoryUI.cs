using UnityEngine;

// Keeps the inventory slots on screen matching what's in the Inventory
public class InventoryUI : MonoBehaviour
{
    public Transform itemsParent;   // the object all the slots are under

    Inventory inventory;
    InventorySlot[] slots;

    void Awake()
    {
        slots = itemsParent.GetComponentsInChildren<InventorySlot>(true);
    }

    void Start()
    {
        inventory = Inventory.instance;
        inventory.onItemChangedCallback += UpdateUI;
        UpdateUI();
    }

    void OnEnable()
    {
        // The panel is hidden most of the time, so refresh it when it opens
        if (inventory != null)
            UpdateUI();
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.onItemChangedCallback -= UpdateUI;
    }

    public void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < inventory.items.Count)
                slots[i].AddItem(inventory.items[i]);
            else
                slots[i].ClearSlot();
        }
    }
}
