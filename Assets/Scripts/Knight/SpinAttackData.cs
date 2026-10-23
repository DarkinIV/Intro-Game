using UnityEngine;

// Settings for the Knight's Spin Attack
[CreateAssetMenu(fileName = "SpinAttack", menuName = "Abilities/Spin Attack (Knight)")]
public class SpinAttackData : BasicAbilityData
{
    [Header("Spin Attack")]
    public float radius = 3f;
    public float strengthMultiplier = 1.5f;   // damage = strength * this
}
