using UnityEngine;

[CreateAssetMenu(fileName = "New Basic Ability", menuName = "Abilities/Basic Ability")]
public class BasicAbilityData : AbilityData
{
    public override Ability CreateRuntime() => new BasicAbility(this);
}
//taken from og file abilitydata 
//thank you lord Asim