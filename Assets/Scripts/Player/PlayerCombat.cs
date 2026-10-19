
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerCombat : CharacterCombat
{
    PlayerStats playerStats;

    protected override void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        base.Awake();
    }

    void Update()
    {
        if (animator == null)
            return;

        animator.SetFloat("AttackSpeed", Mathf.Max(0.01f, playerStats.GetAttackSpeed()));
    }

    protected override bool CanAttack()
    {
        int cost = weapons.CurrentWeapon != null ? weapons.CurrentWeapon.attackStaminaCost : 15;

        return playerStats.currentStamina >= 1;
    }

    protected override bool ConsumeAttackCost()
    {
        int cost = weapons.CurrentWeapon != null ? weapons.CurrentWeapon.attackStaminaCost : 15;

        return playerStats.TrySpendStamina(cost);
    }

    protected override int GetAttackDamage()
    {
        return playerStats.strength.Value;
    }

    protected override void OnSuccessfulHit(CharacterStats target, int damageDealt)
    {
        playerStats.AddUltimateCharge(damageDealt);
    }


    protected override Vector3 GetAimDirection(Vector3 origin)
    {
        Camera camera = Camera.main;

        if (camera == null)
            return base.GetAimDirection(origin);

        Ray ray = new Ray(camera.transform.position, camera.transform.forward);

        Vector3 point = ray.GetPoint(100f);
        float closest = 100f;

        foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.root == transform.root)
                continue;

            if (hit.distance < closest)
            {
                closest = hit.distance;
                point = hit.point;
            }
        }

        return (point - origin).normalized;
    }

    
}
