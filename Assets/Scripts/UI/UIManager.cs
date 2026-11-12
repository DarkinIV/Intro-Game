using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The main HUD: health, stamina and mana bars, level and insanity, karma, potions,
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

    [Header("Insanity")]
    [SerializeField] TMP_Text levelText;
    [SerializeField] Image insanityFill;
    [SerializeField] Image insanityEffects;
    public List<Sprite> effects = new List<Sprite>();

    [SerializeField] TMP_Text experienceText;

    [Header("Progression Feedback")]
    [SerializeField] float insanityFillDuration = 0.45f;

    [Header("Abilities")]
    [SerializeField] AbilitySlotUI abilitySlotPrefab;
    [SerializeField] Transform abilitySlotParent;       // a slot gets spawned here for each ability

    [Header("Tutorial")]
    [SerializeField] TutorialSlotUI tutorialSlotPrefab;
    public List<TutorialData> tutorials = new List<TutorialData>();
    bool destroyTrigger = false;

    [Header("Interaction")]
    [SerializeField] GameObject interactionPanel;
    [SerializeField] TMP_Text interactionText;

    [Header("Inventory")]
    [SerializeField] GameObject inventoryPanel;

    [Header("Death and Resting")]
    [SerializeField] CanvasGroup deathScreen;
    [SerializeField] CanvasGroup restScreen;
    [SerializeField] float screenFadeTime = 1f;

    PlayerStats playerStats;
    PlayerController playerController;
    Mana playerMana;

    Coroutine shakeRoutine;
    Coroutine insanityRoutine;
    Coroutine karmaRoutine;
    Coroutine karmaPunchRoutine;

    Vector2 healthBarStartPosition;
    int displayedKarma;

    RectTransform healthBarRect;

    void Awake()
    {
        instance = this;
        healthBarRect = healthBar.transform.GetComponent<RectTransform>();
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
        playerStats.OnInsanityChanged += UpdateInsanity;
        playerStats.OnLevelChanged += UpdateLevel;
        playerStats.OnKarmaChanged += UpdateKarma;

        UpdatePlayerHealth(playerStats.currentHealth, playerStats.maxHealth.Value);
        UpdatePlayerStamina(playerStats.currentStamina, playerStats.maxStamina.Value);
        healthBarStartPosition = healthBarRect.anchoredPosition;

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

        levelText.text = playerStats.CurrentLevel.ToString();
        insanityFill.fillAmount = playerStats.CurrentInsanity / 100f;


        displayedKarma = playerStats.Karma;
        karmaText.text = displayedKarma.ToString();

        HideInteractionPrompt();
        HideTutorial();
        inventoryPanel.SetActive(false);

        if (deathScreen != null)
            deathScreen.gameObject.SetActive(false);
        if (restScreen != null)
            restScreen.gameObject.SetActive(false);

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
            playerStats.OnInsanityChanged -= UpdateInsanity;
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
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();
    }

    // Checked in LateUpdate so the player's scripts have already run this frame while still paused,
    // that way the E that closes the tutorial doesn't also interact with something
    void LateUpdate()
    {
        if (tutorialSlotPrefab.gameObject.activeSelf && Input.GetKeyDown(KeyCode.E))
            HideTutorial();
    }

    // One slot for each ability, with the ultimate last
    void CreateAbilitySlots()
    {
        foreach (Ability ability in playerStats.Abilities)
            Instantiate(abilitySlotPrefab, abilitySlotParent).Setup(ability);

        if (playerStats.Ultimate != null)
            Instantiate(abilitySlotPrefab, abilitySlotParent).Setup(playerStats.Ultimate);
    }

    // Tutorial

    // The game is paused while a tutorial is open, until the player presses E
    public void ShowTutorial(int collisionID)
    {
        TutorialData tutorial = tutorials[collisionID];

        // Tutorials for one character are skipped for the others
        if (tutorial.onlyFor != null && tutorial.onlyFor != playerStats.Character)
            return;

        tutorialSlotPrefab.Setup(tutorial);
        tutorialSlotPrefab.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    public void HideTutorial()
    {
        tutorialSlotPrefab.gameObject.SetActive(false);
        Time.timeScale = 1f;
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
        RectTransform bar = healthBarRect;

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

    // Level and insanity

    void UpdateLevel(int level)
    {
        levelText.text = level.ToString();
    }

    // percent goes from 0 to 100
    void UpdateInsanity(float percent)
    {
        if (insanityRoutine != null)
            StopCoroutine(insanityRoutine);

        insanityEffects.sprite = effects[Mathf.RoundToInt(percent / 25)];
        insanityRoutine = StartCoroutine(FillInsanityBar(percent / 100f));
    }

    // Slides the bar from where it is to the new amount
    IEnumerator FillInsanityBar(float target)
    {
        float start = insanityFill.fillAmount;

        for (float elapsed = 0f; elapsed < insanityFillDuration; elapsed += Time.unscaledDeltaTime)
        {
            insanityFill.fillAmount = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / insanityFillDuration));
            yield return null;
        }

        insanityFill.fillAmount = target;
        insanityRoutine = null;
    }

    void SetExperienceText(int current, int required)
    {
        if (experienceText != null)
            experienceText.text = current + " / " + required + " XP";
    }

    // Death and resting

    public void ShowDeathScreen() => StartCoroutine(FadeIn(deathScreen));
    public void ShowRestScreen() => StartCoroutine(FadeIn(restScreen));

    // Fades a screen in over everything. It never fades back out, because the scene reloads right after.
    IEnumerator FadeIn(CanvasGroup screen)
    {
        if (screen == null)
            yield break;

        screen.gameObject.SetActive(true);

        for (float elapsed = 0f; elapsed < screenFadeTime; elapsed += Time.unscaledDeltaTime)
        {
            screen.alpha = elapsed / screenFadeTime;
            yield return null;
        }

        screen.alpha = 1f;
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
