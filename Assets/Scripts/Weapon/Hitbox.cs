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

        // If the swing touches the Knight's shield first, the shield takes it instead of him
        Shield shield = other.GetComponent<Shield>();
        if (shield != null)
        {
            if (owner.IsValidTarget(shield.Owner) && alreadyHit.Add(shield.Owner))
                shield.Block(owner);
            return;
        }

        // Everyone only gets checked once per swing. If they were dodging at that moment,
        // the swing has missed them and can't hit them later when their i-frames run out.
        CharacterStats victim = other.GetComponentInParent<CharacterStats>();
        if (victim != null && alreadyHit.Add(victim))
            owner.DealDamage(victim);
    }
}
