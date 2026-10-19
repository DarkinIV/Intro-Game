using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    CharacterCombat owner;
    Transform target;
    WeaponItem.ProjectileMode mode;
    float speed;
    float turnSpeed;
    float expireAt;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Launch(CharacterCombat shooter, Transform targetTransform, WeaponItem.ProjectileMode flightMode, Vector3 direction, float projectileSpeed, float lifetime, float trackingSpeed)
    {
        owner = shooter;
        target = targetTransform;
        mode = flightMode;
        speed = projectileSpeed;
        turnSpeed = trackingSpeed;
        expireAt = Time.time + lifetime;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    void FixedUpdate()
    {
        if (Time.time >= expireAt)
        {
            Destroy(gameObject);
            return;
        }

        if (mode == WeaponItem.ProjectileMode.Homing && target != null)
        {
            Collider targetCollider = target.GetComponentInChildren<Collider>();

            Vector3 targetPosition = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up;

            Vector3 direction = targetPosition - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion wanted = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, turnSpeed * Time.fixedDeltaTime);
            }
        }

        transform.position += transform.forward * (speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner == null || other.transform.root == owner.transform.root) 
            return;
        
        CharacterStats victim = other.GetComponentInParent<CharacterStats>();
        
        if (victim != null)
        {
            if (!owner.CanDamage(victim)) 
                return;
            
            owner.DealDamage(victim);
        }
        Destroy(gameObject);
    }
}
