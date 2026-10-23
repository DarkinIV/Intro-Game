using UnityEngine;

// Health, stamina and the other stats. Used by the player and the enemies.
public class CharacterStats : MonoBehaviour
{
    // A stat with a base value plus whatever equipment adds to it
    [System.Serializable]
    public class Stat
    {
        public int baseValue;
        int modifiers;

        public int Value => baseValue + modifiers;

        public void AddModifier(int amount) => modifiers += amount;
        public void RemoveModifier(int amount) => modifiers -= amount;
    }

    [Header("Health")]
    public Stat maxHealth;
    public int currentHealth { get; protected set; }

    [Header("Stamina")]
    public Stat maxStamina;
    public float currentStamina { get; protected set; }

    [Header("Combat")]
    public Stat strength;           // melee damage
    public Stat magic;              // spell damage
    public float attackRate = 1f;   // attacks per second

    [Header("Movement")]
    public float moveSpeedBase;     // set from the Pace rank and level ups
    float moveSpeedFlat;            // bonus from weapons
    float moveSpeedPct;             // percent changes, like the Fool's curse
    public Stat turnSpeed;

    public bool IsDead { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    float invulnerableUntil;
    IDamageInterceptor[] damageInterceptors;

    public event System.Action<int, int> OnHealthChanged;
    public event System.Action<float, int> OnStaminaChanged;
    public event System.Action<int> OnDamaged;
    public event System.Action<int> OnStaminaDamaged;
    public event System.Action<int, DamageType> OnDamageTaken;
    public event System.Action OnHealthReachedZero;

    // An attack hit us while we were invulnerable (used for perfect dodges)
    public event System.Action<CharacterCombat> OnHitAvoided;

    protected virtual void Awake()
    {
        currentHealth = maxHealth.Value;
        currentStamina = maxStamina.Value;
        damageInterceptors = GetComponents<IDamageInterceptor>();
    }

    public virtual float GetAttackSpeed() => attackRate;
    public float GetMoveSpeed() => (moveSpeedBase + moveSpeedFlat) * (1f + moveSpeedPct);

    public void AddPaceFlat(float amount) => moveSpeedFlat += amount;
    public void RemovePaceFlat(float amount) => moveSpeedFlat -= amount;

    // 0.2 means 20% faster, -0.1 means 10% slower
    public void AddPacePercent(float percent) => moveSpeedPct += percent;
    public void RemovePacePercent(float percent) => moveSpeedPct -= percent;

    // Never shortens invulnerability that's already running, so a dash can't cut an ultimate short
    public void GrantInvulnerability(float seconds)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    public void NotifyHitAvoided(CharacterCombat attacker)
    {
        OnHitAvoided?.Invoke(attacker);
    }

    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical, CharacterCombat attacker = null)
    {
        if (IsDead || IsInvulnerable || amount <= 0)
            return;

        // Things like the Knight's Unkillable get to change the damage first
        foreach (IDamageInterceptor interceptor in damageInterceptors)
            amount = interceptor.InterceptDamage(amount, damageType, attacker);

        if (amount <= 0)
            return;

        currentHealth -= amount;

        OnDamaged?.Invoke(amount);
        OnDamageTaken?.Invoke(amount, damageType);
        OnHealthChanged?.Invoke(currentHealth, maxHealth.Value);

        if (currentHealth <= 0)
        {
            IsDead = true;
            OnHealthReachedZero?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0 || currentHealth >= maxHealth.Value)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth.Value);
        OnHealthChanged?.Invoke(currentHealth, maxHealth.Value);
    }

    // showDamageEffect is false for things like sprinting so the bar doesn't flash all the time
    public void TakeStaminaDamage(float amount, bool showDamageEffect = true)
    {
        if (IsDead || amount <= 0 || currentStamina <= 0f)
            return;

        float lost = Mathf.Min(amount, currentStamina);
        currentStamina -= lost;

        if (showDamageEffect)
            OnStaminaDamaged?.Invoke(Mathf.RoundToInt(lost));

        OnStaminaChanged?.Invoke(currentStamina, maxStamina.Value);
    }

    public void RegainStamina(float amount)
    {
        if (IsDead || amount <= 0 || currentStamina >= maxStamina.Value)
            return;

        currentStamina = Mathf.Min(currentStamina + amount, maxStamina.Value);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina.Value);
    }

    // Called after the max changes (level ups, equipment)
    protected void RefreshHealth()
    {
        currentHealth = Mathf.Min(currentHealth, maxHealth.Value);
        OnHealthChanged?.Invoke(currentHealth, maxHealth.Value);
    }

    protected void RefreshStamina()
    {
        currentStamina = Mathf.Min(currentStamina, maxStamina.Value);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina.Value);
    }
}
