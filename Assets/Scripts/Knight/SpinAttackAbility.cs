using UnityEngine;
//runtime side of Spin Attack. the base basicability handles the cooldown
public class SpinAttackAbility : BasicAbility
{
    SpinAttackData settings;
    public SpinAttackAbility(SpinAttackData data) : base(data)
    {
        settings = data;
    }
    protected override void ExecuteInternal(PlayerStats stats, PlayerController controller, PlayerCombat combat)
    {
        KnightAbilities knight = stats.GetComponent<KnightAbilities>();
        if (knight != null)
        {
            knight.Spin(settings);
        }
        else
        {
            Debug.LogWarning("Spin Attack needs the KnightAbilities component on the player.");
        }
    }
}
