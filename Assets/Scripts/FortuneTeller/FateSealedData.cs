using UnityEngine;

// Settings for the Fortune Teller's ultimate. It marks every enemy she can see,
// and for a few seconds all her hits on them are critical hits.
[CreateAssetMenu(fileName = "Fate Sealed", menuName = "Abilities/Fate Sealed (Fortune Teller)")]
public class FateSealedData : UltimateAbilityData
{
    [Header("Fate Sealed")]
    public float duration = 6f;
    public float range = 15f;           // enemies have to be this close and on screen
    public float critMultiplier = 2f;   // hits on marked enemies do this many times the damage
}
