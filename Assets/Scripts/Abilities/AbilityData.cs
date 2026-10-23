using UnityEngine;

// Settings for an ability, saved as an asset. CreateRuntime makes the in-game version of it.
public abstract class AbilityData : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public float cooldown;
    [TextArea] public string description;

    public abstract Ability CreateRuntime();
}
