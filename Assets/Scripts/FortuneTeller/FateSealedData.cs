using UnityEngine;

//the fortune teller's ult, it marks every enemy in view and for a few seconds every hit on them crits
[CreateAssetMenu(fileName = "Fate Sealed", menuName = "Abilities/Fate Sealed (Fortune Teller)")]
public class FateSealedData : UltimateAbilityData
{
    [Header("Fate Sealed")]
    public float duration = 6f;
    public float range = 15f; //enemies this close AND on screen get marked
    public float critMultiplier = 2f; //hits on marked enemies deal this many times the damage
    public override Ability CreateRuntime()
    {
        return new FateSealed(this);
    }
}
