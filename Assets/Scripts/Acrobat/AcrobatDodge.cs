using UnityEngine;

//the acrobat's dodge roll and slide, both on Ctrl
// Ctrl = dodge roll in the direction u press (backstep if nothing pressed), with i-frames
// Ctrl while sprinting = slide: drop to half height and shoot forward, space jumps out of it keeping the speed
//AcrobatController decides when these run (it calls TryStartDodgeOrSlide/ContinueDodge/ContinueSlide)
[DefaultExecutionOrder(-10)] //read the ctrl key before the controller looks at it
[RequireComponent(typeof(AcrobatController))]
public class AcrobatDodge : MonoBehaviour
{
    [Header("Dodge (Ctrl)")]
    [SerializeField] KeyCode dodgeKey = KeyCode.LeftControl;
    [SerializeField] float dodgeDistance = 5f;
    [SerializeField] float dodgeDuration = 0.3f;
    [SerializeField] float dodgeIFrames = 0.3f;               //invulnerable from the start of the dodge for this long
    [SerializeField] float dodgeStaminaCost = 20f;
    [SerializeField] float dodgeCooldown = 0.15f;
    [SerializeField] float dodgeInputBuffer = 0.15f;          //pressing ctrl this long before u are allowed still counts

    [Header("Slide (Ctrl while sprinting)")]
    [SerializeField] float slideDuration = 0.6f;
    [SerializeField] float slideBoost = 1.15f;                //starts this much faster than the sprint
    [SerializeField] float slideStaminaCost = 15f;
    [SerializeField, Range(0.3f, 0.9f)] float slideHeightPercent = 0.5f;

    public bool IsDodging { get; private set; }
    public bool IsSliding { get; private set; }

    AcrobatController acro;
    PlayerStats stats;
    AcrobatInvulnerability invuln;
    CharacterController cc;

    float lastDodgePressedTime = -999f;
    float dodgeEndTime;
    float nextDodgeTime;
    Vector3 dodgeDir;

    float slideTimer;
    float slideStartSpeed;
    Vector3 slideDir;
    float standHeight;  //the normal capsule size, so the slide can shrink it and put it back
    Vector3 standCenter;

    void Awake()
    {
        acro = GetComponent<AcrobatController>();
        stats = GetComponent<PlayerStats>();
        cc = GetComponent<CharacterController>();
        invuln = GetComponent<AcrobatInvulnerability>();
        if (invuln == null)
        {
            invuln = gameObject.AddComponent<AcrobatInvulnerability>(); //the dodge needs it for the i-frames
        }

        standHeight = cc.height;
        standCenter = cc.center;
    }

    void Update()
    {
        //remember when ctrl was pressed, the controller asks us later (again buffer)
        if (Input.GetKeyDown(dodgeKey)) lastDodgePressedTime = Time.time;
    }

    bool DodgeBuffered()
    {
        return Time.time - lastDodgePressedTime <= dodgeInputBuffer;
    }
    //called by the controller when nothing else is moving the player if true = we started a dodge or slide
    public bool TryStartDodgeOrSlide()
    {
        if (!DodgeBuffered()) return false;
        if (!acro.IsGroundedNow) return false;             //no dodging in the air
        if (Time.time < nextDodgeTime) return false;       //still recovering from the last one

        lastDodgePressedTime = -999f; //use up the press

        Vector3 moveDir = acro.MoveDir;
        bool pressingADirection = moveDir.sqrMagnitude > 0.01f;

        // Ctrl while sprinting = slide
        if (acro.IsSprintingNow && pressingADirection)
        {
            if (!stats.TrySpendStamina(slideStaminaCost)) return false;
            StartSlide(moveDir);
            return true;
        }
        //otherwise = dodge roll
        if (!stats.TrySpendStamina(dodgeStaminaCost)) return false;

        Vector3 dir = moveDir;
        if (!pressingADirection) dir = -transform.forward; // nothing pressed = backstep
        StartDodge(dir);
        return true;
    }

    //also used by the ultimate's auto dodge (no stamina check there, its GOTTA be free)
    public void StartDodge(Vector3 dir)
    {
        if (IsSliding) EndSlide();

        dir.y = 0f;
        dir.Normalize();

        dodgeDir = dir;
        IsDodging = true;
        dodgeEndTime = Time.time + dodgeDuration;
        invuln.MakeInvulnerable(dodgeIFrames); //the i-frames start right as the dodge starts

        if (acro.LockTarget == null) transform.rotation = Quaternion.LookRotation(dir); //locked on = keep facing the enemy
    }

    public void ContinueDodge()
    {
        if (Time.time >= dodgeEndTime)
        {
            IsDodging = false;
            nextDodgeTime = Time.time + dodgeCooldown;
            return;
        }

        float speed = dodgeDistance / dodgeDuration; //speed = distance / time
        acro.MoveWithGravity(dodgeDir * speed);
    }

    void StartSlide(Vector3 moveDir)
    {
        slideDir = moveDir.normalized;
        slideStartSpeed = stats.GetMoveSpeed() * acro.SprintMultiplier * slideBoost;
        slideTimer = 0f;
        IsSliding = true;

        // shrink the capsule, keeping the feet where they are
        float newHeight = standHeight * slideHeightPercent;
        cc.height = newHeight;
        cc.center = standCenter - Vector3.up * (standHeight - newHeight) * 0.5f;

        transform.rotation = Quaternion.LookRotation(slideDir);
    }

    public void ContinueSlide()
    {
        slideTimer += Time.deltaTime;
        float t = Mathf.Clamp01(slideTimer / slideDuration);                 //0 at the start, 1 at the end
        float speed = Mathf.Lerp(slideStartSpeed, stats.GetMoveSpeed(), t);  //slows down to walking speed

        acro.MoveWithGravity(slideDir * speed);

        bool roomToStand = HasRoomToStand();

        //Space during the slide: end it and leave the press buffered, so the normal jump fires next frame with this speed
        if (acro.JumpBuffered() && roomToStand)
        {
            EndSlide();
            return;
        }

        bool slideOver = t >= 1f || !acro.IsGroundedNow;
        if (slideOver && roomToStand)
        {
            EndSlide();
        }
        //no room (still under something) = keep sliding slowly until there is
    }

    void EndSlide()
    {
        IsSliding = false;
        cc.height = standHeight;
        cc.center = standCenter;
    }

    //stop whatever we are doing (the whip takes over the player)
    public void CancelAll()
    {
        IsDodging = false;
        if (IsSliding) EndSlide();
    }

    // checks the space the full capsule would take
    bool HasRoomToStand()
    {
        float r = cc.radius;
        Vector3 bottom = transform.position + standCenter - Vector3.up * (standHeight * 0.5f - r);
        Vector3 top = transform.position + standCenter + Vector3.up * (standHeight * 0.5f - r);

        Collider[] hits = Physics.OverlapCapsule(bottom, top, r * 0.95f, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; //ignore ourselves
            return false; //something is in the way
        }
        return true;
    }

    void OnDisable()
    {
        if (cc != null && IsSliding) EndSlide(); //dont stay crouched if switched off mid slide
    }
}
