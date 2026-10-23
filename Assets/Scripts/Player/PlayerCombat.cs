using UnityEngine;

// Combat for all the player characters. Each character has its own version of this (AcrobatCombat etc).
[RequireComponent(typeof(PlayerStats))]
public class PlayerCombat : CharacterCombat
{
    protected PlayerStats playerStats;

    protected int AttackStaminaCost => weapons.CurrentWeapon != null ? weapons.CurrentWeapon.attackStaminaCost : 15;

    protected override void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        base.Awake();
    }

    protected virtual void Update()
    {
        animator.SetFloat("AttackSpeed", playerStats.GetAttackSpeed());
    }

    protected override bool CanAttack()
    {
        return playerStats.currentStamina >= 1;
    }

    protected override bool ConsumeAttackCost()
    {
        return playerStats.TrySpendStamina(AttackStaminaCost);
    }

    protected override int GetAttackDamage()
    {
        return playerStats.strength.Value;
    }

    // Damage we deal charges the ultimate
    protected override void OnSuccessfulHit(CharacterStats target, int damageDealt)
    {
        playerStats.AddUltimateCharge(damageDealt);
    }

    // Shoot in the direction the player faces, tilted up or down to match the camera
    protected override Vector3 GetAimDirection(Vector3 origin)
    {
        float cameraPitch = Camera.main.transform.eulerAngles.x;
        return Quaternion.Euler(cameraPitch, transform.eulerAngles.y, 0f) * Vector3.forward;
    }
}
