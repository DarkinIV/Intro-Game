using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponItem : Equipment
{
    public enum AttackKind { Melee, Projectile }
    public enum ProjectileMode { Straight, Homing }

    [Header("Visuals")]
    public GameObject weaponPrefab;

    [Header("Attack")]
    public AttackKind attackKind = AttackKind.Melee;
    [Min(1)] public int comboLength = 1;
    [Min(0)] public int attackStaminaCost = 15;
    public float attackRateModifier = 0f;
    public int bonusDamage = 0;

    [Header("Projectile (if ranged)")]
    public GameObject projectilePrefab;
    public ProjectileMode projectileMode = ProjectileMode.Straight;
    [Min(0.1f)] public float projectileSpeed = 15f;
    [Min(0.1f)] public float projectileLifetime = 5f;
    public float projectileTurnSpeed = 360f;

    [Header("Existing Character Modifiers")]
    public int maxStaminaModifier = 0;
    public int moveSpeedModifier = 0;

    public enum WeaponType { Sword, Dagger, Staff }
    public WeaponType weaponType = WeaponType.Sword;
    [HideInInspector] public float attackRadiusOverride = 0f;

    protected override void OnValidate()
    {
        base.OnValidate();
        category = ItemCategory.Weapon;
        equipSlot = EquipmentSlot.Weapon;
        isStackable = false;
        maxStack = 1;
    }
}
