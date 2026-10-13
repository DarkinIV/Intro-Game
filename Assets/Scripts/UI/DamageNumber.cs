using System.Collections;
using TMPro;
using UnityEngine;

/*
    One animated world-space damage number.

    The number pops, jumps in a small arc, has a light secondary bounce,
    drifts sideways and fades away.
*/
[RequireComponent(typeof(RectTransform))]
public class DamageNumber : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] TMP_Text damageText;
    [SerializeField] float outlineWidth = 0.16f;
    [SerializeField] Color outlineColour =
        new Color(0f, 0f, 0f, 0.85f);

    [Header("Lifetime")]
    [SerializeField] float lifetime = 0.85f;
    [SerializeField] float fadeStart = 0.55f;

    [Header("Bounce Motion")]
    [SerializeField] float bounceHeight = 0.65f;
    [SerializeField] float secondaryBounceHeight = 0.10f;
    [SerializeField] float upwardDrift = 0.15f;
    [SerializeField] float horizontalDrift = 0.30f;

    [Header("Impact Pop")]
    [SerializeField] float startingScale = 0.55f;
    [SerializeField] float overshootScale = 1.28f;
    [SerializeField] float popDuration = 0.08f;
    [SerializeField] float settleDuration = 0.10f;

    Camera cam;
    Vector3 startPosition;
    Vector3 sidewaysDirection;
    float damageScale = 1f;
    Color baseColour;


    public void Initialise(
        int damageAmount,
        Color colour,
        float scale,
        Camera targetCamera)
    {
        cam = targetCamera;
        damageScale = Mathf.Max(0.01f, scale);
        baseColour = colour;

        if (damageText == null)
            damageText = GetComponent<TMP_Text>();

        if (damageText != null)
        {
            damageText.text = damageAmount.ToString();
            damageText.color = colour;
            damageText.outlineWidth = outlineWidth;
            damageText.outlineColor = outlineColour;
            damageText.raycastTarget = false;
        }

        startPosition = transform.position;

        if (cam != null)
        {
            sidewaysDirection = cam.transform.right * Random.Range(-horizontalDrift, horizontalDrift);

            transform.rotation = cam.transform.rotation;
        }
        else
        {
            sidewaysDirection = Vector3.right * Random.Range(-horizontalDrift, horizontalDrift);
        }

        transform.localScale = Vector3.one * damageScale * startingScale;

        StartCoroutine(Animate());
    }


    IEnumerator Animate()
    {
        float elapsed = 0f;
        float safeLifetime = Mathf.Max(0.01f, lifetime);

        while (elapsed < safeLifetime)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / safeLifetime);

            UpdatePosition(t);
            UpdateScale(elapsed);
            UpdateAlpha(t);

            if (cam != null)
                transform.rotation = cam.transform.rotation;

            yield return null;
        }

        Destroy(gameObject);
    }


    void UpdatePosition(float t)
    {
        // Main jump: one clean arc.
        float mainArc = Mathf.Sin(t * Mathf.PI) * bounceHeight;

        // Small secondary movement makes the number feel less linear
        // and gives it the "bouncy" combat-text feel.
        float secondaryBounce = Mathf.Sin(t * Mathf.PI * 3f) * secondaryBounceHeight * (1f - t);

        float verticalOffset = mainArc + secondaryBounce + upwardDrift * t;

        transform.position = startPosition + sidewaysDirection * t + Vector3.up * verticalOffset;
    }


    void UpdateScale(float elapsed)
    {
        float scaleMultiplier;

        if (elapsed < popDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, popDuration));

            float easedT = EaseOutBack(t);

            scaleMultiplier = Mathf.Lerp(startingScale, overshootScale, easedT);
        }
        else if (elapsed < popDuration + settleDuration)
        {
            float t = Mathf.Clamp01((elapsed - popDuration) / Mathf.Max(0.01f, settleDuration));

            scaleMultiplier = Mathf.Lerp(overshootScale, 1f, SmoothStep01(t));
        }
        else
        {
            scaleMultiplier = 1f;
        }

        transform.localScale = Vector3.one * damageScale * scaleMultiplier;
    }


    void UpdateAlpha(float t)
    {
        if (damageText == null)
            return;

        float alpha = 1f;

        if (t > fadeStart)
        {
            float fadeT = Mathf.InverseLerp(fadeStart, 1f, t);

            alpha = 1f - SmoothStep01(fadeT);
        }

        Color colour = baseColour;
        colour.a *= alpha;

        damageText.color = colour;
    }


    float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }


    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        float x = t - 1f;

        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
