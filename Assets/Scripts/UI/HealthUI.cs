using UnityEngine;

// The health bar above an enemy. It only shows up for a few seconds after the enemy gets hit.
public class HealthUI : WorldUI
{
    const float stayTime = 3f;

    [Header("Health Bar")]
    public RectTransform healthSlider;
    public RectTransform damageSlider;   // the yellow part that shows how much was just lost
    public GameObject graphic;

    [Header("Damage Feedback")]
    [SerializeField] float damageHoldTime = 0.15f;
    [SerializeField] float damageDrainSpeed = 1.2f;

    CharacterStats stats;
    float lastHealthChangeTime;
    float drainAfter;

    public void Init(Transform target, CharacterStats enemyStats)
    {
        Follow(target);
        stats = enemyStats;

        stats.OnHealthChanged += OnHealthChanged;
        stats.OnDamaged += OnDamaged;

        float percent = stats.currentHealth / (float)stats.maxHealth.Value;
        SetWidth(healthSlider, percent);
        SetWidth(damageSlider, percent);

        graphic.SetActive(false);
    }

    void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= OnHealthChanged;
            stats.OnDamaged -= OnDamaged;
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (graphic.activeSelf && Time.time - lastHealthChangeTime > stayTime)
            graphic.SetActive(false);

        // The yellow part waits for a moment, then shrinks down to the real health
        if (Time.unscaledTime >= drainAfter)
        {
            float width = Mathf.MoveTowards(damageSlider.localScale.x, healthSlider.localScale.x, damageDrainSpeed * Time.unscaledDeltaTime);
            SetWidth(damageSlider, width);
        }
    }

    // This comes before OnHealthChanged, so the health slider still shows the old health here
    void OnDamaged(int damageAmount)
    {
        SetWidth(damageSlider, Mathf.Max(damageSlider.localScale.x, healthSlider.localScale.x));
        drainAfter = Time.unscaledTime + damageHoldTime;
    }

    void OnHealthChanged(int currentHealth, int maxHealth)
    {
        SetWidth(healthSlider, Mathf.Clamp01(currentHealth / (float)maxHealth));

        lastHealthChangeTime = Time.time;
        graphic.SetActive(true);
    }

    void SetWidth(RectTransform slider, float amount)
    {
        slider.localScale = new Vector3(amount, 1f, 1f);
    }
}
