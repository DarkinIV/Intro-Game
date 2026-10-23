using UnityEngine;

[CreateAssetMenu(fileName = "New Ultimate", menuName = "Abilities/Ultimate Ability")]
public class UltimateAbilityData : AbilityData
{
    [Tooltip("Total damage dealt to fully charge")]
    public int damageRequiredForFullCharge = 500;

    public override Ability CreateRuntime() => new UltimateAbility(this);
}