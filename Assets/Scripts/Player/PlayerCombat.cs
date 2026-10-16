
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

    protected override bool CanAttack()
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

    
}
