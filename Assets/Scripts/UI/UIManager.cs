using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
    Controls the main overlay HUD.

    Displays player health, progression, resources,
    ability cooldowns, interaction prompts and UI panels.
*/
public class UIManager : MonoBehaviour
{
    public static UIManager instance;


    [System.Serializable]
    public class CooldownDisplay
    {
        public Image fill;
        public TMP_Text timerText;
    }


    [Header("Player Health")]
    [SerializeField] Image healthFill;
    [SerializeField] Image damageFill;
    [SerializeField] TMP_Text healthText;
    [SerializeField] RectTransform healthBarRoot;

    [Header("Health Feedback")]
    [SerializeField] float damageHoldTime = 0.2f;
    [SerializeField] float damageDrainSpeed = 0.8f;
    [SerializeField] float healFillDuration = 1f;
    [SerializeField] float shakeDuration = 0.12f;
    [SerializeField] float shakeStrength = 5f;

    [Header("Player Stamina")]
    [SerializeField] Image staminaFill;
    [SerializeField] Image staminaDamageFill;
    [SerializeField] TMP_Text staminaText;
    [SerializeField] RectTransform staminaBarRoot;

    [Header("Stamina Feedback")]
    [SerializeField] float staminaHoldTime = 0.2f;
    [SerializeField] float staminaDrainSpeed = 0.8f;
    [SerializeField] float staminaFillDuration = 1f;

    [Header("Bar Size")]
    [SerializeField] float pixelsPerHealth = 2f;
    [SerializeField] float pixelsPerStamina = 2f;

    [Header("Player Resources")]
    [SerializeField] TMP_Text potionText;
    [SerializeField] TMP_Text goldText;

    [Header("Resource Feedback")]
    [SerializeField] float goldCountMinDuration = 0.25f;
    [SerializeField] float goldCountMaxDuration = 0.75f;
    [SerializeField] int goldDifferenceForMaxDuration = 100;
    [SerializeField] float goldPunchScale = 1.12f;
    [SerializeField] float goldPunchDuration = 0.18f;

    [Header("Player Progression")]
    [SerializeField] TMP_Text levelText;
    [SerializeField] Image experienceFill;
    [SerializeField] TMP_Text experienceText;

    [Header("Progression Feedback")]
    [SerializeField] float experienceFillDuration = 0.45f;
    [SerializeField] float levelWrapDuration = 0.22f;
    [SerializeField] float levelPunchScale = 1.18f;
    [SerializeField] float levelPunchDuration = 0.22f;

    [Header("Abilities")]
    [SerializeField] CooldownDisplay basicAttack;
    [SerializeField] CooldownDisplay dash;

    [Header("Interaction")]
    [SerializeField] GameObject interactionPanel;
    [SerializeField] TMP_Text interactionText;

    [Header("Inventory")]
    [SerializeField] GameObject inventoryPanel;


    public PlayerStats playerStats;
    PlayerController playerController;
    PlayerCombat playerCombat;

    Coroutine damageDrainRoutine;
    Coroutine healFillRoutine;
    Coroutine shakeRoutine;
    Coroutine staminaDamageDrainRoutine;
    Coroutine staminaHealFillRoutine;
    Coroutine experienceRoutine;
    Coroutine goldRoutine;
    Coroutine goldPunchRoutine;
    Coroutine levelPunchRoutine;

    Vector2 healthBarStartPosition;
    Vector2 staminaBarStartPosition;
    int lastActualHealth = -1;
    float lastActualStamina = -1;

    bool initialising = true;

    int displayedExperience;
    int displayedGold;
    int lastDisplayedLevel = -1;
    int pendingLevelWraps;


    void Awake()
    {
        instance = this;
    }


    void Start()
    {
        playerStats = Player.instance.playerStats;
        playerController = Player.instance.GetComponent<PlayerController>();
        playerCombat = Player.instance.GetComponent<PlayerCombat>();

        playerStats.OnHealthChanged += UpdatePlayerHealth;
        playerStats.OnDamaged += OnPlayerDamaged;
        playerStats.OnStaminaChanged += UpdatePlayerStamina;
        playerStats.OnStaminaDamaged += OnPlayerStaminaDamaged;
        playerStats.OnPotionsChanged += UpdatePotions;
        playerStats.OnExperienceChanged += UpdateExperience;
        playerStats.OnLevelChanged += UpdateLevel;
        playerStats.OnGoldChanged += UpdateGold;

        UpdatePlayerHealth(playerStats.currentHealth, playerStats.maxHealth.Value);

        UpdatePlayerStamina(playerStats.currentStamina, playerStats.maxStamina.Value);

        if (damageFill != null && healthFill != null)
            damageFill.fillAmount = healthFill.fillAmount;

        if (staminaDamageFill != null && staminaFill != null)
            staminaDamageFill.fillAmount = staminaFill.fillAmount;

        if (healthBarRoot != null)
            healthBarStartPosition = healthBarRoot.anchoredPosition;

        if (staminaBarRoot != null)
            staminaBarStartPosition = staminaBarRoot.anchoredPosition;

        UpdatePotions(playerStats.CurrentHealthPotions, playerStats.MaxHealthPotions);

        UpdateExperience(playerStats.CurrentExperience, playerStats.ExperienceToNextLevel);

        UpdateLevel(playerStats.CurrentLevel);
        UpdateGold(playerStats.Gold);

        initialising = false;

        HideInteractionPrompt();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        UpdateCooldownDisplay(basicAttack, 0f, 1f);
        UpdateCooldownDisplay(dash, 0f, 1f);
    }


