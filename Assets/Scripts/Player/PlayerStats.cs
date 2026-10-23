using System.Collections.Generic;
using UnityEngine;

// The player's stats. On top of CharacterStats this has levels, karma, potions,
// stamina regen and the abilities from the chosen character.
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
    [SerializeField] int levelsLostOnDeath = 1;

    [Header("Stamina Regeneration")]
    [SerializeField] float staminaRegenDelay = 0.8f;
    [SerializeField] float staminaRegenPerSecond = 35f;

    [Header("Selected Character")]
    [SerializeField] CharacterData selectedCharacter;

    public int CurrentHealthPotions { get; private set; }
    public int MaxHealthPotions => maxHealthPotions;
    public int CurrentLevel { get; private set; }
    public int CurrentExperience { get; private set; }
    public int Karma { get; private set; }
    public CharacterData Character => selectedCharacter;

    // Each level needs a bit more experience than the last one
    public int ExperienceToNextLevel => Mathf.RoundToInt(baseExperienceToLevel * Mathf.Pow(experienceGrowth, CurrentLevel - 1));

    // Movement bonuses from the Agility rank
    public int AirJumps => selectedCharacter.Agility.AirJumps();
    public float JumpHeightMultiplier => selectedCharacter.Agility.JumpHeightMultiplier();
    public float WallTimeMultiplier => selectedCharacter.Agility.WallTimeMultiplier();
    public float TraversalStaminaMultiplier => selectedCharacter.Agility.TraversalStaminaMultiplier();

    public IReadOnlyList<Ability> Abilities => abilities;
    public UltimateAbility Ultimate { get; private set; }

    // The Knight's block stops stamina coming back while it's held
    public bool StaminaRegenPaused { get; set; }

    public event System.Action<int, int> OnPotionsChanged;
    public event System.Action<int, int> OnExperienceChanged;
    public event System.Action<int> OnLevelChanged;
    public event System.Action<int> OnKarmaChanged;

    List<Ability> abilities = new List<Ability>();
    float lastStaminaSpendTime;
    float attackRateModifier;

    protected override void Awake()
    {
        // Use the character picked on the selection screen (the one set in the inspector is for testing)
        if (RunSelection.SelectedCharacter != null)
            selectedCharacter = RunSelection.SelectedCharacter;

        ApplyCharacterRanks();
        base.Awake();

        CurrentLevel = startingLevel;
        CurrentHealthPotions = startingHealthPotions;

        CreateAbilities();
    }

    void Start()
    {
        if (EquipmentManager.instance != null)
            EquipmentManager.instance.onEquipmentChanged += OnEquipmentChanged;

        lastStaminaSpendTime = -staminaRegenDelay;
    }

    void OnDestroy()
    {
        if (EquipmentManager.instance != null)
            EquipmentManager.instance.onEquipmentChanged -= OnEquipmentChanged;
    }

    void Update()
    {
        if (!IsDead)
            RegenerateStamina();
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

    void CreateAbilities()
    {
        foreach (CharacterData.AbilityBinding binding in selectedCharacter.abilities)
        {
            if (binding.ability == null)
                continue;

            Ability ability = binding.ability.CreateRuntime();
            ability.Key = binding.key;
            abilities.Add(ability);
        }

        if (selectedCharacter.ultimateAbility != null)
        {
            Ultimate = (UltimateAbility)selectedCharacter.ultimateAbility.CreateRuntime();
            Ultimate.Key = KeyCode.R;
        }
    }

    public override float GetAttackSpeed() => base.GetAttackSpeed() + attackRateModifier;

    public void AddUltimateCharge(int damageDealt)
    {
        if (Ultimate != null && damageDealt > 0)
            Ultimate.AddCharge(damageDealt);
    }

    // Stamina

    void RegenerateStamina()
    {
        if (StaminaRegenPaused || currentStamina >= maxStamina.Value)
            return;

        // Wait a moment after using stamina before it starts coming back
        if (Time.time - lastStaminaSpendTime < staminaRegenDelay)
            return;

        RegainStamina(staminaRegenPerSecond * Time.deltaTime);
    }

    // Works as long as there's at least 1 stamina left, even if the cost is more than that
    public bool TrySpendStamina(float amount, bool showDamageEffect = true)
    {
        if (IsDead || amount <= 0 || currentStamina < 1)
            return false;

        TakeStaminaDamage(amount, showDamageEffect);
        lastStaminaSpendTime = Time.time;
        return true;
    }

    // Levels

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        CurrentExperience += amount;

        // A big reward can give more than one level
        while (CurrentExperience >= ExperienceToNextLevel)
        {
            CurrentExperience -= ExperienceToNextLevel;
            LevelUp();
        }

        OnExperienceChanged?.Invoke(CurrentExperience, ExperienceToNextLevel);
    }

    void LevelUp()
    {
        CurrentLevel++;

        int oldMaxHealth = maxHealth.Value;
        int oldMaxStamina = maxStamina.Value;

        maxHealth.baseValue += selectedCharacter.Vitality.LevelVitality();
        maxStamina.baseValue += selectedCharacter.Energy.LevelEnergy();
        strength.baseValue += selectedCharacter.Strength.LevelStrength();
        magic.baseValue += selectedCharacter.Magic.LevelMagic();
        attackRate += selectedCharacter.AttackSpeed.LevelAttackSpeed();
        moveSpeedBase += selectedCharacter.Pace.LevelPace();

        // Fill up the extra health and stamina we just got
        Heal(maxHealth.Value - oldMaxHealth);
        RegainStamina(maxStamina.Value - oldMaxStamina);

        OnLevelChanged?.Invoke(CurrentLevel);
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

    public void ApplyDeathPenalty()
    {
        for (int i = 0; i < levelsLostOnDeath; i++)
            LevelDown();
    }

    // Karma

    public void AddKarma(int amount)
    {
        if (amount <= 0)
            return;

        Karma += amount;
        OnKarmaChanged?.Invoke(Karma);
    }

    // Equipment

    void OnEquipmentChanged(Equipment newItem, Equipment oldItem)
    {
        if (oldItem != null)
            strength.RemoveModifier(oldItem.damageModifier);

        if (newItem != null)
            strength.AddModifier(newItem.damageModifier);

        if (oldItem is WeaponItem oldWeapon)
        {
            attackRateModifier -= oldWeapon.attackRateModifier;
            maxStamina.RemoveModifier(oldWeapon.maxStaminaModifier);
            RemovePaceFlat(oldWeapon.moveSpeedModifier);
        }

        if (newItem is WeaponItem newWeapon)
        {
            attackRateModifier += newWeapon.attackRateModifier;
            maxStamina.AddModifier(newWeapon.maxStaminaModifier);
            AddPaceFlat(newWeapon.moveSpeedModifier);

            if (newWeapon.maxStaminaModifier > 0)
                RegainStamina(newWeapon.maxStaminaModifier);
        }

        RefreshStamina();
    }
}
