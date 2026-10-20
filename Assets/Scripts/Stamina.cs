using UnityEngine;

//stamina meter: sprinting, dodging and special moves use it up, it refills by itself after a short pause
public class Stamina : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenPerSecond = 30f; //how fast it refills
    [SerializeField] private float regenDelay = 1f; //how long after spending before it starts refilling
    [SerializeField, Range(0f, 1f)] private float exhaustedRecoverPercent = 0.25f;
    //when stamina hits 0 you are "exhausted" and cant sprint until it gets back to this much (25%)
    //stops the sprint flickering on and off when stamina is nearly empty

    public float Current { get; private set; }
    public float Max => maxStamina;
    public bool IsExhausted { get; private set; } //ran out and hasnt recovered enough yet

    private float lastSpendTime = -999f;

    private void Awake()
    {
        Current = maxStamina;
    }

    //the class script calls this to set the numbers from the class sheet
    public void Setup(float max, float regen, float delay)
    {
        maxStamina = max;
        regenPerSecond = regen;
        regenDelay = delay;
        Current = maxStamina; //start full
        IsExhausted = false;
    }

    void Update()
    {
        //only refill if we havent spent any for a little while
        if (Time.time - lastSpendTime >= regenDelay)
        {
            Current = Mathf.Min(Current + regenPerSecond * Time.deltaTime, maxStamina); //Min stops it going over max
        }

        if (IsExhausted && Current >= maxStamina * exhaustedRecoverPercent)
        {
            IsExhausted = false; //recovered enough
        }
    }

    //for one off actions (dodge, whip pull...). like elden ring: works as long as u have ANY stamina left,
    //even if the cost is more than what u have (it just goes down to 0). gives back false if u have none
    public bool TrySpend(float amount)
    {
        if (Current <= 0f) return false;
        Spend(amount);
        return true;
    }

    //for things that drain over time (sprint, wall run), call every frame with cost * Time.deltaTime
    public void Drain(float amount)
    {
        Spend(amount);
    }

    public void Refill()
    {
        Current = maxStamina;
        IsExhausted = false;
    }

    private void Spend(float amount)
    {
        Current = Mathf.Max(Current - amount, 0f);
        lastSpendTime = Time.time; //restart the refill delay
        if (Current <= 0f) IsExhausted = true;
    }
}
