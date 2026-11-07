using System.Collections.Generic;
using UnityEngine;

// Fired by ranged weapons and ranged enemies. Flies straight, or turns towards the target if it's homing.
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    CharacterCombat owner;
    Transform target;
    Collider targetCollider;
    float speed;
    bool homing;
    float turnSpeed;
    float expireAt;

    // Anyone this already flew through (teammates, or someone who dodged it)
    HashSet<CharacterStats> passedThrough = new HashSet<CharacterStats>();

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Launch(CharacterCombat shooter, Transform lockTarget, float flySpeed, float lifetime, bool homesIn, float homingTurnSpeed)
    {
        owner = shooter;
        target = lockTarget;
        speed = flySpeed;
        homing = homesIn;
        turnSpeed = homingTurnSpeed;
        expireAt = Time.time + lifetime;

        if (target != null)
            targetCollider = target.GetComponentInChildren<Collider>();
    }

    void FixedUpdate()
    {
        if (Time.time >= expireAt)
        {
            Destroy(gameObject);
            return;
        }

        if (homing && target != null)
        {
            // Aim for the middle of their body
            Vector3 aimPoint = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up;
            Quaternion wanted = Quaternion.LookRotation(aimPoint - transform.position);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, turnSpeed * Time.fixedDeltaTime);
        }

        transform.position += transform.forward * speed * Time.fixedDeltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner == null || other.transform.root == owner.transform.root)
            return;

        Shield shield = other.GetComponent<Shield>();
        if (shield != null && owner.IsValidTarget(shield.Owner))
        {
            shield.Block(owner);
            Destroy(gameObject);
            return;
        }

        CharacterStats victim = other.GetComponentInParent<CharacterStats>();
        if (victim != null)
        {
            if (passedThrough.Contains(victim))
                return;

            // Fly straight through anyone we can't hurt, and never come back for them.
            // If it was the target that dodged, stop homing so it doesn't turn round and hit them after the i-frames.
            if (!owner.CanDamage(victim))
            {
                passedThrough.Add(victim);

                if (target != null && target.GetComponentInParent<CharacterStats>() == victim)
                    target = null;

                return;
            }

            owner.DealDamage(victim);
        }

        Destroy(gameObject);
    }
}