    void Update()
    {
        //UpdateCooldownDisplay(basicAttack, playerCombat.AttackCooldownRemaining, playerCombat.AttackCooldownDuration);

        UpdateCooldownDisplay(dash, playerController.DashCooldownRemaining, playerController.DashCooldownDuration);

        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();
    }


    void UpdatePlayerHealth(int currentHealth, int maxHealth)
    {
        UpdateHealthBarWidth(maxHealth);
        float targetPercent = maxHealth > 0 ? Mathf.Clamp01(currentHealth / (float)maxHealth) : 0f;

        bool isInitialUpdate = lastActualHealth < 0;
        bool healed = !isInitialUpdate && currentHealth > lastActualHealth;

        if (healFillRoutine != null)
        {
            StopCoroutine(healFillRoutine);
            healFillRoutine = null;
        }

        if (healthFill != null)
        {
            if (healed)
            {
                healFillRoutine = StartCoroutine(AnimateHealingFill(targetPercent));
            }
            else
            {
                healthFill.fillAmount = targetPercent;
            }
        }

        if (healthText != null)
            healthText.text = currentHealth + " / " + maxHealth;

        if (!healed && damageFill != null && healthFill != null && damageFill.fillAmount < healthFill.fillAmount)
        {
            damageFill.fillAmount = healthFill.fillAmount;
        }

        lastActualHealth = currentHealth;
    }


    void UpdatePlayerStamina(float currentStamina, int maxStamina)
    {
        UpdateStaminaBarWidth(maxStamina);

        float targetPercent = maxStamina > 0 ? Mathf.Clamp01(currentStamina / (float)maxStamina) : 0f;

        if (staminaFill != null)
        {
            staminaFill.fillAmount = targetPercent;
        }

        if (staminaText != null)
        {
            staminaText.text = currentStamina + " / " + maxStamina;
        }

        // If there is no stamina damage animation,
        // keep the damage fill attached to the real stamina fill.
        if (staminaDamageDrainRoutine == null && staminaDamageFill != null && staminaFill != null)
        {
            staminaDamageFill.fillAmount = staminaFill.fillAmount;
        }

        lastActualStamina = currentStamina;
    }

    void UpdateHealthBarWidth(int maxHealth)
    {
        healthBarRoot.sizeDelta = new Vector2(maxHealth * pixelsPerHealth, healthBarRoot.sizeDelta.y);
    }

    void UpdateStaminaBarWidth(int maxStamina)
    {
        staminaBarRoot.sizeDelta = new Vector2(maxStamina * pixelsPerStamina, staminaBarRoot.sizeDelta.y);
    }

    void OnPlayerStaminaDamaged(int staminaLost)
    {
        if (staminaLost <= 0)
            return;

        if (staminaDamageFill != null && staminaFill != null)
        {
            float previousActualPercent =
                playerStats != null && playerStats.maxStamina.Value > 0 ? Mathf.Clamp01(lastActualStamina / (float)playerStats.maxStamina.Value) : staminaFill.fillAmount;

            staminaDamageFill.fillAmount = Mathf.Max(staminaDamageFill.fillAmount, previousActualPercent);

            if (staminaDamageDrainRoutine != null)
                StopCoroutine(staminaDamageDrainRoutine);

            staminaDamageDrainRoutine = StartCoroutine(DrainStaminaDamageFill());
        }
    }


    IEnumerator DrainStaminaDamageFill()
    {
        yield return new WaitForSecondsRealtime(staminaHoldTime);

        while (staminaDamageFill != null && staminaFill != null && staminaDamageFill.fillAmount > staminaFill.fillAmount + 0.001f)
        {
            staminaDamageFill.fillAmount = Mathf.MoveTowards(staminaDamageFill.fillAmount, staminaFill.fillAmount, staminaDrainSpeed * Time.unscaledDeltaTime);

            yield return null;
        }

        if (staminaDamageFill != null && staminaFill != null)
            staminaDamageFill.fillAmount = staminaFill.fillAmount;

        staminaDamageDrainRoutine = null;
    }


