using UnityEngine;

//runtime side of the untouchable ultimate
//this only says what happens when it fires
public class AutoDodgeUltimate : UltimateAbility
{
    AutoDodgeUltimateData settings;
    public AutoDodgeUltimate(AutoDodgeUltimateData data) : base(data)
    {
        settings = data;
    }
    protected override void ExecuteInternal(PlayerStats stats, PlayerController controller, PlayerCombat combat)
    {
        base.ExecuteInternal(stats, controller, combat); //empties the charge meter
        AcrobatUltimate acrobatUlt = stats.GetComponent<AcrobatUltimate>();
        if (acrobatUlt != null)
        {
            acrobatUlt.ActivateUntouchable(settings.duration);
        }
        else
        {
            Debug.LogWarning("Untouchable needs the AcrobatUltimate component on player.");
        }
    }
}
