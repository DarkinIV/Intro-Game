using UnityEngine;

// The Knight's combat: blocking, parrying, Spin Attack and Unkillable.
// The block animation has events to raise the shield and open/close the parry window,
// and the spin animation has a SpinHit event for when the damage happens.
public class KnightCombat : PlayerCombat, IDamageInterceptor
{
    [Header("Shield")]
    [SerializeField] Collider shieldCollider;
    [Range(0f, 1f)]
    [SerializeField] float damageNegation = 0.7f;    // 1 blocks the whole hit
    [SerializeField] float blockStaminaCost = 20f;   // per blocked hit (parries are free)

    [Header("Parry")]
    [SerializeField] float staggerTime = 1.2f;       // how long a parried enemy is stunned
    [SerializeField] float riposteMultiplier = 2f;   // extra damage on the next hit = strength * this
    [SerializeField] float riposteWindow = 2f;

    [Header("Effects")]
    [SerializeField] GameObject parryParticles;
    [SerializeField] GameObject spinParticles;
    [SerializeField] GameObject unkillableParticles;

    bool parryWindowOpen;
    SpinAttackData spin;
    float riposteUntil;
    float unkillableUntil;

    public bool IsBlocking { get; private set; }
    public bool ShieldRaised => shieldCollider.enabled;
    public bool RiposteReady => Time.time < riposteUntil;
    public bool UnkillableActive => Time.time < unkillableUntil;
    public float UnkillableRemaining => Mathf.Max(0f, unkillableUntil - Time.time);

    protected override void Awake()
    {
        base.Awake();
        shieldCollider.enabled = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        OnHit += Riposte;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        OnHit -= Riposte;
    }

    // Called every frame by KnightController with whether right click is held
    public void SetBlocking(bool blocking)
    {
        IsBlocking = blocking;
        animator.SetBool("Blocking", blocking);
        playerStats.StaminaRegenPaused = blocking;

        // Always drop the shield when letting go, even if the animation got cut off
        if (!blocking)
            LowerShield();
    }

    // Animation events on the block animation

    public void RaiseShield()
    {
        shieldCollider.enabled = true;
    }

    public void LowerShield()
    {
        shieldCollider.enabled = false;
        parryWindowOpen = false;
    }

    public void OpenParryWindow()
    {
        parryWindowOpen = true;
    }

    public void CloseParryWindow()
    {
        parryWindowOpen = false;
    }

    // Called by the Shield when an attack hits it before it hits the Knight
    public void Blocked(CharacterCombat attacker)
    {
        if (parryWindowOpen)
        {
            Parry(attacker);
            return;
        }

        // A normal block costs stamina and still lets some damage through.
        // With no stamina left the guard breaks and he takes the whole hit.
        bool guarded = playerStats.TrySpendStamina(blockStaminaCost);
        attacker.DealDamage(playerStats, guarded ? 1f - damageNegation : 1f);
    }

    // Blocks all the damage, stuns the enemy and makes the next hit stronger
    void Parry(CharacterCombat attacker)
    {
        riposteUntil = Time.time + riposteWindow;

        EnemyController enemy = attacker.GetComponent<EnemyController>();
        if (enemy != null)
            enemy.Stun(staggerTime);

        if (parryParticles != null)
            Destroy(Instantiate(parryParticles, shieldCollider.transform.position, Quaternion.identity), 2f);
    }

    // The first hit after a parry does bonus damage
    void Riposte(CharacterStats victim)
    {
        if (!RiposteReady || victim.IsDead)
            return;

        riposteUntil = 0f;
        DealAbilityDamage(victim, Mathf.RoundToInt(playerStats.strength.Value * riposteMultiplier));
    }

    // Spin Attack (F). Only plays the animation, the damage happens in SpinHit.
    public void Spin(SpinAttackData data)
    {
        spin = data;
        animator.SetTrigger("Spin");
    }

    // Animation event on the spin animation
    public void SpinHit()
    {
        int damage = Mathf.RoundToInt(playerStats.strength.Value * spin.strengthMultiplier);
        DealAreaDamage(transform.position + Vector3.up, spin.radius, damage);

        if (spinParticles != null)
            Destroy(Instantiate(spinParticles, transform), 2f);
    }

    // Unkillable (R)
    public void ActivateUnkillable(float duration)
    {
        unkillableUntil = Time.time + duration;

        if (unkillableParticles != null)
            Destroy(Instantiate(unkillableParticles, transform), duration);
    }

    // While Unkillable is on, nothing can take him below 1 health
    public int InterceptDamage(int damage, DamageType damageType, CharacterCombat attacker)
    {
        if (!UnkillableActive)
            return damage;

        return Mathf.Min(damage, playerStats.currentHealth - 1);
    }
}
