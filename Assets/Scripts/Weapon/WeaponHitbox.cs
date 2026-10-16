using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    CharacterCombat owner;
    bool active;
    readonly HashSet<CharacterStats> alreadyHit = new HashSet<CharacterStats>();

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Setup(CharacterCombat combat)
    {
        owner = combat;
        StopSwing();
    }

    public void StartSwing()
    {
        alreadyHit.Clear();
        active = true;
    }

    public void StopSwing()
    {
        active = false;
        alreadyHit.Clear();
    }

    void OnDisable() => StopSwing();

    void OnTriggerEnter(Collider other) => TryHit(other);
    void OnTriggerStay(Collider other) => TryHit(other);

    void TryHit(Collider other)
    {
        if (!active || owner == null) return;
        CharacterStats victim = other.GetComponentInParent<CharacterStats>();
        if (victim == null || !owner.CanDamage(victim) || !alreadyHit.Add(victim)) return;
        owner.DealDamage(victim);
    }
}
