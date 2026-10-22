using UnityEngine;
//runtime side of ult fate sealed
public class FateSealed : UltimateAbility
{
    FateSealedData settings;
    public FateSealed(FateSealedData data) : base(data)
    {
        settings = data;
    }
    protected override void ExecuteInternal(PlayerStats stats, PlayerController controller, PlayerCombat combat)
    {
        base.ExecuteInternal(stats, controller, combat); //empties the charge meter
        FortuneTellerAbilities ft = stats.GetComponent<FortuneTellerAbilities>();
        if (ft != null)
        {
            ft.ActivateFateSealed(settings);
        }
        else
        {
            Debug.LogWarning("Fate Sealed needs the FortuneTellerAbilities component on the player.");
        }
    }
}
