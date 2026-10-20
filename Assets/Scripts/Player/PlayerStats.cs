using UnityEngine;

public class PlayerStats : CharacterStats
{
    [Header("Health Potions")]
    [SerializeField] int maxHealthPotions = 3;
    [SerializeField] int startingHealthPotions = 3;

    [Header("Level Progression")]
    [SerializeField] int startingLevel = 1;
    [SerializeField] int baseExperienceToLevel = 100;
    [SerializeField] float experienceGrowth = 1.35f;

    [Header("Rogue Settings")]
    [SerializeField] int buffMilestoneInterval = 5;
    [SerializeField] int levelsLostOnDeath = 1;

    [Header("Stamina Regeneration")]
    [SerializeField] float staminaRegenDelay = 0.8f;
    [SerializeField] float staminaRegenPerSecond = 35f;

    public int CurrentHealthPotions { get; private set; }
    public int CurrentLevel { get; private set; }
    public int CurrentExperience { get; private set; }
    public int Karma { get; private set; }

    public int MaxHealthPotions => maxHealthPotions;

    [Header("Selected Character")]
    [SerializeField] CharacterData selectedCharacter;

    public CharacterData Character => selectedCharacter;
    public BasicAbility BasicAbility { get; private set; }
    public UltimateAbility Ultimate { get; private set; }

    [SerializeField] float ultimateChargeRateMultiplier = 1f;

    int highestAwardedMilestoneLevel; // no duplicate buffs on de-level/re-level

    public event System.Action<int> OnBuffMilestoneReached;
    public event System.Action<int> OnUltimateChargeChanged;


    public int ExperienceToNextLevel => Mathf.Max(1, Mathf.RoundToInt(baseExperienceToLevel * Mathf.Pow(experienceGrowth, CurrentLevel - 1)));

    public event System.Action<int, int> OnPotionsChanged;
    public event System.Action<int, int> OnExperienceChanged;
    public event System.Action<int> OnLevelChanged;
    public event System.Action<int> OnKarmaChanged;

    float lastStaminaSpendTime;
    float attackRateModifier;

    protected override void Awake()
    {
        if (RunSelection.SelectedCharacter != null)
        {
            selectedCharacter = RunSelection.SelectedCharacter;
        }

        if (selectedCharacter != null)
        {
            ApplyCharacterRanks();
        }

        base.Awake();

        CurrentLevel = Mathf.Max(1, startingLevel);
        CurrentHealthPotions = Mathf.Clamp(startingHealthPotions, 0, maxHealthPotions);

        if (selectedCharacter != null)
        {
            if (selectedCharacter.basicAbility != null)
            {
                BasicAbility = selectedCharacter.basicAbility.CreateRuntime() as BasicAbility;
            }

            if (selectedCharacter.ultimateAbility != null)
            {
                Ultimate = selectedCharacter.ultimateAbility.CreateRuntime() as UltimateAbility;
            }
        }
    }

    void ApplyCharacterRanks()
    {
        maxHealth.baseValue = selectedCharacter.Vitality.StartVitality();
        maxStamina.baseValue = selectedCharacter.Energy.StartEnergy();
        strength.baseValue = selectedCharacter.Strength.StartStrength();
        magic.baseValue = selectedCharacter.Magic.StartMagic();
        attackRate = selectedCharacter.AttackSpeed.StartAttackSpeed();
        moveSpeedBase = selectedCharacter.Pace.StartPace();
    }

    public override float GetAttackSpeed() => base.GetAttackSpeed() + attackRateModifier;

    void Start()
    {
        if (EquipmentManager.instance != null)
        {
            EquipmentManager.instance.onEquipmentChanged += OnEquipmentChanged;
        }

        lastStaminaSpendTime = -staminaRegenDelay;
    }
    void Update()
    {
        if (IsDead)
            return;

        RegenerateStamina();
    }

    // -------------------------
    // Stamina
    // -------------------------

    void RegenerateStamina()
    {
        if (currentStamina >= maxStamina.Value)
            return;

        if (Time.time - lastStaminaSpendTime < staminaRegenDelay)
            return;

        RegainStamina(staminaRegenPerSecond * Time.deltaTime);
    }

    public bool TrySpendStamina(float amount, bool showDamageEffect = true)
    {
        if (IsDead || amount <= 0)
            return false;

        if (currentStamina < 1)
            return false;

        TakeStaminaDamage(amount, showDamageEffect);

        lastStaminaSpendTime = Time.time;

        return true;
    }

    // -------------------------
    // Experience / Levels
    // -------------------------

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        CurrentExperience += amount;

