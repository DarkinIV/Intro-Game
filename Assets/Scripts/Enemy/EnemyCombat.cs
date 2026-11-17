// AI use: Claude (Anthropic) wrote the attack list support, body-part hitboxes, enemy stats and projectiles.

using System.Collections.Generic;
using UnityEngine;

// Enemy attacks. EnemyController picks which attack to do, this plays it.
// Enemies don't need a weapon: their damage is their Strength, and their hitboxes are Hitbox
// components anywhere on their body (jaw, claws, spikes...), each with a part name.
// It also lets the player's scripts know whenever any enemy starts an attack.
public class EnemyCombat : CharacterCombat
{
    [System.Serializable]
    public class ProjectileSettings
    {
        public GameObject prefab;
        public Transform muzzle;               // where the projectile comes out
        public float speed = 12f;
        public float lifetime = 4f;
        public bool homing;
        public float turnSpeed = 180f;
    }

    // Used by the Acrobat's auto dodge and the Fortune Teller's foresight
    public static event System.Action<EnemyCombat> OnAnyEnemyAttack;

    static readonly int AttackIDParam = Animator.StringToHash("AttackID");

    public Transform lockOnTransform;

    // How hard it hits is Strength on CharacterStats, times the attack's damage multiplier
    [Header("Damage")]
    [SerializeField] DamageType damageType = DamageType.Physical;

    [Header("Projectile (ranged enemies)")]
    [SerializeField] ProjectileSettings projectile = new ProjectileSettings();

    // Set by BossPhases while the game runs
    public float attackSpeedMultiplier = 1f;

    float damageMultiplier = 1f; // from the attack being played
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
        damageMultiplier = attack.damageMultiplier;
        animator.SetFloat(AttackIDParam, attack.animationID);
        animator.SetFloat(AttackSpeedParam, attack.animationSpeed * attackSpeedMultiplier);
        TryAttack();
    }

    protected override int GetAttackDamage()
    {
        return Mathf.RoundToInt(stats.strength.Value * damageMultiplier);
    }

    protected override void OnSuccessfulHit(CharacterStats target, int damageDealt)
    {
        // Enemies hitting the player increases insanity
        if (target is PlayerStats player)
            player.IncreaseInsanity(player.damagedInsanityIncreaseAmount);
    }

    // Every hitbox on the enemy, found once the first time they're needed
    protected override List<Hitbox> AllHitboxes
    {
        get
        {
            if (hitboxes == null)
            {
                hitboxes = new List<Hitbox>(GetComponentsInChildren<Hitbox>(true));
                foreach (Hitbox hitbox in hitboxes)
                    hitbox.Setup(this);
            }

            return hitboxes;
        }
    }

    // The part name typed into the animation event picks the hitbox
    // empty means use all of them
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

    protected override bool CanShoot => projectile.prefab != null;

    protected override void Shoot()
    {
        Transform muzzle = projectile.muzzle != null ? projectile.muzzle : transform;
        LaunchProjectile(projectile.prefab, muzzle, projectile.speed, projectile.lifetime, projectile.homing, projectile.turnSpeed);
    }

    void AnnounceAttack()
    {
        OnAnyEnemyAttack?.Invoke(this);
    }
}
