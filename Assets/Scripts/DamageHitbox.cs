using System.Collections.Generic;
using UnityEngine;

/*
    Reusable animation-driven damaging hitbox.

    Put this on a weapon, claw, mouth, fist, body part, etc.

    The collider stays as a trigger, but it only deals damage while
    EnableDamage() has been called by the attack animation.

    A target can only be damaged once per activation.
*/
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DamageHitbox : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] LayerMask targetLayers = ~0;

    Collider hitCollider;
    Rigidbody hitRigidbody;

    CharacterStats ownerStats;

    int configuredDamage;
    DamageType configuredDamageType;

    bool damageActive;

    readonly HashSet<CharacterStats> hitTargets =
        new HashSet<CharacterStats>();


    public bool IsDamageActive => damageActive;

    public event System.Action<CharacterStats> OnHit;


    void Awake()
    {
        hitCollider = GetComponent<Collider>();
        hitRigidbody = GetComponent<Rigidbody>();

        hitCollider.isTrigger = true;

        hitRigidbody.isKinematic = true;
        hitRigidbody.useGravity = false;
        hitRigidbody.interpolation = RigidbodyInterpolation.None;
        hitRigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        damageActive = false;
    }


    public void Initialise(CharacterStats owner)
    {
        ownerStats = owner;
        damageActive = false;
        hitTargets.Clear();
    }


    public void Configure(
        int damage,
        DamageType damageType)
    {
        configuredDamage = Mathf.Max(0, damage);
        configuredDamageType = damageType;
    }


    public void EnableDamage()
    {
        hitTargets.Clear();
        damageActive = true;
    }


    public void DisableDamage()
    {
        damageActive = false;
    }


    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }


    /*
        OnTriggerStay is intentional.

        If the animation enables the hitbox while the weapon is already
        overlapping a target, we still want that active frame to connect.
        The HashSet prevents repeated damage on subsequent physics frames.
    */
    void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }


    void TryDamage(Collider other)
    {
        if (!damageActive)
            return;

        CharacterStats targetStats =
            other.GetComponentInParent<CharacterStats>();

        if (targetStats == null ||
            targetStats == ownerStats ||
            targetStats.IsDead)
        {
            return;
        }

        if (!LayerIsAllowed(other.gameObject.layer) &&
            !LayerIsAllowed(targetStats.gameObject.layer))
        {
            return;
        }

        if (!hitTargets.Add(targetStats))
            return;

        targetStats.TakeDamage(
            configuredDamage,
            configuredDamageType
        );

        OnHit?.Invoke(targetStats);
    }


    bool LayerIsAllowed(int layer)
    {
        return
            (targetLayers.value & (1 << layer)) != 0;
    }


    void OnDisable()
    {
        damageActive = false;
        hitTargets.Clear();
    }
}