        while (CurrentExperience >= ExperienceToNextLevel)
        {
            int requiredExperience = ExperienceToNextLevel;
            CurrentExperience -= requiredExperience;

            LevelUp();
        }

        OnExperienceChanged?.Invoke(CurrentExperience, ExperienceToNextLevel);
    }
    void LevelUp()
    {
        CurrentLevel++;

        int oldMaxHealth = maxHealth.Value;
        int oldMaxStamina = maxStamina.Value;

        int gainVit = selectedCharacter.Vitality.LevelVitality();
        int gainEng = selectedCharacter.Energy.LevelEnergy();
        int gainStr = selectedCharacter.Strength.LevelStrength();
        int gainMag = selectedCharacter.Magic.LevelMagic();

        float gainAS = selectedCharacter.AttackSpeed.LevelAttackSpeed();

        float gainPace = selectedCharacter.Pace.LevelPace();

        maxHealth.baseValue += gainVit;
        maxStamina.baseValue += gainEng;

        strength.baseValue += gainStr;
        magic.baseValue += gainMag;

        attackRate += gainAS;
        moveSpeedBase += gainPace;

        int actualHealthIncrease = maxHealth.Value - oldMaxHealth;

        int actualStaminaIncrease = maxStamina.Value - oldMaxStamina;

        Heal(actualHealthIncrease);
        RegainStamina(actualStaminaIncrease);

        OnLevelChanged?.Invoke(CurrentLevel);

        if (buffMilestoneInterval > 0 && CurrentLevel % buffMilestoneInterval == 0 && CurrentLevel > highestAwardedMilestoneLevel)
        {
            highestAwardedMilestoneLevel = CurrentLevel;
            OnBuffMilestoneReached?.Invoke(CurrentLevel);
        }
    }

    public void ApplyDeathPenalty()
    {
        RemoveLevels(levelsLostOnDeath);
    }

    void RemoveLevels(int count)
    {
        for (int i = 0; i < count; i++)
            LevelDown();
    }

    public void AddUltimateCharge(int damageDealt)
    {
        if (Ultimate == null || damageDealt <= 0) return;

        int before = Ultimate.CurrentCharge;
        Ultimate.AddCharge(damageDealt, ultimateChargeRateMultiplier);
        if (Ultimate.CurrentCharge != before)
            OnUltimateChargeChanged?.Invoke(Ultimate.CurrentCharge);
    }

    void LevelDown()
    {
        if (CurrentLevel <= 1)
            return;

        CurrentLevel--;

        maxHealth.baseValue -= selectedCharacter.Vitality.LevelVitality();
        maxStamina.baseValue -= selectedCharacter.Energy.LevelEnergy();
        strength.baseValue -= selectedCharacter.Strength.LevelStrength();
        magic.baseValue -= selectedCharacter.Magic.LevelMagic();
        attackRate = Mathf.Max(0.2f, attackRate - selectedCharacter.AttackSpeed.LevelAttackSpeed());
        moveSpeedBase = Mathf.Max(1f, moveSpeedBase - selectedCharacter.Pace.LevelPace());

        RefreshHealth();
        RefreshStamina();

        OnLevelChanged?.Invoke(CurrentLevel);
    }
    // -------------------------
    // Karma
    // -------------------------

    public void AddKarma(int amount)
    {
        if (amount <= 0)
            return;

        Karma += amount;

        OnKarmaChanged?.Invoke(Karma);
    }

    // -------------------------
    // Equipment
    // -------------------------

    void OnEquipmentChanged(Equipment newItem, Equipment oldItem)
    {
        if (oldItem != null)
        {
            strength.RemoveModifier(oldItem.damageModifier);
        }

        if (newItem != null)
        {
            strength.AddModifier(newItem.damageModifier);
        }

        WeaponItem oldWeapon = oldItem as WeaponItem;
        WeaponItem newWeapon = newItem as WeaponItem;

        if (oldWeapon != null)
        {
            attackRateModifier -= oldWeapon.attackRateModifier;
            maxStamina.RemoveModifier(oldWeapon.maxStaminaModifier);
            RemovePaceFlat(oldWeapon.moveSpeedModifier);
        }

        if (newWeapon != null)
        {
            attackRateModifier += newWeapon.attackRateModifier;
            maxStamina.AddModifier(newWeapon.maxStaminaModifier);
            AddPaceFlat(newWeapon.moveSpeedModifier);

            if (newWeapon.maxStaminaModifier > 0)
                RegainStamina(newWeapon.maxStaminaModifier);
        }


        RefreshStamina();
    }
    


    void OnDestroy()
    {
        if (EquipmentManager.instance != null)
        {
            EquipmentManager.instance.onEquipmentChanged -= OnEquipmentChanged;

        }
    }
}