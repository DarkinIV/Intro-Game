using UnityEngine;

public abstract class AbilityData : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public float cooldown;
    [TextArea] public string description;
    public abstract Ability CreateRuntime();
}
