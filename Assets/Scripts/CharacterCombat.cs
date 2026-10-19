using UnityEngine;

// Shared combat mechanism between the player and the enemy AIs

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(WeaponInventory))]
public class CharacterCombat : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] protected int baseDamage = 10;

    protected CharacterStats stats;
    protected Animator animator;
    protected WeaponInventory weapons;

    bool attackStarted;
    bool canCombo;
    bool queued;

    public Transform Target { get; set; }

    public bool IsAttacking => animator != null && (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsTag("Attack")));

    public event System.Action OnAttack;
    public event System.Action<CharacterStats> OnHit;

    protected virtual void Awake()
    {
        stats = GetComponent<CharacterStats>();
        animator = GetComponentInChildren<Animator>();
        weapons = GetComponent<WeaponInventory>();
    }

    protected virtual bool CanQueueCombo => true;

    // ATTACK INPUT

    public virtual void TryAttack()
    {
        if (stats.IsDead || weapons.CurrentWeapon == null || animator == null)
            return;

        if (!attackStarted)
        {
            StartAttack();
            return;
        }

        // Only accept another input while the combo window is open
        if (canCombo && CanQueueCombo)
            queued = true;
    }

    void StartAttack()
    {
        if (!CanAttack())
            return;

        attackStarted = true;
        queued = false;
        canCombo = false;

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        OnAttack?.Invoke();
    }

    // ANIMATION EVENTS

    public void OpenComboWindow()
    {
        canCombo = true;
    }

    public void CloseComboWindow()
    {
        canCombo = false;
    }

    public void ContinueCombo()
    {
        canCombo = false;

        if (!queued || !CanQueueCombo || !CanAttack())
        {
            EndAttack();
            return;
        }
        queued = false;

        CloseHitbox();

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        OnAttack?.Invoke();
    }

    public void EndAttack()
    {
        attackStarted = false;
        canCombo = false;
        queued = false;

        CloseHitbox();
        animator.ResetTrigger("Attack");
    }

    // MELEE

    public void OpenHitbox()
    {
        if (weapons.Hitbox == null)
            return;

        weapons.Hitbox.StartSwing();
    }

    public void CloseHitbox()
    {
        if (weapons != null && weapons.Hitbox != null)
            weapons.Hitbox.StopSwing();
    }

    // RANGED
    // Needs 2 types, flying straight and homing missile
    // FireProjectile can be called during animation using Animation Events

    public void FireProjectile()
    {
        WeaponItem weapon = weapons.CurrentWeapon;
        if (weapon == null)
            return;
        GameObject orb = Instantiate(weapon.projectilePrefab, weapons.Muzzle.position, weapons.Muzzle.rotation);
        OrbController orbController = orb.GetComponent<OrbController>();
        orbController.shooter = this;

        Rigidbody orbBody = orb.GetComponent<Rigidbody>();
        orbBody.velocity = transform.forward * weapon.projectileSpeed;
        Destroy(orb, weapon.projectileLifetime);
    }
    

    // CHARACTER-SPECIFIC COMBAT

    protected virtual bool CanAttack() => true;

    protected virtual int GetAttackDamage() => baseDamage;

    protected virtual void OnSuccessfulHit(CharacterStats target, int damageDealt)
    {
    }

    public bool CanDamage(CharacterStats victim)
    {
        // Status validation
        if (victim == null || victim == stats || stats.IsDead || victim.IsDead)
            return false;

        // Friendly fire check based on tags
        if (gameObject.CompareTag(victim.gameObject.tag))
        {
            return false; // Same tags will return false
        }

        return true;
    }

    public void DealDamage(CharacterStats victim)
    {
        if (!CanDamage(victim))
            return;

        int before = victim.currentHealth;

        int bonus = weapons.CurrentWeapon != null ? weapons.CurrentWeapon.bonusDamage : 0;

        victim.TakeDamage(Mathf.Max(0, GetAttackDamage() + bonus));

        int dealt = Mathf.Max(0, before - victim.currentHealth);

        OnSuccessfulHit(victim, dealt);
        OnHit?.Invoke(victim);
    }

    protected virtual void OnDisable()
    {
        CloseHitbox();

        attackStarted = false;
        canCombo = false;
        queued = false;

        if (animator != null && animator.isActiveAndEnabled)
            animator.ResetTrigger("Attack");
    }
}
