using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Characters/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Display")]
    public string characterName = "Unnamed";
    public Sprite portrait;
    public RuntimeAnimatorController animatorController;

    [Header("Prefab")]
    public GameObject playerPrefab;

    [Header("Stat Ranks (Level 1 Base)")]
    public StatRank Vitality = StatRank.B;
    public StatRank Energy = StatRank.B;
    public StatRank Strength = StatRank.B;
    public StatRank Magic = StatRank.C;
    public StatRank AttackSpeed = StatRank.B;
    public StatRank Pace = StatRank.B;

    [Header("Abilities")]
    public BasicAbilityData basicAbility;
    public UltimateAbilityData ultimateAbility;
}