using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stat
{
    public int baseValue;

    readonly List<int> flatModifiers = new List<int>();
    readonly List<float> percentModifiers = new List<float>();

    public int GetValue()
    {
        int flat = baseValue;
        flatModifiers.ForEach(x => flat += x);

        float pct = 1f;
        percentModifiers.ForEach(x => pct += x);

        return Mathf.RoundToInt(flat * pct);
    }

    public void AddFlat(int amount) { if (amount != 0) flatModifiers.Add(amount); }
    public void RemoveFlat(int amount) { if (amount != 0) flatModifiers.Remove(amount); }

    // multiplier is additive: 0.2f = +20%, -0.1f = -10%
    public void AddPercent(float multiplier) { if (multiplier != 0f) percentModifiers.Add(multiplier); }
    public void RemovePercent(float multiplier) { if (multiplier != 0f) percentModifiers.Remove(multiplier); }

    // Legacy API (weapon modifiers / equipment still use these as flat)
    public void AddModifier(int amount) => AddFlat(amount);
    public void RemoveModifier(int amount) => RemoveFlat(amount);
}
