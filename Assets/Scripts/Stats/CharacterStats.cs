// AI use: simplified the code and calculations

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
    public Stat strength; // melee damage (and the damage of every enemy attack)
    public Stat magic; // spell damage

    [Header("Movement")]
    public float moveSpeedBase; // set from the Pace rank and level ups
    float moveSpeedFlat; // bonus from weapons
    float moveSpeedPct; // percent changes, like the Fool's curse
    public float turnSpeed = 10f; // how quickly it turns to face a new direction

    [Header("Poise")]
    [Tooltip("How much damage from hits it can take before it staggers. 0 means it never staggers")]
    public Stat maxPoise;
    [Tooltip("Poise goes back to full after this many seconds without being hit")]
    public float poiseResetTime = 10f;
    [Tooltip("How long a stagger lasts")]
    public float staggerTime = 0.6f;

    public bool IsDead { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public bool IsStaggered => Time.time < staggeredUntil;

    // Full again once it hasn't been hit for poiseResetTime
    public int CurrentPoise => Time.time - lastPoiseHitTime >= poiseResetTime ? maxPoise.Value : poise;

    float invulnerableUntil;
    IDamageInterceptor[] damageInterceptors;

    int poise;
    float lastPoiseHitTime = -999f;
    float staggeredUntil;
    float poiseLockedUntil;

    public event System.Action<int, int> OnHealthChanged;
    public event System.Action<float, int> OnStaminaChanged;
    public event System.Action<int> OnDamaged;
    public event System.Action<int> OnStaminaDamaged;
    public event System.Action<int, DamageType> OnDamageTaken;
    public event System.Action OnHealthReachedZero;

    public event System.Action OnStaggered;

    // An attack hit us while we were invulnerable (used for perfect dodges)
    public event System.Action<CharacterCombat> OnHitAvoided;

    protected virtual void Awake()
    {
        currentHealth = maxHealth.Value;
        currentStamina = maxStamina.Value;
        poise = maxPoise.Value;
        damageInterceptors = GetComponents<IDamageInterceptor>();
    }

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
            return;
        }

        // Only hits from a character wear down poise, not things like lava
        if (attacker != null)
            LosePoise(amount);
    }

    void LosePoise(int amount)
    {
        // No poise at all or still reeling from the last stagger (so it can't be stagger locked)
        if (maxPoise.Value <= 0 || IsStaggered || Time.time < poiseLockedUntil)
            return;

        poise = CurrentPoise - amount;
        lastPoiseHitTime = Time.time;

        if (poise <= 0)
        {
            poise = maxPoise.Value;
            staggeredUntil = Time.time + staggerTime;
            OnStaggered?.Invoke();
        }
    }

    // Hits don't wear down poise for a while (like during a stun)
    public void LockPoise(float seconds)
    {
        poiseLockedUntil = Mathf.Max(poiseLockedUntil, Time.time + seconds);
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
