using UnityEngine;
//the acrobat's ultimate where for a few seconds every enemy attack is dodged automatically
[CreateAssetMenu(fileName = "Untouchable", menuName = "Abilities/Auto-Dodge Ultimate (Acrobat)")]
public class AutoDodgeUltimateData : UltimateAbilityData
{
    [Header("Untouchable")]
    [Tooltip("How long the auto-dodge lasts once activated")] //super nice btw 
    public float duration = 8f;
    public override Ability CreateRuntime()
    {
        return new AutoDodgeUltimate(this);
    }
}
