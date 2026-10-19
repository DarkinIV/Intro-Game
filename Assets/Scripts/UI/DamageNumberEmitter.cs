using UnityEngine;

/*
    Add this to any character that should display floating damage numbers.

    It listens to CharacterStats and forwards actual health lost to the
    dedicated DamageNumberManager/world-space canvas.
*/
[RequireComponent(typeof(CharacterStats))]
public class DamageNumberEmitter : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] Transform damageNumberAnchor;
    [SerializeField] Vector3 fallbackOffset;

    [Header("Display")]
    [SerializeField] bool showDamageNumbers = true;

    CharacterStats stats;


    void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }


    void OnEnable()
    {
        if (stats != null)
            stats.OnDamageTaken += OnDamageTaken;
    }


    void OnDisable()
    {
        if (stats != null)
            stats.OnDamageTaken -= OnDamageTaken;
    }


    void OnDamageTaken(int damageAmount, DamageType damageType)
    {
        if (!showDamageNumbers)
            return;

        if (DamageNumberManager.instance == null)
            return;

        Vector3 spawnPosition = damageNumberAnchor != null ? damageNumberAnchor.position : transform.position + fallbackOffset;

        DamageNumberManager.instance.ShowDamage(damageAmount, damageType, spawnPosition);

    }
}
