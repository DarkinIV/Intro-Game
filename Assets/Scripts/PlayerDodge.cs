using UnityEngine;

//dodge roll with i-frames (invincible for a moment), goes on the player
//press ctrl: roll the way you are pressing, or hop backwards if you arent pressing anything (like elden ring)
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Health))]
public class PlayerDodge : MonoBehaviour
{
    [Header("Dodge")]
    [SerializeField] private KeyCode dodgeKey = KeyCode.LeftControl;
    [SerializeField] private float staminaCost = 20f;
    [SerializeField] private float dodgeDistance = 5f;
    [SerializeField] private float dodgeDuration = 0.3f;
    [SerializeField] private float iFrameDuration = 0.25f;
    [SerializeField] private float recovery = 0.1f;
    [SerializeField] private float inputBuffer = 0.15f; //pressing ctrl this long before u are allowed to dodge still counts
    //the numbers above get replaced by the class sheet if the player has PlayerClass

    [Header("Debug")]
    [SerializeField] private bool showIFrameDebug = true; //turn the player cyan + write in the console during i-frames
    [SerializeField] private Color iFrameColor = Color.cyan;

    public bool IsDodging { get; private set; }

    private PlayerMovement movement;
    private Health health;
    private Stamina stamina;
    private PlayerTraversal traversal; //for the slide (can be missing)
    private Rigidbody rb;
    private Renderer rend;
    private Color normalColor;

    private float dodgePressedTime = -999f;
    private float dodgeEndTime;
    private float nextDodgeAllowedTime;
    private Vector3 dodgeDirection;
    private bool wasInvulnerable; //for the debug, to notice when i-frames switch on or off

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<Health>();
        stamina = GetComponent<Stamina>();
        traversal = GetComponent<PlayerTraversal>();
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        if (rend != null) normalColor = rend.material.color;
    }

    //the class script calls this to put in the numbers from the class sheet
    public void Setup(ClassData data)
    {
        staminaCost = data.dodgeStaminaCost;
        dodgeDistance = data.dodgeDistance;
        dodgeDuration = Mathf.Max(data.dodgeDuration, 0.05f); //never 0, we divide by it
        iFrameDuration = data.iFrameDuration;
        recovery = data.dodgeRecovery;
    }

    void Update()
    {
        if (Input.GetKeyDown(dodgeKey))
        {
            dodgePressedTime = Time.time; //remember the press, the dodge starts in FixedUpdate (physics)
        }

        if (showIFrameDebug) UpdateIFrameDebug();
    }

    private void FixedUpdate()
    {
        if (IsDodging)
        {
            ContinueDodge();
            return;
        }

        bool pressedRecently = Time.time - dodgePressedTime <= inputBuffer;
        if (!pressedRecently) return;

        //ctrl while sprinting = SLIDE instead of dodge (only if this class can slide)
        if (traversal != null && movement.IsSprinting && traversal.TryStartSlide())
        {
            dodgePressedTime = -999f; //use up the press
            return;
        }

        if (CanDodge())
        {
            StartDodge();
        }
    }

    private bool CanDodge()
    {
        return movement.enabled //movement is switched off while dead
            && !health.IsDead
            && movement.IsGrounded //no dodging in the air
            && !movement.ControlsLocked //something else (whip zip, slam) is moving us
            && Time.time >= nextDodgeAllowedTime;
    }

    private void StartDodge()
    {
        dodgePressedTime = -999f; //use up the press

        if (stamina != null && !stamina.TrySpend(staminaCost))
        {
            Debug.Log("Dodge failed: no stamina");
            return;
        }

        //roll where we are pressing, or straight backwards if nothing is pressed (a backstep)
        dodgeDirection = movement.GetMoveDirection();
        dodgeDirection.y = 0f;
        if (dodgeDirection.sqrMagnitude < 0.01f) dodgeDirection = -transform.forward;
        dodgeDirection.Normalize();

        IsDodging = true;
        dodgeEndTime = Time.time + dodgeDuration;
        movement.ControlsLocked = true; //we drive the movement now
        health.MakeInvulnerable(iFrameDuration); //the i-frames start right as the dodge starts
    }

    private void ContinueDodge()
    {
        if (Time.time >= dodgeEndTime)
        {
            EndDodge();
            return;
        }

        //speed = distance / time, constant speed for the whole dodge
        float speed = dodgeDistance / dodgeDuration;
        Vector3 v = dodgeDirection * speed;
        rb.velocity = new Vector3(v.x, rb.velocity.y, v.z); //keep the falling speed so gravity still works
    }

    private void EndDodge()
    {
        IsDodging = false;
        movement.ControlsLocked = false; //hand control back to normal movement
        nextDodgeAllowedTime = Time.time + recovery;

        //cut most of the dodge speed so we dont slide on after the roll (the player has no friction)
        Vector3 v = rb.velocity;
        rb.velocity = new Vector3(v.x * 0.3f, v.y, v.z * 0.3f);
    }

    //=============== DEBUG ===============

    private void UpdateIFrameDebug()
    {
        bool invulnerable = health.IsInvulnerable;
        if (invulnerable == wasInvulnerable) return; //nothing changed since last frame
        wasInvulnerable = invulnerable;

        //F2 = show the time with 2 decimals, so u can see exactly how long the window was
        if (invulnerable) Debug.Log($"<color=cyan>I-frames ON</color> at {Time.time:F2}s");
        else Debug.Log($"<color=cyan>I-frames OFF</color> at {Time.time:F2}s");

        if (rend != null) rend.material.color = invulnerable ? iFrameColor : normalColor;
    }
}
