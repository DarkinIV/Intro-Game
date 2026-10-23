using UnityEngine;

// The in-game version of an ability. AbilityData has the settings, this keeps track of the cooldown.
public abstract class Ability
{
    public AbilityData Data { get; }
    public KeyCode Key { get; set; }

    protected float readyTime;

    protected Ability(AbilityData data)
    {
        Data = data;
    }

    public bool IsReady => Time.time >= readyTime;
    public float CooldownRemaining => Mathf.Max(0f, readyTime - Time.time);

    public void TryUse(PlayerController player)
    {
        if (!CanUse())
            return;

        // Only start the cooldown if the ability actually did something
        if (Activate(player))
            readyTime = Time.time + Data.cooldown;
    }

    protected virtual bool CanUse() => IsReady;
    protected abstract bool Activate(PlayerController player);
}

// A normal press ability. The character's controller decides what it does.
public class BasicAbility : Ability
{
    public BasicAbility(BasicAbilityData data) : base(data) { }

    protected override bool Activate(PlayerController player) => player.UseAbility(this);
}

// A held ability (Crystal Sight, Meditate). The meter goes down while the key is held
// and when it runs out the ability goes on cooldown.
public class ChanneledAbility : Ability
{
    public float Meter { get; private set; } = 1f;

    ChanneledAbilityData channelData;

    public ChanneledAbility(ChanneledAbilityData data) : base(data)
    {
        channelData = data;
    }

    // Called every frame. Returns true while the ability is running.
    public bool Tick(bool held)
    {
        if (!IsReady)
            return false;

        // Cooldown just finished so fill the meter back up
        if (Meter <= 0f)
            Meter = 1f;

        if (!held)
        {
            if (channelData.refillTime > 0f)
                Meter = Mathf.Min(1f, Meter + Time.deltaTime / channelData.refillTime);

            return false;
        }

        Meter -= Time.deltaTime / channelData.channelTime;

        if (Meter <= 0f)
        {
            Meter = 0f;
            readyTime = Time.time + Data.cooldown;
        }

        return true;
    }

    // Held abilities are never pressed
    protected override bool Activate(PlayerController player) => false;
}

// The ultimate (R). It has to be charged up by dealing damage before it can be used.
public class UltimateAbility : Ability
{
    public int CurrentCharge { get; private set; }

    int chargeNeeded;

    public UltimateAbility(UltimateAbilityData data) : base(data)
    {
        chargeNeeded = data.damageRequiredForFullCharge;
    }

    public float ChargePercent => Mathf.Clamp01(CurrentCharge / (float)chargeNeeded);

    public void AddCharge(int damageDealt)
    {
        CurrentCharge = Mathf.Min(chargeNeeded, CurrentCharge + damageDealt);
    }

    protected override bool CanUse() => IsReady && CurrentCharge >= chargeNeeded;

    protected override bool Activate(PlayerController player)
    {
        if (!player.UseUltimate(this))
            return false;

        CurrentCharge = 0;
        return true;
    }
}
