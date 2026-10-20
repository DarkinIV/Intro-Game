
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

        // Horizontal direction the player is facing
        Vector3 playerForward = transform.forward;
        playerForward.y = 0f;
        playerForward.Normalize();

        // Camera's vertical aiming angle
        float cameraPitch = camera.transform.eulerAngles.x;

        // Apply camera pitch to the player's facing direction
        Quaternion aimRotation = Quaternion.Euler(cameraPitch, transform.eulerAngles.y, 0f);

        return (aimRotation * Vector3.forward).normalized;
    }


}