    IEnumerator AnimateHealingFill(float targetPercent)
    {
        if (healthFill == null)
            yield break;

        float startPercent = healthFill.fillAmount;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, healFillDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = SmoothStep01(t);
            float amount = Mathf.Lerp(startPercent, targetPercent, easedT);

            healthFill.fillAmount = amount;

            if (damageFill != null && damageFill.fillAmount < amount)
                damageFill.fillAmount = amount;

            yield return null;
        }

        healthFill.fillAmount = targetPercent;

        if (damageFill != null && damageFill.fillAmount < targetPercent)
            damageFill.fillAmount = targetPercent;

        healFillRoutine = null;
    }


    void OnPlayerDamaged(int damageAmount)
    {
        if (damageAmount <= 0)
            return;

        if (damageFill != null && healthFill != null)
        {
            float previousActualPercent =
                playerStats != null && playerStats.maxHealth.Value > 0 ? Mathf.Clamp01(lastActualHealth / (float)playerStats.maxHealth.Value) : healthFill.fillAmount;

            damageFill.fillAmount = Mathf.Max(damageFill.fillAmount, previousActualPercent);

            if (damageDrainRoutine != null)
                StopCoroutine(damageDrainRoutine);

            damageDrainRoutine = StartCoroutine(DrainDamageFill());
        }

        if (healthBarRoot != null)
        {
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);

            shakeRoutine = StartCoroutine(ShakeHealthBar());
        }
    }


    IEnumerator DrainDamageFill()
    {
        yield return new WaitForSecondsRealtime(damageHoldTime);

        while (damageFill != null && healthFill != null && damageFill.fillAmount > healthFill.fillAmount + 0.001f)
        {
            damageFill.fillAmount = Mathf.MoveTowards(damageFill.fillAmount, healthFill.fillAmount, damageDrainSpeed * Time.unscaledDeltaTime);

            yield return null;
        }

        if (damageFill != null && healthFill != null)
            damageFill.fillAmount = healthFill.fillAmount;

        damageDrainRoutine = null;
    }


    IEnumerator ShakeHealthBar()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, shakeDuration));
            float strength = shakeStrength * (1f - t);

            healthBarRoot.anchoredPosition = healthBarStartPosition + Random.insideUnitCircle * strength;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        healthBarRoot.anchoredPosition = healthBarStartPosition;
        shakeRoutine = null;
    }


    void UpdatePotions(int current, int maximum)
    {
        if (potionText != null)
            potionText.text = current.ToString();
    }


    void UpdateExperience(int current, int required)
    {
        float targetFill = required > 0 ? Mathf.Clamp01(current / (float)required) : 0f;

        if (initialising)
        {
            if (experienceFill != null)
                experienceFill.fillAmount = targetFill;

            displayedExperience = current;

            if (experienceText != null)
                experienceText.text = current + " / " + required + " XP";

            return;
        }

        if (experienceRoutine != null)
            StopCoroutine(experienceRoutine);

        int wraps = pendingLevelWraps;
        pendingLevelWraps = 0;

        experienceRoutine = StartCoroutine(AnimateExperience(current, required, targetFill, wraps));
    }


    IEnumerator AnimateExperience(
        int targetExperience,
        int required,
        float targetFill,
        int levelWraps)
    {
        if (experienceFill == null)
        {
            displayedExperience = targetExperience;

            if (experienceText != null)
                experienceText.text = targetExperience + " / " + required + " XP";

            experienceRoutine = null;
            yield break;
        }

        // Normal XP gain: ease directly towards the new amount.
        if (levelWraps <= 0)
        {
            float startFill = experienceFill.fillAmount;
            int startExperience = displayedExperience;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, experienceFillDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = SmoothStep01(t);

                experienceFill.fillAmount = Mathf.Lerp(startFill, targetFill, easedT);

                displayedExperience = Mathf.RoundToInt(Mathf.Lerp(startExperience, targetExperience, easedT));

                if (experienceText != null)
                {
                    experienceText.text = displayedExperience + " / " + required + " XP";
                }

                yield return null;
            }
        }
        else
        {
            // When levelling up, visually finish the old bar first.
            yield return AnimateExperienceFillSegment(experienceFill.fillAmount, 1f, levelWrapDuration);

            // If one reward causes several levels, give every level a
            // visible 0 -> 100% sweep instead of skipping them.
            for (int i = 1; i < levelWraps; i++)
            {
                experienceFill.fillAmount = 0f;

                yield return AnimateExperienceFillSegment(0f, 1f, levelWrapDuration);
            }

            // Fill the leftover XP in the new level.
            experienceFill.fillAmount = 0f;
            displayedExperience = 0;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, experienceFillDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = SmoothStep01(t);

                experienceFill.fillAmount =
                    Mathf.Lerp(0f, targetFill, easedT);

                displayedExperience = Mathf.RoundToInt(Mathf.Lerp(0f, targetExperience, easedT));

                if (experienceText != null)
                {
                    experienceText.text = displayedExperience + " / " + required + " XP";
                }

                yield return null;
            }
        }

        experienceFill.fillAmount = targetFill;
        displayedExperience = targetExperience;

        if (experienceText != null)
        {
            experienceText.text = targetExperience + " / " + required + " XP";
        }

        experienceRoutine = null;
    }


    IEnumerator AnimateExperienceFillSegment(float from, float to, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = SmoothStep01(t);

            experienceFill.fillAmount = Mathf.Lerp(from, to, easedT);

            yield return null;
        }

        experienceFill.fillAmount = to;
    }


    void UpdateLevel(int level)
    {
        if (levelText != null)
            levelText.text = level.ToString();

        if (initialising || lastDisplayedLevel < 0)
        {
            lastDisplayedLevel = level;
            return;
        }

        if (level > lastDisplayedLevel)
        {
            pendingLevelWraps += level - lastDisplayedLevel;

            if (levelText != null)
            {
                if (levelPunchRoutine != null)
                    StopCoroutine(levelPunchRoutine);

                levelText.rectTransform.localScale = Vector3.one;

                levelPunchRoutine = StartCoroutine(PulseScale(levelText.rectTransform, levelPunchScale, levelPunchDuration));
            }
        }

        lastDisplayedLevel = level;
    }


    void UpdateGold(int gold)
    {
        if (goldText == null)
            return;

        if (initialising)
        {
            displayedGold = gold;
            goldText.text = gold.ToString();
            return;
        }

        if (goldRoutine != null)
            StopCoroutine(goldRoutine);

        goldRoutine = StartCoroutine(AnimateGold(gold));

        if (gold > displayedGold)
        {
            if (goldPunchRoutine != null)
                StopCoroutine(goldPunchRoutine);

            goldText.rectTransform.localScale = Vector3.one;

            goldPunchRoutine = StartCoroutine(PulseScale(goldText.rectTransform, goldPunchScale, goldPunchDuration));
        }
    }


    IEnumerator AnimateGold(int targetGold)
    {
        int startGold = displayedGold;
        int difference = Mathf.Abs(targetGold - startGold);

        float amountT = Mathf.InverseLerp(1f, Mathf.Max(2, goldDifferenceForMaxDuration), difference);

        float duration = Mathf.Lerp(goldCountMinDuration, goldCountMaxDuration, amountT);

        duration = Mathf.Max(0.01f, duration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Ease-out feels like coins arrive quickly and settle naturally.
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            displayedGold = Mathf.RoundToInt(Mathf.Lerp(startGold, targetGold, easedT));

            goldText.text = displayedGold.ToString();

            yield return null;
        }

        displayedGold = targetGold;
        goldText.text = targetGold.ToString();

        goldRoutine = null;
    }


    IEnumerator PulseScale(RectTransform target, float peakScale, float duration)
    {
        if (target == null)
            yield break;

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // 1 -> peak -> 1.
            float pulse = Mathf.Sin(t * Mathf.PI);
            float scale = Mathf.Lerp(1f, peakScale, pulse);

            target.localScale = Vector3.one * scale;

            yield return null;
        }

        target.localScale = Vector3.one;
    }


    void UpdateCooldownDisplay(CooldownDisplay display, float remaining, float duration)
    {
        if (display == null)
            return;

        bool isCoolingDown = remaining > 0f;

        if (display.fill != null)
        {
            display.fill.fillAmount = duration > 0f ? remaining / duration : 0f;

            display.fill.enabled = isCoolingDown;
        }

        if (display.timerText != null)
        {
            display.timerText.gameObject.SetActive(isCoolingDown);

            if (isCoolingDown)
                display.timerText.text = remaining.ToString("0.0");
        }
    }


    public void ShowInteractionPrompt(string message)
    {
        if (interactionPanel != null)
            interactionPanel.SetActive(true);

        if (interactionText != null)
            interactionText.text = message;
    }

    public void HideInteractionPrompt()
    {
        if (interactionPanel != null)
            interactionPanel.SetActive(false);
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null)
            return;

        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }

    float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
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
            playerStats.OnGoldChanged -= UpdateGold;
        }
    }
}
