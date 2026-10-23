// AI use: Claude added target checks, ability and area damage, and simplified the rest.
// Owner: (add name)

using System.Collections.Generic;
using UnityEngine;

// Attacking for both the player and the enemies.
// Animation events (OpenHitbox, FireProjectile, the combo window) decide when things happen during a swing,
// and AttackStateBehaviour tells us whether we're in an attack animation at all.
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(WeaponInventory))]
public class CharacterCombat : MonoBehaviour
{
    // How long the animator gets to actually start the attack after it's pressed
    const float AttackStartGrace = 0.3f;
    static readonly int AttackTrigger = Animator.StringToHash("Attack");

    [Header("Combat")]
    [SerializeField] protected int baseDamage = 10;

    public Transform Target;

    protected CharacterStats stats;
    protected Animator animator;
    protected WeaponInventory weapons;

    int activeAttackStates;   // can be 2 for a moment while blending into the next combo hit
    float pendingUntil;       // attack pressed but the animator hasn't started it yet
    bool canCombo;
    bool queued;
    bool interrupted;         // ignore events from an attack that got cancelled
    bool committed;           // the hit has happened
    bool recovered;           // the hit is over so we can move again

    bool Busy => activeAttackStates > 0 || Time.time < pendingUntil;

    // From pressing attack until the hit is over you can't move (or only slowly)
    public bool IsMovementLocked => Busy && !recovered;
    // During the hit itself you can't turn or dash
    public bool IsCommitted => committed && !recovered;
    public float MoveMultiplier => IsMovementLocked && weapons.CurrentWeapon != null ? weapons.CurrentWeapon.attackMoveMultiplier : 1f;

    public event System.Action OnAttack;    // attack was pressed
    public event System.Action OnStrike;    // the hitbox opened or the projectile was fired
    public event System.Action<CharacterStats> OnHit;

    protected virtual void Awake()
    {
        stats = GetComponent<CharacterStats>();
        animator = GetComponentInChildren<Animator>();
        weapons = GetComponent<WeaponInventory>();
    }

    protected virtual void OnEnable()
    {
        stats.OnHealthReachedZero += CancelAttack;
    }

    protected virtual void OnDisable()
    {
        stats.OnHealthReachedZero -= CancelAttack;

        // The animator might get disabled before it calls exit on the attack state
        activeAttackStates = 0;
        pendingUntil = 0f;
        committed = false;
        recovered = false;
        ResetAttackFlags();

        if (animator.isActiveAndEnabled)
            animator.ResetTrigger(AttackTrigger);
    }

    protected virtual bool CanQueueCombo => true;

    public virtual void TryAttack()
    {
        if (stats.IsDead || weapons.CurrentWeapon == null)
            return;

        if (!Busy)
        {
            StartAttack();
            return;
        }

        // Pressing during the combo window queues the next hit
        if (canCombo && CanQueueCombo)
            queued = true;
    }

    void StartAttack()
    {
        if (!CanAttack())
            return;

        queued = false;
        canCombo = false;
        pendingUntil = Time.time + AttackStartGrace;

        animator.SetTrigger(AttackTrigger);
        OnAttack?.Invoke();
    }

    // Used by dashing, stuns and dying
    public void CancelAttack()
    {
        interrupted = true;
        recovered = true;
        pendingUntil = 0f;
        ResetAttackFlags();
        animator.ResetTrigger(AttackTrigger);
    }

    void ResetAttackFlags()
    {
        canCombo = false;
        queued = false;
        CloseHitbox();
    }

    // Called by AttackStateBehaviour

    public void AttackStateEntered()
    {
        activeAttackStates++;
        pendingUntil = 0f;
        interrupted = false;
        canCombo = false;
        committed = false;
        recovered = false;
    }

    public void AttackStateExited()
    {
        activeAttackStates = Mathf.Max(0, activeAttackStates - 1);

        if (activeAttackStates == 0)
        {
            committed = false;
            recovered = false;
            ResetAttackFlags();
        }
    }

    // Animation events

    public void OpenComboWindow()
    {
        if (!interrupted)
            canCombo = true;
    }

    public void CloseComboWindow()
    {
        canCombo = false;
    }

