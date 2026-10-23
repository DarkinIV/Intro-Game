using UnityEngine;

// Put this on any character that should show damage numbers when they get hit
[RequireComponent(typeof(CharacterStats))]
public class DamageNumberEmitter : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] Transform damageNumberAnchor;   // optional, otherwise uses the offset below
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
        stats.OnDamageTaken += OnDamageTaken;
    }

    void OnDisable()
    {
        stats.OnDamageTaken -= OnDamageTaken;
    }

    void OnDamageTaken(int damageAmount, DamageType damageType)
    {
        if (!showDamageNumbers)
            return;

        Vector3 position = damageNumberAnchor != null ? damageNumberAnchor.position : transform.position + fallbackOffset;
        DamageNumberManager.instance.ShowDamage(damageAmount, damageType, position);
    }
}
