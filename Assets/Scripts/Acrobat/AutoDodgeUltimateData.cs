using UnityEngine;

// Settings for the Acrobat's ultimate, Untouchable
[CreateAssetMenu(fileName = "Untouchable", menuName = "Abilities/Auto-Dodge Ultimate (Acrobat)")]
public class AutoDodgeUltimateData : UltimateAbilityData
{
    [Header("Untouchable")]
    [Tooltip("How long the auto dodge lasts")]
    public float duration = 8f;
}
