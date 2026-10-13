using UnityEngine;

public abstract class AbilityData : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public float cooldown;
    [TextArea] public string description;
    public abstract Ability CreateRuntime();
}

[CreateAssetMenu(fileName = "New Basic Ability", menuName = "Abilities/Basic Ability")]
public class BasicAbilityData : AbilityData
{
    public override Ability CreateRuntime() => new BasicAbility(this);
}

[CreateAssetMenu(fileName = "New Ultimate", menuName = "Abilities/Ultimate Ability")]
public class UltimateAbilityData : AbilityData
{
    [Tooltip("Total damage required to fully charge (buffs modify effective gain)")]
    public int damageRequiredForFullCharge = 500;

    public override Ability CreateRuntime() => new UltimateAbility(this);
}