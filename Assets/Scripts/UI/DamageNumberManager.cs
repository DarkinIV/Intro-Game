using UnityEngine;

// On the world space canvas for damage numbers. Spawns them and picks their colour and size.
public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager instance;

    [Header("Prefab")]
    [SerializeField] DamageNumber damageNumberPrefab;

    [Header("Damage Colours")]
    [SerializeField] Color physicalColour;
    [SerializeField] Color magicColour;

    [Header("Damage Size")]
    [SerializeField] int damageForMaximumSize = 100;
    [SerializeField] float minimumScale = 0.85f;
    [SerializeField] float maximumScale = 1.65f;
    [SerializeField] float sizeCurvePower = 0.60f;   // below 1 makes small hits grow faster

    [Header("Spawn Variation")]
    [SerializeField] float horizontalSpawnJitter = 0.15f;
    [SerializeField] float verticalSpawnJitter = 0.08f;

    Camera cam;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        cam = Camera.main;
    }

    public void ShowDamage(int damageAmount, DamageType damageType, Vector3 worldPosition)
    {
        Color colour = damageType == DamageType.Magic ? magicColour : physicalColour;

        // Bigger hits get bigger numbers
        float amount = Mathf.Clamp01(damageAmount / (float)damageForMaximumSize);
        float scale = Mathf.Lerp(minimumScale, maximumScale, Mathf.Pow(amount, sizeCurvePower));

        // Move it around a little so numbers from fast hits don't stack on top of each other
        Vector3 position = worldPosition;
        position += cam.transform.right * Random.Range(-horizontalSpawnJitter, horizontalSpawnJitter);
        position += Vector3.up * Random.Range(-verticalSpawnJitter, verticalSpawnJitter);

        DamageNumber number = Instantiate(damageNumberPrefab, transform);
        number.transform.position = position;
        number.Initialise(damageAmount, colour, scale, cam);
    }
}
