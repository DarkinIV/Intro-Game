using System.Collections.Generic;
using UnityEngine;

// Goes on a melee weapon's collider. While a swing is going it damages whatever it touches, once per swing.
[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    CharacterCombat owner;
    bool active;
    HashSet<CharacterStats> alreadyHit = new HashSet<CharacterStats>();

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

    void OnDisable()
    {
        StopSwing();
    }

    void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    void TryHit(Collider other)
    {
        if (!active || owner == null)
            return;

        // If the swing touches the Knight's shield first, the shield takes it instead of him
        Shield shield = other.GetComponent<Shield>();
        if (shield != null)
        {
            if (owner.IsValidTarget(shield.Owner) && alreadyHit.Add(shield.Owner))
                shield.Block(owner);
            return;
        }

        CharacterStats victim = other.GetComponentInParent<CharacterStats>();
        if (victim != null && owner.CanDamage(victim) && alreadyHit.Add(victim))
            owner.DealDamage(victim);
    }
}
