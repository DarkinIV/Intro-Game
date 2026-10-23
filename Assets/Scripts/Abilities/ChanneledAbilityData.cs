// AI use: Whole script written by Claude
using UnityEngine;

// Settings shared by held abilities (Crystal Sight, Meditate)
public abstract class ChanneledAbilityData : AbilityData
{
    [Header("Channel")]
    [Tooltip("How many seconds it can be held from a full meter")]
    public float channelTime = 4f;
    [Tooltip("Seconds to refill if you let go early. 0 means it only refills after the cooldown")]
    public float refillTime = 8f;

    public override Ability CreateRuntime() => new ChanneledAbility(this);
}
