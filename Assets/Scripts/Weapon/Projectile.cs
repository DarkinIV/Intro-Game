using UnityEngine;

// Fired by ranged weapons. Flies straight, or turns towards the target if the weapon is set to homing.
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    CharacterCombat owner;
    Transform target;
    Collider targetCollider;
    WeaponItem weapon;
    float expireAt;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Launch(CharacterCombat shooter, Transform lockTarget, WeaponItem firedFrom)
    {
        owner = shooter;
        target = lockTarget;
        weapon = firedFrom;
        expireAt = Time.time + weapon.projectileLifetime;

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

        if (weapon.projectileMode == WeaponItem.ProjectileMode.Homing && target != null)
        {
            // Aim for the middle of their body
            Vector3 aimPoint = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up;
            Quaternion wanted = Quaternion.LookRotation(aimPoint - transform.position);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, weapon.projectileTurnSpeed * Time.fixedDeltaTime);
        }

        transform.position += transform.forward * weapon.projectileSpeed * Time.fixedDeltaTime;
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
            // Fly straight through anyone we can't hurt (teammates, someone dodging)
            if (!owner.CanDamage(victim))
                return;

            owner.DealDamage(victim);
        }

        Destroy(gameObject);
    }
}
