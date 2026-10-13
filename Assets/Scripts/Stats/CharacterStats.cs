using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [System.Serializable]
    public class Stat
    {
        public int baseValue;
        int totalFlat;
        float totalPercent;

        public int Value => Mathf.RoundToInt((baseValue + totalFlat) * (1f + totalPercent));

        public void AddFlat(int amount)    => totalFlat += amount;
        public void RemoveFlat(int amount) => totalFlat -= amount;
        public void AddPercent(float m)    => totalPercent += m;
        public void RemovePercent(float m) => totalPercent -= m;

        // Equipment / weapons use flat modifiers
        public void AddModifier(int amount)    => AddFlat(amount);
        public void RemoveModifier(int amount) => RemoveFlat(amount);
    }


    [Header("Health")]
    public Stat maxHealth;
    public int currentHealth { get; protected set; }

    [Header("Stamina")]
    public Stat maxStamina;
    public float currentStamina { get; protected set; }

    [Header("Combat")]
    public Stat strength;  // melee damage
    public Stat magic;     // spell / ultimate damage
    public float attackRate = 1f;  // base attacks/sec (Attack Speed rank)
    float attackSpeedPct;           // percent buffs (e.g., +20% AS)

    [Header("Movement")]
    public float moveSpeedBase;  // Pace base value (set from rank / level up)
    float moveSpeedFlat;         // weapon flat bonuses
    float moveSpeedPct;          // buff percent
    public Stat turnSpeed;

    public bool IsDead { get; private set; }

    public float GetAttackSpeed() => attackRate * (1f + attackSpeedPct);
    public float GetMoveSpeed() => (moveSpeedBase + moveSpeedFlat) * (1f + moveSpeedPct);

    public void AddPaceFlat(float amount)    => moveSpeedFlat += amount;
    public void RemovePaceFlat(float amount) => moveSpeedFlat -= amount;

    // Buff helpers – 0.2f = +20%, -0.1f = -10%
    public void AddVitalityPercent(float m)
    {
        maxHealth.AddPercent(m);
        RefreshHealth();
    }
    public void RemoveVitalityPercent(float m)
    {
        maxHealth.RemovePercent(m);
        RefreshHealth();
    }

    public void AddEnergyPercent(float m)
    {
        maxStamina.AddPercent(m);
        RefreshStamina();
    }
    public void RemoveEnergyPercent(float m)
    {
        maxStamina.RemovePercent(m);
        RefreshStamina();
    }
    public void AddStrengthPercent(float m) => strength.AddPercent(m);
    public void RemoveStrengthPercent(float m) => strength.RemovePercent(m);
    public void AddMagicPercent(float m) => magic.AddPercent(m);
    public void RemoveMagicPercent(float m) => magic.RemovePercent(m);
    public void AddPacePercent(float m) => moveSpeedPct += m;
    public void RemovePacePercent(float m) => moveSpeedPct -= m;
    public void AddAttackSpeedPercent(float m) => attackSpeedPct += m;
    public void RemoveAttackSpeedPercent(float m) => attackSpeedPct -= m;


    public event System.Action<int, int> OnHealthChanged;
    public event System.Action<float, int> OnStaminaChanged;

    public event System.Action<int> OnDamaged;
    public event System.Action<int> OnStaminaDamaged;

    public event System.Action<int, DamageType> OnDamageTaken;

    public event System.Action OnHealthReachedZero;
    public event System.Action OnStaminaReachedZero;


    protected virtual void Awake()
    {
        currentHealth = maxHealth.Value;
        currentStamina = maxStamina.Value;
    }


    public void TakeDamage(
        int amount,
        DamageType damageType = DamageType.Physical)
    {
        if (IsDead || amount <= 0)
            return;

        int oldHealth = currentHealth;

        currentHealth = Mathf.Max(currentHealth - amount, 0);

        int damageTaken = oldHealth - currentHealth;

        if (damageTaken > 0)
        {
            OnDamaged?.Invoke(damageTaken);
            OnDamageTaken?.Invoke(damageTaken, damageType);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth.Value);

        if (currentHealth <= 0)
        {
            IsDead = true;
            OnHealthReachedZero?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        int oldHealth = currentHealth;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth.Value);

        if (currentHealth != oldHealth)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth.Value);
        }
    }

    public void TakeStaminaDamage(float amount, bool showDamageEffect = true)
    {
        if (IsDead || amount <= 0)
            return;

        float oldStamina = currentStamina;

        currentStamina = Mathf.Max(currentStamina - amount, 0f);

        float staminaLost = oldStamina - currentStamina;

        if (staminaLost > 0)
        {
            if (showDamageEffect)
            {
                OnStaminaDamaged?.Invoke(
                    Mathf.RoundToInt(staminaLost)
                );
            }

            OnStaminaChanged?.Invoke(
                currentStamina,
                maxStamina.Value
            );
        }

        if (currentStamina <= 0)
            OnStaminaReachedZero?.Invoke();
    }

    public void RegainStamina(float amount)
    {
        if (IsDead || amount <= 0)
            return;

        float oldStamina = currentStamina;

        currentStamina = Mathf.Min(currentStamina + amount, maxStamina.Value);

        if (!Mathf.Approximately(currentStamina, oldStamina))
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina.Value);
        }
    }

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