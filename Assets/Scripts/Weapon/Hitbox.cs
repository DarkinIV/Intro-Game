// AI use: Claude (Anthropic) added the shield check and the part name for enemy body parts.
// Owner: (add name)

using System.Collections.Generic;
using UnityEngine;

// A collider that damages whatever it touches while it's open, once per swing.
// It can go on a weapon (sword, whip) or on part of an enemy's body (jaw, claws, spikes).
// Animation events open and close it through CharacterCombat.
[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [Tooltip("Only needed on enemies with more than one hitbox, like Bite or LeftClaw. Their attacks pick hitboxes by this name")]
    public string partName;

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

        // Don't hit other weapon hitboxes
        if (other.GetComponent<Hitbox>() != null)
            return;

        // Shields intercept attacks
        Shield shield = other.GetComponent<Shield>();
        if (shield != null)
        {
            if (owner.IsValidTarget(shield.Owner) && alreadyHit.Add(shield.Owner))
                shield.Block(owner);
            return;
        }

        CharacterStats victim = other.GetComponentInParent<CharacterStats>();

        if (victim == null || !owner.IsValidTarget(victim))
            return;

        // Only check each character once per swing including when they dodge the attack
        if (alreadyHit.Add(victim))
            owner.DealDamage(victim);
    }
}