    public void ContinueCombo()
    {
        canCombo = false;
        committed = false;

        if (!queued || interrupted || !CanQueueCombo || !CanAttack())
        {
            EndAttack();
            return;
        }

        queued = false;
        CloseHitbox();
        animator.SetTrigger(AttackTrigger);
        OnAttack?.Invoke();
    }

    // No next hit was queued, so the attack is over
    void EndAttack()
    {
        recovered = true;
        ResetAttackFlags();
        animator.ResetTrigger(AttackTrigger);
    }

    public void OpenHitbox()
    {
        if (interrupted)
            return;

        committed = true;

        if (weapons.Hitbox == null || !ConsumeAttackCost())
            return;

        weapons.Hitbox.StartSwing();
        OnStrike?.Invoke();
    }

    public void CloseHitbox()
    {
        if (committed)
            recovered = true;

        if (weapons.Hitbox != null)
            weapons.Hitbox.StopSwing();
    }

    public void FireProjectile()
    {
        WeaponItem weapon = weapons.CurrentWeapon;

        if (interrupted || weapon == null)
            return;

        committed = true;

        if (weapon.attackKind != WeaponItem.AttackKind.Ranged || !ConsumeAttackCost())
            return;

        Vector3 direction = GetAimDirection(weapons.Muzzle.position);
        GameObject projectile = Instantiate(weapon.projectilePrefab, weapons.Muzzle.position, Quaternion.LookRotation(direction));
        projectile.GetComponent<Projectile>().Launch(this, Target, weapon);

        OnStrike?.Invoke();
    }

    protected virtual Vector3 GetAimDirection(Vector3 origin)
    {
        return Target != null ? (Target.position + Vector3.up - origin).normalized : transform.forward;
    }

    // Each character's combat script can change these
    protected virtual bool CanAttack() => true;
    protected virtual bool ConsumeAttackCost() => true;
    protected virtual int GetAttackDamage() => baseDamage;
    protected virtual DamageType AttackDamageType => DamageType.Physical;
    protected virtual void OnSuccessfulHit(CharacterStats target, int damageDealt) { }

    // Not ourselves, not dead and not on our team (checked with tags)
    public bool IsValidTarget(CharacterStats victim)
    {
        if (victim == null || victim == stats || stats.IsDead || victim.IsDead)
            return false;

        return !gameObject.CompareTag(victim.gameObject.tag);
    }

    public bool CanDamage(CharacterStats victim)
    {
        if (!IsValidTarget(victim))
            return false;

        // They're dodging. Tell them so perfect dodges can be rewarded.
        if (victim.IsInvulnerable)
        {
            victim.NotifyHitAvoided(this);
            return false;
        }

        return true;
    }

    // multiplier is less than 1 when the hit was partly blocked
    public void DealDamage(CharacterStats victim, float multiplier = 1f)
    {
        if (!CanDamage(victim))
            return;

        int bonus = weapons.CurrentWeapon != null ? weapons.CurrentWeapon.bonusDamage : 0;
        int damage = Mathf.RoundToInt((GetAttackDamage() + bonus) * multiplier);

        if (damage <= 0)
            return;

        victim.TakeDamage(damage, AttackDamageType, this);

        OnSuccessfulHit(victim, damage);
        OnHit?.Invoke(victim);
    }

    // Same as a weapon hit but doesn't call OnHit, so things like the riposte don't trigger off it
    public void DealAbilityDamage(CharacterStats victim, int amount, DamageType damageType = DamageType.Physical)
    {
        if (!CanDamage(victim))
            return;

        victim.TakeDamage(amount, damageType, this);
        OnSuccessfulHit(victim, amount);
    }

    public void DealAreaDamage(Vector3 center, float radius, int amount, DamageType damageType = DamageType.Physical)
    {
        // A character can have more than one collider so make sure we only hit them once
        HashSet<CharacterStats> alreadyHit = new HashSet<CharacterStats>();

        foreach (Collider col in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            CharacterStats victim = col.GetComponentInParent<CharacterStats>();

            if (victim != null && alreadyHit.Add(victim))
                DealAbilityDamage(victim, amount, damageType);
        }
    }
}
