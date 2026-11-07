// AI use: Claude (Anthropic) moved the weapon parts of combat in here (hitbox, projectiles, weapon stats).
// Owner: (add name)

using System.Collections.Generic;
using UnityEngine;

// Combat for all the player characters. Each character has its own version of this (AcrobatCombat etc).
// The player's hitbox, projectiles and some stats come from the weapon they're holding.
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(WeaponInventory))]
public class PlayerCombat : CharacterCombat
{
    protected PlayerStats playerStats;
    protected WeaponInventory weapons;

    List<Hitbox> weaponHitbox = new List<Hitbox>();

    protected int AttackStaminaCost => weapons.CurrentWeapon != null ? weapons.CurrentWeapon.attackStaminaCost : 15;

    protected override void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        weapons = GetComponent<WeaponInventory>();
        base.Awake();
    }

    protected virtual void Update()
    {
        animator.SetFloat("AttackSpeed", playerStats.GetAttackSpeed());
    }

    // Weapon stats

    protected override bool IsArmed => weapons.CurrentWeapon != null;
    protected override float AttackMoveMultiplier => weapons.CurrentWeapon != null ? weapons.CurrentWeapon.attackMoveMultiplier : 1f;
    protected override int BonusDamage => weapons.CurrentWeapon != null ? weapons.CurrentWeapon.bonusDamage : 0;

    // The held weapon's hitbox (melee weapons only)
    protected override List<Hitbox> AllHitboxes
    {
        get
        {
            weaponHitbox.Clear();
            if (weapons.Hitbox != null)
                weaponHitbox.Add(weapons.Hitbox);
            return weaponHitbox;
        }
    }

    protected override bool CanShoot => weapons.CurrentWeapon != null && weapons.CurrentWeapon.attackKind == WeaponItem.AttackKind.Ranged;

    protected override void Shoot()
    {
        WeaponItem weapon = weapons.CurrentWeapon;
        bool homing = weapon.projectileMode == WeaponItem.ProjectileMode.Homing;
        LaunchProjectile(weapon.projectilePrefab, weapons.Muzzle, weapon.projectileSpeed, weapon.projectileLifetime, homing, weapon.projectileTurnSpeed);
    }

    // Stamina and damage

    protected override bool CanAttack()
    {
        return playerStats.currentStamina >= 1;
    }

    protected override bool ConsumeAttackCost()
    {
        return playerStats.TrySpendStamina(AttackStaminaCost);
    }

    protected override int GetAttackDamage()
    {
        return playerStats.strength.Value;
    }

    // Damage we deal charges the ultimate
    protected override void OnSuccessfulHit(CharacterStats target, int damageDealt)
    {
        playerStats.AddUltimateCharge(damageDealt);
        playerStats.IncreaseInsanity(playerStats.attackInsanityIncreaseAmount); // Successful Attacks increases insanity
    }

    // Shoot in the direction the player faces, tilted up or down to match the camera
    protected override Vector3 GetAimDirection(Vector3 origin)
    {
        float cameraPitch = Camera.main.transform.eulerAngles.x;
        return Quaternion.Euler(cameraPitch, transform.eulerAngles.y, 0f) * Vector3.forward;
    }
}
