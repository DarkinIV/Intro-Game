using UnityEngine;

//the knight's ultimate: for a few seconds he cant drop below 1 HP
//DarkinIV already planned it ("Unkillable - Defy death" on the Swordman's class sheet), this makes it do something
//NOTE: a ScriptableObject class must live in a file with the same name, which is why this has its own file, pls do not tweak that
[CreateAssetMenu(fileName = "Unkillable", menuName = "Abilities/Unkillable (Knight)")]
public class UnkillableData : UltimateAbilityData
{
    [Header("Unkillable")]
    public float duration = 8f;

    public override Ability CreateRuntime()
    {
        return new UnkillableUltimate(this);
    }
}
