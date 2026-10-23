using System.Collections;
using UnityEngine;

// The Acrobat's combat. Handles the damage side of the whip pull and the plunge slam,
// AcrobatController does the movement part.
public class AcrobatCombat : PlayerCombat
{
    [Header("Plunge Slam")]
    [SerializeField] float slamDamageMultiplier = 2.5f;   // damage = strength * this
    [SerializeField] float slamRadius = 3.5f;
    [SerializeField] float slamStaminaMultiplier = 2f;    // costs this many normal attacks worth of stamina

    // Pulls the locked on enemy, or the closest one in front of us. Returns false if nothing was hit.
    // Heavy enemies don't move, instead zipTarget is set so the controller can fly us to them.
    public bool TryWhipPull(WhipPullData pull, Transform lockTarget, out Transform zipTarget)
    {
        zipTarget = null;

        CharacterStats target = FindPullTarget(pull.range, lockTarget);
        if (target == null)
            return false;

        EnemyController enemy = target.GetComponent<EnemyController>();

        if (enemy != null && enemy.IsHeavy)
        {
            zipTarget = target.transform;
            ShowWhipPull(target.transform, pull.zipMaxTime);
        }
        else
        {
            StartCoroutine(PullTarget(pull, target, enemy));
            ShowWhipPull(target.transform, pull.pullTime);
        }

        return true;
    }

    public bool TryPaySlam()
    {
        return playerStats.TrySpendStamina(AttackStaminaCost * slamStaminaMultiplier);
    }

    public void SlamImpact(Vector3 landingPoint)
    {
        DealAreaDamage(landingPoint, slamRadius, Mathf.RoundToInt(playerStats.strength.Value * slamDamageMultiplier));
    }

    CharacterStats FindPullTarget(float range, Transform lockTarget)
    {
        if (lockTarget != null)
        {
            CharacterStats locked = lockTarget.GetComponentInParent<CharacterStats>();
            bool inRange = Vector3.Distance(transform.position, locked.transform.position) <= range;

            return inRange && IsValidTarget(locked) ? locked : null;
        }

        // Not locked on, so look for the closest enemy in front of us
        CharacterStats closest = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in Physics.SphereCastAll(transform.position + Vector3.up, 0.8f, transform.forward, range, ~0, QueryTriggerInteraction.Ignore))
        {
            CharacterStats target = hit.collider.GetComponentInParent<CharacterStats>();

            if (target != null && hit.distance < closestDistance && IsValidTarget(target))
            {
                closest = target;
                closestDistance = hit.distance;
            }
        }

        return closest;
    }

    // Drags the enemy to just in front of us and stuns them for a bit
    IEnumerator PullTarget(WhipPullData pull, CharacterStats target, EnemyController enemy)
    {
        if (enemy != null)
            enemy.Stun(pull.pullTime + pull.stunTime);

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;

        Vector3 destination = transform.position + toTarget.normalized * pull.stopDistance;
        destination.y = target.transform.position.y;

        float speed = Vector3.Distance(target.transform.position, destination) / pull.pullTime;

        // target becomes null if it dies during the pull
        for (float t = 0f; t < pull.pullTime && target != null; t += Time.deltaTime)
        {
            Vector3 current = target.transform.position;
            Vector3 step = Vector3.MoveTowards(current, destination, speed * Time.deltaTime) - current;

            if (enemy != null)
                enemy.Displace(step);
            else
                target.transform.position += step;

            yield return null;
        }
    }

    // Stretches the whip out to the target (only if the whip is the weapon being held)
    void ShowWhipPull(Transform target, float duration)
    {
        if (weapons.WeaponObject == null)
            return;

        WhipLash lash = weapons.WeaponObject.GetComponentInChildren<WhipLash>();
        if (lash != null)
            lash.ShowPull(target, duration);
    }
}
