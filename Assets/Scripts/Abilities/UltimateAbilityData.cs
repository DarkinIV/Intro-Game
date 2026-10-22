using UnityEngine;

[CreateAssetMenu(fileName = "New Ultimate", menuName = "Abilities/Ultimate Ability")]
public class UltimateAbilityData : AbilityData
{
    [Tooltip("Total damage required to fully charge (buffs modify effective gain)")]
    public int damageRequiredForFullCharge = 500;

    public override Ability CreateRuntime() => new UltimateAbility(this);
}
//also taken out of abailitydata
//thank u again Asim for making everyone's work easier - kc 