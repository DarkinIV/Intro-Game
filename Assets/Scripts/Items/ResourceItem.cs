using UnityEngine;

[CreateAssetMenu(fileName = "New Resource", menuName = "Inventory/Resource")]
public class ResourceItem : Item
{
    [Min(0)] public int craftingTier;

    protected override void OnValidate()
    {
        base.OnValidate();
        category = ItemCategory.Resource;
        isStackable = true;
        maxStack = Mathf.Max(1, maxStack);
    }
}
