using UnityEngine;

public enum PlayerClassType { Acrobat, Scientist } //the dropdown options

//goes on the player: pick a class from the dropdown and it puts that class sheet's numbers into the other scripts
//RequireComponent = adding this also adds Health, Stamina and PlayerDodge if they are missing
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Stamina))]
[RequireComponent(typeof(PlayerDodge))]
public class PlayerClass : MonoBehaviour
{
    [SerializeField] private PlayerClassType selectedClass = PlayerClassType.Acrobat;

    [Header("Class Sheets")]
    [SerializeField] private ClassData acrobatData; //drag Assets/Classes/Acrobat here
    [SerializeField] private ClassData scientistData; //drag Assets/Classes/Scientist here

    [Header("Weapon Looks (optional)")]
    [SerializeField] private GameObject swordVisual; //the placeholder sword, hidden when the class uses the whip

    public ClassData Current { get; private set; } //the class sheet in use rn, other scripts can read it

    private void Awake()
    {
        ApplyClass();
    }

    //OnValidate runs whenever a value is changed in the inspector
    //so u can switch class from the dropdown WHILE the game is playing and test the difference straight away
    private void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled) ApplyClass();
    }

    private void ApplyClass()
    {
        ClassData data = selectedClass == PlayerClassType.Acrobat ? acrobatData : scientistData;
        if (data == null)
        {
            Debug.LogWarning($"No class sheet in the {selectedClass} slot on PlayerClass, drag one in");
            return;
        }
        Current = data;

        //hand each number to the script that uses it
        GetComponent<Health>().SetMaxHealth(data.maxHealth);
        GetComponent<Stamina>().Setup(data.maxStamina, data.staminaRegen, data.staminaRegenDelay);
        GetComponent<PlayerDodge>().Setup(data);

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetSpeeds(data.walkSpeed, data.sprintSpeed, data.sprintStaminaPerSecond);
            movement.SetAirJumps(data.airJumps, data.airJumpStaminaCost);
        }

        PlayerTraversal traversal = GetComponent<PlayerTraversal>();
        if (traversal != null) traversal.Setup(data);
        else if (data.canSlide || data.canWallRun) Debug.LogWarning($"{data.displayName} can slide/wall run but the Player has no PlayerTraversal");

        //weapon: switch ON the one this class uses and switch OFF the other, so left click only does one thing
        PlayerWhip whip = GetComponent<PlayerWhip>();
        PlayerAttack attack = GetComponent<PlayerAttack>();
        bool whipActive = data.usesWhip && whip != null; //if the whip script is missing, fall back to the normal attack
        if (data.usesWhip && whip == null) Debug.LogWarning($"{data.displayName} uses the whip but the Player has no PlayerWhip");
        if (whip != null) whip.enabled = whipActive;
        if (attack != null) attack.enabled = !whipActive;
        if (swordVisual != null) swordVisual.SetActive(!whipActive);

        Debug.Log($"Class set to {data.displayName}");
    }
}
