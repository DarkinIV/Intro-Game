using UnityEngine;

//a "class sheet": all the numbers for one class (acrobat, scientist...) in one file
//ScriptableObject = a data file that lives in the project (not in the scene), edited in the inspector
//CreateAssetMenu adds it to the right click menu: Create > Game > Class Data
[CreateAssetMenu(fileName = "NewClass", menuName = "Game/Class Data")]
public class ClassData : ScriptableObject
{
    [Header("Info")]
    public string displayName = "New Class"; //name shown on the debug screen

    [Header("Weapon")]
    public bool usesWhip = false; //true = whip (PlayerWhip), false = the normal close attack (PlayerAttack)

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegen = 30f; //stamina refilled per second
    public float staminaRegenDelay = 1f; //seconds after using stamina before it starts refilling
    public float sprintStaminaPerSecond = 10f; //sprinting drains this much every second

    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 11f;

    [Header("Dodge")]
    public float dodgeStaminaCost = 20f;
    public float dodgeDistance = 5f; //how far one dodge goes, in units
    public float dodgeDuration = 0.3f; //how long the dodge movement takes, in seconds
    public float iFrameDuration = 0.25f; //how long u are invincible, starts the moment the dodge starts
    public float dodgeRecovery = 0.1f; //short pause after the dodge before u can dodge again

    [Header("Traversal")]
    public int airJumps = 0; //extra jumps in the air, 1 = double jump
    public float airJumpStaminaCost = 15f;
    public bool canSlide = false; //ctrl while sprinting = slide instead of dodge
    public float slideStaminaCost = 15f;
    public bool canWallRun = false;
    public float wallRunStaminaPerSecond = 15f;
}
