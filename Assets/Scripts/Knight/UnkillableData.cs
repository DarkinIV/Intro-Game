using UnityEngine;

// Settings for the Knight's ultimate. While it's on he can't go below 1 health.
[CreateAssetMenu(fileName = "Unkillable", menuName = "Abilities/Unkillable (Knight)")]
public class UnkillableData : UltimateAbilityData
{
    [Header("Unkillable")]
    public float duration = 8f;
}
