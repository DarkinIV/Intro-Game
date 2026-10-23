// AI use: Rewritten by Claude to be simpler
using TMPro;
using UnityEngine;

// One floating damage number. It pops in, jumps up in an arc with a little bounce,
// drifts to the side and fades out.
[RequireComponent(typeof(RectTransform))]
public class DamageNumber : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] TMP_Text damageText;
    [SerializeField] float outlineWidth = 0.16f;
    [SerializeField] Color outlineColour = new Color(0f, 0f, 0f, 0.85f);

    [Header("Lifetime")]
    [SerializeField] float lifetime = 0.85f;
    [SerializeField] float fadeStart = 0.55f;   // starts fading at this point of its life (0 to 1)

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
    Vector3 sideways;
    float size;
    Color baseColour;
    float age;

    public void Initialise(int damageAmount, Color colour, float scale, Camera targetCamera)
    {
        cam = targetCamera;
        size = scale;
        baseColour = colour;

        damageText.text = damageAmount.ToString();
        damageText.color = colour;
        damageText.outlineWidth = outlineWidth;
        damageText.outlineColor = outlineColour;

        startPosition = transform.position;
        sideways = cam.transform.right * Random.Range(-horizontalDrift, horizontalDrift);
        transform.localScale = Vector3.one * size * startingScale;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        float t = age / lifetime;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // One big arc plus a smaller bounce that dies down
        float height = Mathf.Sin(t * Mathf.PI) * bounceHeight
                     + Mathf.Sin(t * Mathf.PI * 3f) * secondaryBounceHeight * (1f - t)
                     + upwardDrift * t;

        transform.position = startPosition + sideways * t + Vector3.up * height;
        transform.localScale = Vector3.one * size * PopScale();
        transform.rotation = cam.transform.rotation;

        Color colour = baseColour;
        colour.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeStart, 1f, t));
        damageText.color = colour;
    }

    // Grows past full size quickly, then settles back down
    float PopScale()
    {
        if (age < popDuration)
            return Mathf.Lerp(startingScale, overshootScale, Mathf.SmoothStep(0f, 1f, age / popDuration));

        if (age < popDuration + settleDuration)
            return Mathf.Lerp(overshootScale, 1f, Mathf.SmoothStep(0f, 1f, (age - popDuration) / settleDuration));

        return 1f;
    }
}
