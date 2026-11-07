// AI use: Claude (Anthropic) wrote the attack list support, body-part hitboxes, enemy stats and projectiles.
// Owner: (add name)

using System.Collections.Generic;
using UnityEngine;

// Enemy attacks. EnemyController picks which attack to do, this plays it.
// Enemies don't need a weapon: their damage is set here, and their hitboxes are Hitbox
// components anywhere on their body (jaw, claws, spikes...), each with a part name.
// It also lets the player's scripts know whenever any enemy starts an attack.
public class EnemyCombat : CharacterCombat
{
    // Used by the Acrobat's auto dodge and the Fortune Teller's foresight
    public static event System.Action<EnemyCombat> OnAnyEnemyAttack;

    public Transform lockOnTransform;

    [Header("Stats")]
    [SerializeField] DamageType damageType = DamageType.Physical;

    [Header("Projectile (ranged enemies)")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform muzzle;               // where the projectile comes out
    [SerializeField] float projectileSpeed = 12f;
    [SerializeField] float projectileLifetime = 4f;
    [SerializeField] bool homing;
    [SerializeField] float projectileTurnSpeed = 180f;

    [SerializeField] EnemyAttack currentAttack;
    List<Hitbox> hitboxes;
    List<Hitbox> chosenHitboxes = new List<Hitbox>();

    // The enemy only attacks when the controller tells it to, so it shouldn't queue combos
    protected override bool CanQueueCombo => false;

    protected override DamageType AttackDamageType => damageType;

    protected override void OnEnable()
    {
        base.OnEnable();
        OnAttack += AnnounceAttack;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        OnAttack -= AnnounceAttack;
    }

    // Picks the animation for this attack (AttackID) and starts it
    public void StartAttack(EnemyAttack attack)
    {
        currentAttack = attack;
        animator.SetFloat("AttackID", attack.animationID);
        TryAttack();
    }

    protected override int GetAttackDamage()
    {
        float multiplier = currentAttack != null ? currentAttack.damageMultiplier : 1f;
        return Mathf.RoundToInt(baseDamage * multiplier);
    }

    protected override void OnSuccessfulHit(CharacterStats target, int damageDealt) 
    {
        PlayerStats playerStats = target.GetComponent<PlayerStats>();
        playerStats.IncreaseInsanity(playerStats.damagedInsanityIncreaseAmount); // Enemies hitting the player increases insanity
    }

    // Every hitbox on the enemy. Found the first time they're needed,
    // so a weapon spawned at the start (by a WeaponInventory) is found too.
    protected override List<Hitbox> AllHitboxes
    {
        get
        {
            if (hitboxes == null || hitboxes.Count == 0)
            {
                hitboxes = new List<Hitbox>(GetComponentsInChildren<Hitbox>(true));
                foreach (Hitbox hitbox in hitboxes)
                    hitbox.Setup(this);
            }

            return hitboxes;
        }
    }

    // The part name typed into the animation event picks the hitbox (like LeftHand).
    // Leaving it empty uses all of them, which is fine for enemies with only one.
    protected override List<Hitbox> GetHitboxes(string part)
    {
        if (string.IsNullOrEmpty(part))
            return AllHitboxes;

        chosenHitboxes.Clear();

        foreach (Hitbox hitbox in AllHitboxes)
        {
            if (hitbox.partName == part)
                chosenHitboxes.Add(hitbox);
        }

        return chosenHitboxes;
    }

    protected override bool CanShoot => projectilePrefab != null;

    protected override void Shoot()
    {
        LaunchProjectile(projectilePrefab, muzzle != null ? muzzle : transform, projectileSpeed, projectileLifetime, homing, projectileTurnSpeed);
    }

    void AnnounceAttack()
    {
        OnAnyEnemyAttack?.Invoke(this);
    }
}
