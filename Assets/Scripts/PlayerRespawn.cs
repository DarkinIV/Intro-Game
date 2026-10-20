using System.Collections;
using UnityEngine;

//goes on the player next to Health, when the player dies they come back at the start after a short wait
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 2f; //seconds dead before coming back

    private Health health;
    private Rigidbody rb;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private Vector3 spawnPosition; //where the player started, we come back here
    private Quaternion spawnRotation;

    private void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    //subscribe to the death event when switched on, unsubscribe when switched off
    //AddListener = "when onDied happens, also run OnDied"
    private void OnEnable() => health.onDied.AddListener(OnDied);
    private void OnDisable() => health.onDied.RemoveListener(OnDied);

    private void OnDied()
    {
        StartCoroutine(RespawnAfterDelay());
    }

    //a coroutine is a method that can pause, yield return = wait here and carry on later
    private IEnumerator RespawnAfterDelay()
    {
        //no moving or attacking while dead
        //remember if the close attack was on, the acrobat has it switched OFF (uses the whip) and should stay that way
        bool attackWasOn = attack != null && attack.enabled;
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        //the whip doesnt need switching off, it checks for itself if we are dead
        PlayerTraversal traversal = GetComponent<PlayerTraversal>();
        if (traversal != null) traversal.CancelAll(); //stand back up / let go of the wall
        //the movement script does our gravity and stopping, so with it off:
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f); //stop sliding (the player has no friction)
        rb.useGravity = true; //use unity's normal gravity so we dont hang in the air if we died mid jump
        Debug.Log("Player died, respawning...");

        yield return new WaitForSeconds(respawnDelay); //wait without freezing the game

        rb.velocity = Vector3.zero; //stop any falling/sliding we had
        rb.position = spawnPosition; //rb.position moves it the physics way, safer than transform for rigidbodies
        rb.rotation = spawnRotation;
        rb.useGravity = false; //back to the movement script's own gravity
        health.Revive();
        Stamina stamina = GetComponent<Stamina>();
        if (stamina != null) stamina.Refill(); //full stamina again

        if (movement != null)
        {
            movement.ControlsLocked = false; //in case we died in the middle of a dodge
            movement.enabled = true;
        }
        if (attack != null) attack.enabled = attackWasOn; //back to how it was before dying
    }
}
