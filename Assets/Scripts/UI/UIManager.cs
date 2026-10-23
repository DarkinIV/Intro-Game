using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The main HUD: health, stamina and mana bars, level and experience, karma, potions,
// the ability bar, the interaction prompt and the inventory panel.
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [System.Serializable]
    public class CooldownDisplay
    {
        public Image fill;
        public TMP_Text timerText;
    }

    [Header("Player Bars")]
    [SerializeField] ResourceBar healthBar;
    [SerializeField] ResourceBar staminaBar;
    [SerializeField] ResourceBar manaBar;       // hidden for characters without mana

    [Header("Health Shake")]
    [SerializeField] float shakeDuration = 0.12f;
    [SerializeField] float shakeStrength = 5f;

    [Header("Player Resources")]
    [SerializeField] TMP_Text potionText;
    [SerializeField] TMP_Text karmaText;

    [Header("Resource Feedback")]
    [SerializeField] float karmaCountMinDuration = 0.25f;
    [SerializeField] float karmaCountMaxDuration = 0.75f;
    [SerializeField] int karmaDifferenceForMaxDuration = 100;   // gaining this much or more takes the max time
    [SerializeField] float karmaPunchScale = 1.12f;
    [SerializeField] float karmaPunchDuration = 0.18f;

    [Header("Player Progression")]
    [SerializeField] TMP_Text levelText;
    [SerializeField] Image experienceFill;
    [SerializeField] TMP_Text experienceText;   // optional

    [Header("Progression Feedback")]
    [SerializeField] float experienceFillDuration = 0.45f;
    [SerializeField] float levelWrapDuration = 0.22f;
    [SerializeField] float levelPunchScale = 1.18f;
    [SerializeField] float levelPunchDuration = 0.22f;

    [Header("Abilities")]
    [SerializeField] CooldownDisplay dash;              // optional
    [SerializeField] AbilitySlotUI abilitySlotPrefab;
    [SerializeField] Transform abilitySlotParent;       // a slot gets spawned here for each ability

    [Header("Interaction")]
    [SerializeField] GameObject interactionPanel;
    [SerializeField] TMP_Text interactionText;

    [Header("Inventory")]
    [SerializeField] GameObject inventoryPanel;

    PlayerStats playerStats;
    PlayerController playerController;
    Mana playerMana;

    Coroutine shakeRoutine;
    Coroutine experienceRoutine;
    Coroutine karmaRoutine;
    Coroutine karmaPunchRoutine;
    Coroutine levelPunchRoutine;

    Vector2 healthBarStartPosition;
    int displayedExperience;
    int displayedKarma;
    int displayedLevel;
    int levelsGained;   // level ups the experience bar still has to show

    RectTransform HealthBarRect => (RectTransform)healthBar.transform;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        playerStats = Player.instance.playerStats;
        playerController = Player.instance.GetComponent<PlayerController>();

        playerStats.OnHealthChanged += UpdatePlayerHealth;
        playerStats.OnDamaged += OnPlayerDamaged;
        playerStats.OnStaminaChanged += UpdatePlayerStamina;
        playerStats.OnStaminaDamaged += OnPlayerStaminaDamaged;
        playerStats.OnPotionsChanged += UpdatePotions;
        playerStats.OnExperienceChanged += UpdateExperience;
        playerStats.OnLevelChanged += UpdateLevel;
        playerStats.OnKarmaChanged += UpdateKarma;

        UpdatePlayerHealth(playerStats.currentHealth, playerStats.maxHealth.Value);
        UpdatePlayerStamina(playerStats.currentStamina, playerStats.maxStamina.Value);
        healthBarStartPosition = HealthBarRect.anchoredPosition;

        // Only characters with mana get a mana bar
        playerMana = Player.instance.GetComponent<Mana>();
        manaBar.gameObject.SetActive(playerMana != null);

        if (playerMana != null)
        {
            playerMana.OnManaChanged += UpdatePlayerMana;
            playerMana.OnManaSpent += OnPlayerManaSpent;
            UpdatePlayerMana(playerMana.Current, playerMana.Max);
        }

        // Set everything straight away at the start, without the animations
        UpdatePotions(playerStats.CurrentHealthPotions, playerStats.MaxHealthPotions);

        displayedLevel = playerStats.CurrentLevel;
        levelText.text = displayedLevel.ToString();

        displayedExperience = playerStats.CurrentExperience;
        SetExperienceBar(displayedExperience, playerStats.ExperienceToNextLevel);

        displayedKarma = playerStats.Karma;
        karmaText.text = displayedKarma.ToString();

        HideInteractionPrompt();
        inventoryPanel.SetActive(false);

        CreateAbilitySlots();
    }

    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdatePlayerHealth;
            playerStats.OnDamaged -= OnPlayerDamaged;
            playerStats.OnStaminaChanged -= UpdatePlayerStamina;
            playerStats.OnStaminaDamaged -= OnPlayerStaminaDamaged;
            playerStats.OnPotionsChanged -= UpdatePotions;
            playerStats.OnExperienceChanged -= UpdateExperience;
            playerStats.OnLevelChanged -= UpdateLevel;
            playerStats.OnKarmaChanged -= UpdateKarma;
        }

        if (playerMana != null)
        {
            playerMana.OnManaChanged -= UpdatePlayerMana;
            playerMana.OnManaSpent -= OnPlayerManaSpent;
        }
    }

    void Update()
    {
        UpdateDashCooldown();

        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();
    }

    // One slot for each ability, with the ultimate last
    void CreateAbilitySlots()
    {
        foreach (Ability ability in playerStats.Abilities)
            Instantiate(abilitySlotPrefab, abilitySlotParent).Setup(ability);

        if (playerStats.Ultimate != null)
            Instantiate(abilitySlotPrefab, abilitySlotParent).Setup(playerStats.Ultimate);
    }

    // Bars

    void UpdatePlayerHealth(int current, int max)
    {
        healthBar.Set(current, max);
    }

    void UpdatePlayerStamina(float current, int max)
    {
        staminaBar.Set(current, max);
    }

    void UpdatePlayerMana(float current, float max)
    {
        manaBar.Set(current, max);
    }

    // By the time these are called the stat has already gone down, so the old value is current + lost

    void OnPlayerDamaged(int damage)
    {
        healthBar.ShowLoss(playerStats.currentHealth + damage, playerStats.maxHealth.Value);

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeHealthBar());
    }

    void OnPlayerStaminaDamaged(int lost)
    {
        staminaBar.ShowLoss(playerStats.currentStamina + lost, playerStats.maxStamina.Value);
    }

    void OnPlayerManaSpent(float spent)
    {
        manaBar.ShowLoss(playerMana.Current + spent, playerMana.Max);
    }

    IEnumerator ShakeHealthBar()
    {
        RectTransform bar = HealthBarRect;

        for (float elapsed = 0f; elapsed < shakeDuration; elapsed += Time.unscaledDeltaTime)
        {
            // Shake less and less towards the end
            float strength = shakeStrength * (1f - elapsed / shakeDuration);
            bar.anchoredPosition = healthBarStartPosition + Random.insideUnitCircle * strength;
            yield return null;
        }

        bar.anchoredPosition = healthBarStartPosition;
        shakeRoutine = null;
    }

    void UpdatePotions(int current, int maximum)
    {
        potionText.text = current.ToString();
    }

    // Level and experience

    void UpdateLevel(int level)
    {
        levelText.text = level.ToString();

        if (level > displayedLevel)
        {
            levelsGained += level - displayedLevel;
            StartPunch(ref levelPunchRoutine, levelText.rectTransform, levelPunchScale, levelPunchDuration);
        }

        displayedLevel = level;
    }

    void UpdateExperience(int current, int required)
    {
        if (experienceRoutine != null)
            StopCoroutine(experienceRoutine);

        experienceRoutine = StartCoroutine(AnimateExperience(current, required, levelsGained));
        levelsGained = 0;
    }

    IEnumerator AnimateExperience(int targetExperience, int required, int levelUps)
    {
        // For each level up, fill the bar to the end first (from empty for any extra levels)
        for (int i = 0; i < levelUps; i++)
        {
            float from = i == 0 ? experienceFill.fillAmount : 0f;
            yield return FillExperienceBar(from, 1f, levelWrapDuration);
            experienceFill.fillAmount = 0f;
            displayedExperience = 0;
        }

        // Then fill up to the new amount
        float startFill = experienceFill.fillAmount;
        int startExperience = displayedExperience;
        float targetFill = targetExperience / (float)required;

        for (float elapsed = 0f; elapsed < experienceFillDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / experienceFillDuration);

            experienceFill.fillAmount = Mathf.Lerp(startFill, targetFill, t);
            displayedExperience = Mathf.RoundToInt(Mathf.Lerp(startExperience, targetExperience, t));
            SetExperienceText(displayedExperience, required);

            yield return null;
        }

        displayedExperience = targetExperience;
        SetExperienceBar(targetExperience, required);
        experienceRoutine = null;
    }

    IEnumerator FillExperienceBar(float from, float to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            experienceFill.fillAmount = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }

        experienceFill.fillAmount = to;
    }

    void SetExperienceBar(int current, int required)
    {
        experienceFill.fillAmount = current / (float)required;
        SetExperienceText(current, required);
    }

    void SetExperienceText(int current, int required)
    {
        if (experienceText != null)
            experienceText.text = current + " / " + required + " XP";
    }

    // Karma

    void UpdateKarma(int karma)
    {
        if (karmaRoutine != null)
            StopCoroutine(karmaRoutine);

        karmaRoutine = StartCoroutine(CountKarma(karma));

        if (karma > displayedKarma)
            StartPunch(ref karmaPunchRoutine, karmaText.rectTransform, karmaPunchScale, karmaPunchDuration);
    }

    // Counts the number up, bigger gains take a bit longer
    IEnumerator CountKarma(int targetKarma)
    {
        int startKarma = displayedKarma;
        float amount = Mathf.Abs(targetKarma - startKarma) / (float)karmaDifferenceForMaxDuration;
        float duration = Mathf.Lerp(karmaCountMinDuration, karmaCountMaxDuration, amount);

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            // Fast at first then slows down at the end
            float t = 1f - Mathf.Pow(1f - elapsed / duration, 3f);

            displayedKarma = Mathf.RoundToInt(Mathf.Lerp(startKarma, targetKarma, t));
            karmaText.text = displayedKarma.ToString();
            yield return null;
        }

        displayedKarma = targetKarma;
        karmaText.text = targetKarma.ToString();
        karmaRoutine = null;
    }

    // Quickly scales some text up and back down to draw attention to it
    void StartPunch(ref Coroutine routine, RectTransform target, float peakScale, float duration)
    {
        if (routine != null)
            StopCoroutine(routine);

        target.localScale = Vector3.one;
        routine = StartCoroutine(Punch(target, peakScale, duration));
    }

    IEnumerator Punch(RectTransform target, float peakScale, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float scale = Mathf.Lerp(1f, peakScale, Mathf.Sin(elapsed / duration * Mathf.PI));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    // Dash cooldown (only if it's been set up in the inspector)
    void UpdateDashCooldown()
    {
        float remaining = playerController.DashCooldownRemaining;
        bool coolingDown = remaining > 0f;

        if (dash.fill != null)
        {
            dash.fill.fillAmount = remaining / playerController.DashCooldownDuration;
            dash.fill.enabled = coolingDown;
        }

        if (dash.timerText != null)
        {
            dash.timerText.gameObject.SetActive(coolingDown);
            dash.timerText.text = remaining.ToString("0.0");
        }
    }

    // Interaction prompt and inventory

    public void ShowInteractionPrompt(string message)
    {
        interactionPanel.SetActive(true);
        interactionText.text = message;
    }

    public void HideInteractionPrompt()
    {
        interactionPanel.SetActive(false);
    }

    public void ToggleInventory()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }
}
