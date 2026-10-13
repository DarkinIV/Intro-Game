using UnityEngine;

public abstract class Ability
{
    public AbilityData Data { get; }
    protected float nextReadyTime;

    protected Ability(AbilityData data) { Data = data; nextReadyTime = -999f; }

    public bool IsReady => Time.time >= nextReadyTime;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    public bool TryUse(PlayerStats stats, PlayerController ctrl, PlayerCombat combat)
    {
        if (!CanUseInternal(stats)) return false;
        ExecuteInternal(stats, ctrl, combat);
        nextReadyTime = Time.time + Data.cooldown;
        return true;
    }

    protected virtual bool CanUseInternal(PlayerStats stats) => IsReady;
    protected abstract void ExecuteInternal(PlayerStats stats, PlayerController ctrl, PlayerCombat combat);
}

public class BasicAbility : Ability
{
    public BasicAbility(BasicAbilityData data) : base(data) { }
    protected override void ExecuteInternal(PlayerStats s, PlayerController c, PlayerCombat cb)
    {
        // Hook point: subclass / implement logic (dash strike, projectile etc.)
    }
}

public class UltimateAbility : Ability
{
    public new UltimateAbilityData Data => (UltimateAbilityData)base.Data;
    public int CurrentCharge { get; private set; }
    public float ChargePercent => Mathf.Clamp01(CurrentCharge / (float)Data.damageRequiredForFullCharge);

    public UltimateAbility(UltimateAbilityData data) : base(data) { }

    public void AddCharge(int rawDamageDealt, float chargeRateMultiplier = 1f)
    {
        CurrentCharge = Mathf.Min(
            Data.damageRequiredForFullCharge,
            CurrentCharge + Mathf.RoundToInt(rawDamageDealt * chargeRateMultiplier)
        );
    }

    protected override bool CanUseInternal(PlayerStats stats)
    {
        return base.CanUseInternal(stats) && CurrentCharge >= Data.damageRequiredForFullCharge;
    }

    protected override void ExecuteInternal(PlayerStats s, PlayerController c, PlayerCombat cb)
    {
        CurrentCharge = 0;
        // Hook point: implement ultimate logic (AOE, burst, etc.)
    }
}