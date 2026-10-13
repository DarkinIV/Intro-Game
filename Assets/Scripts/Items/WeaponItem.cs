using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponItem : Equipment
{
    public enum WeaponType { Sword, Dagger, Staff }

    [Header("Weapon Type")]
    public WeaponType weaponType = WeaponType.Sword;

    [Header("Combat Modifiers")]
    [Tooltip("Added to base attackRate. Dagger ~+0.5, Sword 0, Greatsword ~-0.3")]
    public float attackRateModifier = 0f;

    [Tooltip("Stamina per light attack. Dagger ~10, Sword ~15, Greatsword ~25")]
    public int attackStaminaCost = 15;

    [Header("Reach Override")]
    [Tooltip("If > 0, overrides PlayerCombat.attackRadius. Dagger 0.8, Spear 1.8")]
    public float attackRadiusOverride = 0f;

    [Header("Character Modifiers")]
    [Tooltip("Added to maxStamina while equipped.")]
    public int maxStaminaModifier = 0;

    [Tooltip("Added to moveSpeed while equipped. Daggers ~+2, heavy ~-1")]
    public int moveSpeedModifier = 0;

    protected override void OnValidate()
    {
        base.OnValidate();
        category = ItemCategory.Weapon;
        equipSlot = EquipmentSlot.Weapon;
        isStackable = false;
        maxStack = 1;
    }
}
