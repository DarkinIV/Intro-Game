using UnityEngine;

// Settings for the Acrobat's whip pull (F). Normal enemies get pulled in and stunned,
// heavy enemies can't be moved so the Acrobat flies to them instead.
[CreateAssetMenu(fileName = "WhipPull", menuName = "Abilities/Whip Pull (Acrobat)")]
public class WhipPullData : BasicAbilityData
{
    [Header("Pull")]
    public float range = 10f;
    public float stopDistance = 2.5f;   // how far in front of us the enemy ends up
    public float pullTime = 0.25f;
    public float stunTime = 0.6f;       // how long they're stunned after landing

    [Header("Zip (heavy enemies)")]
    public float zipSpeed = 22f;
    public float zipStopDistance = 2.5f;
    public float zipMaxTime = 0.6f;
}
