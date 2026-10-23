using UnityEngine;

// Mana for the Fortune Teller. Max mana goes up with Magic.
// It doesn't refill by itself, her abilities do that (Meditate, the Star card etc).
[RequireComponent(typeof(PlayerStats))]
public class Mana : MonoBehaviour
{
    [SerializeField] float baseMana = 50f;
    [SerializeField] float manaPerMagic = 4f;   // max mana = baseMana + magic * this

    PlayerStats stats;
    float lastMax;

    public float Current { get; private set; }
    public float Max => baseMana + stats.magic.Value * manaPerMagic;
    public bool IsFull => Current >= Max;

    public event System.Action<float, float> OnManaChanged;   // current, max
    public event System.Action<float> OnManaSpent;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Start()
    {
        // Done in Start because PlayerStats sets Magic in Awake
        Set(Max);
    }

    void Update()
    {
        // Level ups change Magic, so update the bar if the max changed
        if (!Mathf.Approximately(Max, lastMax))
            Set(Current);
    }

    public bool Has(float amount)
    {
        return Current >= amount;
    }

    // showLoss is false for slow drains (Crystal Sight) so the bar doesn't flash every frame
    public bool TrySpend(float amount, bool showLoss = true)
    {
        if (Current < amount)
            return false;

        Set(Current - amount);

        if (showLoss)
            OnManaSpent?.Invoke(amount);

        return true;
    }

    public void Add(float amount)
    {
        if (!IsFull)
            Set(Current + amount);
    }

    public void AddPercent(float percent)
    {
        Add(Max * percent);
    }

    void Set(float value)
    {
        lastMax = Max;
        Current = Mathf.Clamp(value, 0f, lastMax);
        OnManaChanged?.Invoke(Current, lastMax);
    }
}
