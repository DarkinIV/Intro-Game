using UnityEngine;

// An item that gets used up. For now that just means healing the player.
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Consumable")]
public class Consumable : Item
{
    public int healthGain;

    public override void Use()
    {
        Player.instance.playerStats.Heal(healthGain);
        Debug.Log(name + " consumed!");

        RemoveFromInventory();
    }

    protected override void OnValidate()
    {
        isStackable = true;
        base.OnValidate();
    }
}
