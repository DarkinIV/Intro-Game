using UnityEngine;

// Settings for the Fortune Teller's Meditate (hold V). She stands still and gets mana back.
// Getting hit stops it.
[CreateAssetMenu(fileName = "Meditate", menuName = "Abilities/Meditate (Fortune Teller)")]
public class MeditateData : ChanneledAbilityData
{
    [Header("Meditate")]
    public float manaPerSecond = 35f;
    public float startDelay = 0.4f;   // how long she has to meditate before the mana starts coming in
}
