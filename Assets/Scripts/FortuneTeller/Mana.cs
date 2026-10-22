using UnityEngine;

//the fortune teller's magic resource. her orbs cost mana instead of stamina( decision about this underway not too sure what has been agreed on should it be stamina or this but i worked hard on this so pls consider it )
//there is no natural regen on purpose bcz we want mana to come back from actually playing with this char (its a unique one)
//foreseen dodge, if u are dashing while a foresight warning ring threatens you, then u regen a bit 
//same thing when u pull the star tarot card
//a fate sealed ult enemy dying   (someone plspls rememebr to test this add, testing can be done by adding more enemies)
//meditate hold V while standing still(someone make this slower its broken)
//the pool grows with the magic stat, so levelling Magic means both harder orbs and more of them
[RequireComponent(typeof(PlayerStats))]
public class Mana : MonoBehaviour
{
    [SerializeField] float baseMana = 50f;
    [SerializeField] float manaPerMagic = 4f; //max mana = base + Magic x this (Magic S at level 1 = 18 to 122)
    public float Current { get; private set; }
    public float Max
    {
        get { return baseMana + stats.magic.Value * manaPerMagic; }
    }

    //fires every time the amount changes (current, max) maybe hud can check this later 
    public event System.Action<float, float> OnManaChanged;
    PlayerStats stats;
    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }
    void Start()
    {
        Current = Max; //start full (start and not awake, so the class ranks are already applied to magic)
        Announce();
    }
    void Update()
    {
        //max can change (level up, buffs) but never be above it
        if (Current > Max) Set(Max);
    }
    public bool Has(float amount)
    {
        return Current >= amount;
    }
//maybe paying at somepoint 
    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return true;
        if (Current < amount) return false;
        Set(Current - amount);
        return true;
    }
//refill but never above 
    public void Add(float amount, string reason)
    {
        if (amount <= 0f || Current >= Max) return;
        Set(Current + amount);
        if (reason != null) Debug.Log("<color=#a070ff>+" + Mathf.RoundToInt(amount) + " mana</color> (" + reason + ")");
    }
    public void Add(float amount)
    {
        Add(amount, null);
    }
    public void AddPercent(float percent, string reason)
    {
        Add(Max * percent, reason);
    }
    void Set(float value)
    {
        Current = Mathf.Clamp(value, 0f, Max);
        Announce();
    }
    void Announce()
    {
        if (OnManaChanged != null) OnManaChanged(Current, Max);
    }
}
