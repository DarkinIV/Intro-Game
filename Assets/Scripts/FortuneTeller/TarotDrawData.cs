using UnityEngine;

// Settings for the Fortune Teller's Tarot Draw (F). She draws a random card with a short effect.
[CreateAssetMenu(fileName = "Tarot Draw", menuName = "Abilities/Tarot Draw (Fortune Teller)")]
public class TarotDrawData : BasicAbilityData
{
    public float manaCost = 20f;

    [Header("How often each card comes up (relative weights)")]
    public int sunWeight = 30;
    public int towerWeight = 30;
    public int deathWeight = 25;
    public int starWeight = 20;
    public int foolWeight = 15;

    [Header("The Star: refill mana")]
    [Range(0f, 1f)] public float starManaPercent = 0.5f;    // of max mana

    [Header("The Sun: heal")]
    [Range(0f, 1f)] public float sunHealPercent = 0.3f;     // of max health

    [Header("The Tower: your next hit explodes")]
    public float towerRadius = 3f;
    public float towerMagicMultiplier = 1.5f;               // explosion damage = magic * this

    [Header("Death: your next hit kills a weak enemy")]
    [Range(0f, 1f)] public float deathThreshold = 0.3f;     // enemies at or below this much health die straight away

    [Header("Tower and Death wait this long for a hit")]
    public float chargedCardDuration = 10f;

    [Header("The Fool: either another good card or a curse")]
    [Range(0f, 1f)] public float foolCurseChance = 0.5f;
    [Range(0f, 1f)] public float foolSlowPercent = 0.3f;    // the curse makes you this much slower
    public float foolCurseDuration = 5f;
}
