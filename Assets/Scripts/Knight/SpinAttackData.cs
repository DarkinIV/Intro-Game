using UnityEngine;

//basic ability for knight which is a whirlwind slash that hits everything around him
//Alrwady pre-planned but now filled in 
[CreateAssetMenu(fileName = "SpinAttack", menuName = "Abilities/Spin Attack (Knight)")]
public class SpinAttackData : BasicAbilityData
{
    [Header("Spin Attack")]
    public float radius = 3f;
    public float strengthMultiplier = 1.5f; //damage = Strength x this
    public float spinTime = 0.35f;  //how long the full turn takes(maaybe we should make this longer to reflect on realoty idk)
    public override Ability CreateRuntime()
    {
        return new SpinAttackAbility(this);
    }
}
