using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponItem : Equipment
{
    public enum AttackKind { Melee, Ranged }
    public enum ProjectileMode { Straight, Homing }

    [Header("Visuals")]
    public GameObject weaponPrefab;

    [Header("Attack")]
    public AttackKind attackKind = AttackKind.Melee;
    [Min(0)] public int attackStaminaCost = 15;
    public float attackRateModifier = 0f;
    public int bonusDamage = 0;
    [Tooltip("How fast you can move while attacking. 0 means you stand still")]
    [Range(0f, 1f)] public float attackMoveMultiplier = 0f;

    [Header("Projectile (if ranged)")]
    public GameObject projectilePrefab;
    public ProjectileMode projectileMode = ProjectileMode.Straight;
    [Min(0.1f)] public float projectileSpeed = 15f;
    [Min(0.1f)] public float projectileLifetime = 5f;
    public float projectileTurnSpeed = 360f;

    [Header("Stat Bonuses")]
    public int maxStaminaModifier = 0;
    public int moveSpeedModifier = 0;

    protected override void OnValidate()
    {
        base.OnValidate();

        // Weapons always go in the weapon slot and never stack
        equipSlot = EquipmentSlot.Weapon;
        isStackable = false;
        maxStack = 1;
    }
}
