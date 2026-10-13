using System.Collections;
using UnityEngine;

/*
    Controls a world-space health bar.

    The bar follows its target and reacts to health changes
    through CharacterStats events.
*/
public class HealthUI : MonoBehaviour
{
    const float stayTime = 3f;

    [Header("Health Bar")]
    public RectTransform healthSlider;
    public RectTransform damageSlider;
    public GameObject graphic;

    [Header("Damage Feedback")]
    [SerializeField] float damageHoldTime = 0.15f;
    [SerializeField] float damageDrainSpeed = 1.2f;

    Transform cam;
    Transform target;
    CharacterStats stats;

    float lastHealthChangeTime;
    Coroutine damageDrainRoutine;


    public void Init(
        Transform target,
        CharacterStats stats)
    {
        this.target = target;
        this.stats = stats;

        cam = Camera.main.transform;

        stats.OnHealthChanged += OnHealthChanged;
        stats.OnDamaged += OnDamaged;

        float startingHealthPercent = GetHealthPercent(stats.currentHealth, stats.maxHealth.Value);

        SetSliderScale(healthSlider, startingHealthPercent);
        SetSliderScale(damageSlider, startingHealthPercent);

        graphic.SetActive(false);
    }


    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.position;

        transform.LookAt(
            new Vector3(
                cam.position.x,
                transform.position.y,
                cam.position.z
            ),
            Vector3.down
        );

        if (graphic.activeSelf &&
            Time.time - lastHealthChangeTime > stayTime)
        {
            graphic.SetActive(false);
        }
    }


    void OnDamaged(int damageAmount)
    {
        if (damageAmount <= 0)
            return;

        if (damageSlider != null && healthSlider != null)
        {
            float previousHealth = healthSlider.localScale.x;
            float currentDamageFill = damageSlider.localScale.x;

            SetSliderScale(
                damageSlider,
                Mathf.Max(currentDamageFill, previousHealth)
            );

            if (damageDrainRoutine != null)
                StopCoroutine(damageDrainRoutine);

            damageDrainRoutine = StartCoroutine(DrainDamageSlider());
        }
    }


    void OnHealthChanged(
        int currentHealth,
        int maxHealth)
    {
        float healthPercent = GetHealthPercent(currentHealth, maxHealth);

        SetSliderScale(healthSlider, healthPercent);

        // Healing should bring the yellow layer up immediately.
        if (damageSlider != null &&
            damageSlider.localScale.x < healthPercent)
        {
            SetSliderScale(damageSlider, healthPercent);
        }

        lastHealthChangeTime = Time.time;
        graphic.SetActive(true);
    }


    IEnumerator DrainDamageSlider()
    {
        yield return new WaitForSecondsRealtime(damageHoldTime);

        while (damageSlider != null &&
               healthSlider != null &&
               damageSlider.localScale.x > healthSlider.localScale.x + 0.001f)
        {
            float newAmount = Mathf.MoveTowards(
                damageSlider.localScale.x,
                healthSlider.localScale.x,
                damageDrainSpeed * Time.unscaledDeltaTime
            );

            SetSliderScale(damageSlider, newAmount);
            yield return null;
        }

        if (damageSlider != null && healthSlider != null)
            SetSliderScale(damageSlider, healthSlider.localScale.x);

        damageDrainRoutine = null;
    }


    float GetHealthPercent(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0)
            return 0f;

        return Mathf.Clamp01(
            currentHealth / (float)maxHealth
        );
    }


    void SetSliderScale(RectTransform slider, float amount)
    {
        if (slider == null)
            return;

        slider.localScale = new Vector3(
            Mathf.Clamp01(amount),
            1f,
            1f
        );
    }


    void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= OnHealthChanged;
            stats.OnDamaged -= OnDamaged;
        }
    }
}
