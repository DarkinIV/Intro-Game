using UnityEngine;

/*
    Lives on the dedicated world-space Damage Numbers canvas.

    It creates damage-number prefabs and decides their colour and size.
*/
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
    [SerializeField] float sizeCurvePower = 0.60f;

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


    public void ShowDamage(
        int damageAmount,
        DamageType damageType,
        Vector3 worldPosition)
    {
        if (damageAmount <= 0 || damageNumberPrefab == null)
            return;

        if (cam == null)
            cam = Camera.main;

        Color colour = damageType == DamageType.Magic ? magicColour : physicalColour;

        float normalisedDamage = Mathf.InverseLerp(1f, Mathf.Max(2, damageForMaximumSize), damageAmount);

        float curvedDamage = Mathf.Pow(Mathf.Clamp01(normalisedDamage), Mathf.Max(0.01f, sizeCurvePower));

        float scale = Mathf.Lerp(minimumScale, maximumScale, curvedDamage);

        Vector3 spawnPosition = worldPosition;

        if (cam != null)
        {
            spawnPosition += cam.transform.right * Random.Range(-horizontalSpawnJitter, horizontalSpawnJitter);
        }

        spawnPosition += Vector3.up * Random.Range(-verticalSpawnJitter, verticalSpawnJitter);

        DamageNumber newNumber = Instantiate(damageNumberPrefab, transform);

        newNumber.transform.position = spawnPosition;

        newNumber.Initialise(damageAmount, colour, scale, cam);
    }
}
