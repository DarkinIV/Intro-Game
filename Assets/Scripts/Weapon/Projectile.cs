using System.Collections.Generic;
using UnityEngine;

// Fired by ranged weapons and ranged enemies. Flies straight, or turns towards the target if it's homing.
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    CharacterCombat owner;
    Transform target;
    Collider targetCollider;
    CharacterStats targetStats;
    float speed;
    bool homing;
    float turnSpeed;
    float expireAt;
    bool finished;

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
        {
            targetCollider = target.GetComponent<Collider>();
            targetStats = target.GetComponentInParent<CharacterStats>();
        }
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
            Vector3 toTarget = AimPoint() - transform.position;

            // It flew past the target, so let it carry on instead of turning round and circling them
            if (Vector3.Dot(transform.forward, toTarget) < 0f)
                target = null;
            else
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toTarget), turnSpeed * Time.fixedDeltaTime);
        }

        transform.position += transform.forward * speed * Time.fixedDeltaTime;
    }

    // The middle of the target's body. The player's transform is at their feet, so aiming at that made it dive into the floor
    Vector3 AimPoint()
    {
        return targetCollider != null ? targetCollider.bounds.center : target.position;
    }

    void OnTriggerEnter(Collider other)
    {
        // Destroy waits until the end of the frame, so ignore anything else it touches in the meantime
        if (finished || owner == null || other.transform.root == owner.transform.root)
            return;

        // Shield colliders are triggers, so they're checked before triggers get ignored
        Shield shield = other.GetComponent<Shield>();
        if (shield != null)
        {
            if (owner.IsValidTarget(shield.Owner))
            {
                shield.Block(owner);
                Stop();
            }
            return;
        }

        // Other triggers aren't solid (hitboxes, tutorial zones, other projectiles...)
        if (other.isTrigger)
            return;

        CharacterStats victim = other.GetComponentInParent<CharacterStats>();

        // Walls, the floor and anything else solid
        if (victim == null)
        {
            Stop();
            return;
        }

        if (passedThrough.Contains(victim))
            return;

        // Fly straight through anyone we can't hurt, and never come back for them.
        // If it was the target that dodged, stop homing so it doesn't turn round and hit them after the i-frames.
        if (!owner.CanDamage(victim))
        {
            passedThrough.Add(victim);

            if (victim == targetStats)
                target = null;

            return;
        }

        owner.DealDamage(victim);
        Stop();
    }

    void Stop()
    {
        finished = true;
        Destroy(gameObject);
    }
}
