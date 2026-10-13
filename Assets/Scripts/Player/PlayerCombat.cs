using System.Collections.Generic;
using UnityEngine;

/*
    Handles player melee attacks.

    The player faces the mouse position when attacking.
    Enemies inside the attack area take damage based on PlayerStats.
*/
[RequireComponent(typeof(PlayerStats))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] Transform attackPoint;
    [SerializeField] float attackRadius = 1.2f;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] int attackStaminaCost = 15;

    PlayerStats stats;
    Camera cam;

    float nextAttackTime;
    float currentAttackCooldown;

    public float AttackCooldownRemaining => Mathf.Max(0f, nextAttackTime - Time.time);

    public float AttackCooldownDuration => currentAttackCooldown;

    // These events can later be used by animations, sound, VFX,
    // camera shake, hit-stop and other combat feedback.
    public event System.Action OnAttack;
    public event System.Action<CharacterStats> OnHit;


    void Start()
    {
        stats = GetComponent<PlayerStats>();
        cam = Camera.main;

        currentAttackCooldown = 1f / Mathf.Max(stats.GetAttackSpeedWithWeapons(), 0.01f);
    }


    public void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        // Currently equipped weapon stats
        WeaponItem equippedWeapon = GetEquippedWeapon();
        int effectiveStaminaCost = equippedWeapon != null ? equippedWeapon.attackStaminaCost : attackStaminaCost;

        // Stamina gate
        if (!stats.TrySpendStamina(effectiveStaminaCost))
            return;

        currentAttackCooldown = 1f / Mathf.Max(stats.GetAttackSpeedWithWeapons(), 0.01f);

        nextAttackTime = Time.time + currentAttackCooldown;

        OnAttack?.Invoke();

        PerformHit();
    }


    /*
        Detects all enemies inside the melee attack area.

        A HashSet is used so an enemy with several colliders
        can only take damage once from the same attack.
    */
    float GetEffectiveAttackRadius()
    {
        WeaponItem equippedWeapon = GetEquippedWeapon();
        if (equippedWeapon != null && equippedWeapon.attackRadiusOverride > 0f)
            return equippedWeapon.attackRadiusOverride;
        return attackRadius;
    }


    WeaponItem GetEquippedWeapon()
    {
        if (WeaponInventory.instance == null)
            return null;
        return WeaponInventory.instance.CurrentWeapon;
    }


    void PerformHit()
    {
        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position,
            GetEffectiveAttackRadius(),
            enemyMask,
            QueryTriggerInteraction.Collide
        );

        HashSet<CharacterStats> damagedEnemies =
            new HashSet<CharacterStats>();

        foreach (Collider hit in hits)
        {
            CharacterStats enemyStats =
                hit.GetComponentInParent<CharacterStats>();

            if (enemyStats == null || enemyStats == stats)
                continue;

            if (!damagedEnemies.Add(enemyStats))
                continue;

            int dmg = stats.strength.Value;
            int hpBefore = enemyStats.currentHealth;
            enemyStats.TakeDamage(dmg);
            stats.AddUltimateCharge(hpBefore - enemyStats.currentHealth);

            OnHit?.Invoke(enemyStats);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            attackPoint.position,
            GetEffectiveAttackRadius()
        );
    }
}