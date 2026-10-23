using UnityEngine;

// Settings for the Fortune Teller's Crystal Sight (hold C).
// While held it shows hidden things and gets rid of illusions nearby (anything with RevealedBySight).
[CreateAssetMenu(fileName = "Crystal Sight", menuName = "Abilities/Crystal Sight (Fortune Teller)")]
public class CrystalSightData : ChanneledAbilityData
{
    [Header("Crystal Sight")]
    public float range = 20f;
    public float manaPerSecond = 8f;
}
