// AI use: Whole script written by Claude (Anthropic).
// Owner: (add name)

using UnityEngine;

// One attack an enemy can do. Each enemy has a list of these on its EnemyController,
// and the controller picks one that suits how far away the player is.
// Works for any kind of enemy: a sword slash, a bite, a tail swipe, a grab.
[System.Serializable]
public class EnemyAttack
{
    public string name = "Attack";

    [Tooltip("Which animation in the attack blend tree to play (the AttackID parameter)")]
    public int animationID;

    [Header("When to use it")]
    public float minRange = 0f;
    public float maxRange = 2f;
    [Tooltip("The enemy has to be facing the player within this angle before it starts")]
    public float maxAngle = 30f;
    public float cooldown = 2f;
    [Tooltip("Higher means it gets picked more often")]
    public float weight = 1f;

    [Header("How it plays")]
    [Tooltip("Seconds the enemy keeps turning towards the player after starting. After this the attack is locked in, so the player can dodge to the side")]
    public float trackTime = 0.4f;
    public float damageMultiplier = 1f;
    [Tooltip("How fast the animation plays, 0.5 is half speed. Needs AttackSpeed as the attack state's speed multiplier")]
    public float animationSpeed = 1f;

    [System.NonSerialized] public float readyTime;

    public bool IsReady => Time.time >= readyTime;
}
