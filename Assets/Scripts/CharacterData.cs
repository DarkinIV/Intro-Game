using System.Collections.Generic;
using UnityEngine;

// Everything that makes a character different: stat ranks, prefab and abilities
[CreateAssetMenu(fileName = "New Character", menuName = "Characters/Character Data")]
public class CharacterData : ScriptableObject
{
    [System.Serializable]
    public class AbilityBinding
    {
        public KeyCode key = KeyCode.F;
        public AbilityData ability;
    }

    [Header("Display")]
    public string characterName = "Unnamed";
    public Sprite portrait;

    [Header("Prefab")]
    public GameObject playerPrefab;

    [Header("Stat Ranks (Level 1 Base)")]
    public StatRank Vitality = StatRank.B;
    public StatRank Energy = StatRank.B;
    public StatRank Strength = StatRank.B;
    public StatRank Magic = StatRank.C;
    public StatRank AttackSpeed = StatRank.B;
    public StatRank Pace = StatRank.B;
    public StatRank Agility = StatRank.B;   // How good they are at jumping and wall moves

    [Header("Abilities")]
    public List<AbilityBinding> abilities = new List<AbilityBinding>();
    public UltimateAbilityData ultimateAbility; // Always on R
}
