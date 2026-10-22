using UnityEngine;
//runtime side of unkillable. the base UltimateAbility handles the charge meter and the cooldown
public class UnkillableUltimate : UltimateAbility
{
    UnkillableData settings;
    public UnkillableUltimate(UnkillableData data) : base(data)
    {
        settings = data;
    }
    protected override void ExecuteInternal(PlayerStats stats, PlayerController controller, PlayerCombat combat)
    {
        base.ExecuteInternal(stats, controller, combat); //empties the charge meter

        KnightAbilities knight = stats.GetComponent<KnightAbilities>();
        if (knight != null)
        {
            knight.ActivateUnkillable(settings.duration);
        }
        else
        {
            Debug.LogWarning("Unkillable needs the KnightAbilities component on the player.");
        }
    }
}
