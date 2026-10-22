using UnityEngine;

//the fortune teller's basic ability which has her draw a tarot card for a short effect
//a normal basic ability for the base ability system (cooldown, TryUse), it only adds the card settings
[CreateAssetMenu(fileName = "Tarot Draw", menuName = "Abilities/Tarot Draw (Fortune Teller)")]
public class TarotDrawData : BasicAbilityData
{
    [Header("How often each card comes up (relative weights)")]
    public int sunWeight = 30;
    public int towerWeight = 30;
    public int deathWeight = 25;
    public int starWeight = 20;
    public int foolWeight = 15;
    [Header("The Star: refill mana")]
    [Range(0f, 1f)] public float starManaPercent = 0.5f; //of max mana
    [Header("The Sun: heal")]
    [Range(0f, 1f)] public float sunHealPercent = 0.3f;     //of max health
    [Header("The Tower: your next hit explodes")]
    public float towerRadius = 3f;
    public float towerMagicMultiplier = 1.5f;//explosion damage = Magic x this
    [Header("Death: your next hit executes a weak enemy")]
    [Range(0f, 1f)] public float deathThreshold = 0.3f;  //enemies at or below this much health die instantly(lets tweak this for fun)
    [Header("Tower and Death wait this long for a hit")]
    public float chargedCardDuration = 10f;
    [Header("The Fool: random. 50% another good card, 50% a curse")]
    [Range(0f, 1f)] public float foolCurseChance = 0.5f;
    [Range(0f, 1f)] public float foolSlowPercent = 0.3f; //curse which is move this much slower
    //i love human creativity, we are amazing 
    public float foolCurseDuration = 5f;
    public override Ability CreateRuntime()
    {
        return new TarotDraw(this);
    }
}
