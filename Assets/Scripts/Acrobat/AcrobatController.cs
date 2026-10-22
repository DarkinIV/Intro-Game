using UnityEngine;


    //thw acro controller: subclass of playercont as well. declares its own start and update
    //the acrobat prefab unity runs this code instead of the base one, as said before in prev comments the playercontroller.cs is itself untouched and anything that looks for it such as the ability system will be able to find it so no worries on that front.
    //base game commands kept so u still move with WASD
    //Acrobat keys where space is jump , double jump so u press space twice ,wall jump, ctrl dodge slide while sprinting(might have to implement this for the rest of them but lets c)
    //right click whip pull, left click in the air slam, Q ultimate,
    //middle click lock on the shared LockOn script, same as the other classes
    //this file only does: input, walking, sprinting, jumping and gravity.
[DefaultExecutionOrder(-5)] //run before AcrobatWhip, which moves the player during pulls and slams,
public class AcrobatController : PlayerController
{
    [Header("Acrobat: Sprint")]
    [SerializeField] float acroSprintMultiplier = 1.5f;
    [SerializeField] float acroSprintStaminaPerSecond = 10f;
    [Header("Acrobat: Jump")]
    [SerializeField] float jumpHeight = 1.6f;   //units
    [SerializeField, Min(0.05f)] float timeToApex = 0.35f; //seconds to the top of the jump
    [SerializeField] float fallMultiplier = 1.5f;  // gravity is this much stronger while falling
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] int airJumps = 1;  //1 = double jump
    [SerializeField] float airJumpStaminaCost = 15f;
    [SerializeField] float groundCheckDistance = 0.15f;

    //read by the other acrobat components and the debug hud, love it when stuff is public :))
    public bool IsGroundedNow { get; private set; }
    public bool IsSprintingNow { get; private set; }
    public bool ExternalControl { get; private set; } //AcrobatWhip is moving us (zip or slam)
    public float VerticalSpeed { get; set; }   
    //our own up anddown speed (the base class gravity is not running)
    public Vector3 MoveDir
    {
        get { return moveDir; }
    }
    public int AirJumpsLeft
    {
        get { return Mathf.Max(0, airJumps - airJumpsUsed); }
    }
    //true while some move has taken over (dodge, slide, wall, whip), so normal actions should wait
    public bool IsBusy
    {
        get
        {
            if (ExternalControl) return true;
            if (dodge != null && (dodge.IsDodging || dodge.IsSliding)) return true;
            if (wall != null && wall.IsOnWall) return true;
            return false;
        }
    }
    //the locked on enemy (null if not locked or no lock on component)
    public Transform LockTarget
    {
        get
        {
            if (lockOn == null) return null;
            return lockOn.LockTarget;
        }
    }
    //physics: to go up jumpHeight in timeToApex seconds you need gravity = 2 times height / time squared
    public float Gravity
    {
        get { return 2f * jumpHeight / (timeToApex * timeToApex); }
    }
    //and the speed needed to reach the top in timeToApex seconds is gravity times time
    public float JumpSpeed
    {
        get { return Gravity * timeToApex; }
    }
    public CharacterController Capsule
    {
        get { return cc; }
    }
    //the slide starts at sprint speed, so AcrobatDodge needs to get this
    public float SprintMultiplier
    {
        get { return acroSprintMultiplier; }
    }

    // references
    CharacterController cc;
    PlayerStats acroStats;
    PlayerCombat acroCombat;
    PlayerInteract interact;
    AcrobatWhip whip;
    AcrobatDodge dodge;
    AcrobatWallMoves wall;
    LockOn lockOn;
    Camera mainCam;

    //moevement memo
    Vector3 moveDir;  //cam relative input direction
    float lastGroundedTime = -999f;
    float lastJumpPressedTime = -999f;
    int airJumpsUsed;

    //setup
    void Start()
    {
        cc = GetComponent<CharacterController>();
        acroStats = GetComponent<PlayerStats>();
        acroCombat = GetComponent<PlayerCombat>();
        interact = GetComponent<PlayerInteract>();
        whip = GetComponent<AcrobatWhip>();
        dodge = GetComponent<AcrobatDodge>();
        wall = GetComponent<AcrobatWallMoves>();
        lockOn = GetComponent<LockOn>();
        mainCam = Camera.main;
    }

//main loop

    void Update()
    {
        if (acroStats == null || acroStats.IsDead) return;

        ReadInput();

        IsGroundedNow = CheckGrounded();
        if (IsGroundedNow)
        {
            lastGroundedTime = Time.time;
            airJumpsUsed = 0; //touched the floor = double jump comes back
        }

        //only 1 of these moves the player each frame
        if (ExternalControl)
        {
            //AcrobatWhip is driving (zip to a heavy enemy or plunge slam). Nothing else moves acro this frame
            IsSprintingNow = false;
        }
        else if (dodge != null && dodge.IsDodging)
        {
            dodge.ContinueDodge();
            IsSprintingNow = false;
        }
        else if (dodge != null && dodge.IsSliding)
        {
            dodge.ContinueSlide();
        }
        else if (wall != null && wall.IsOnWall)
        {
            wall.ContinueWall();
        }
        else if (wall != null && wall.TryStartWall())
        {
            wall.ContinueWall();
            IsSprintingNow = false;
        }
        else if (dodge != null && dodge.TryStartDodgeOrSlide())
        {
         //the dodge or slide started this frame, nothing more to do
        }
        else
        {
            NormalMove();
        }

        HandleActions();
    }

    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        //forward and right are taken from the camera, flattened so we dont walk into the floor
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (mainCam != null)
        {
            forward = mainCam.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            right = mainCam.transform.right;
            right.y = 0f;
            right.Normalize();
        }
        moveDir = forward * v + right * h;
        if (moveDir.magnitude > 1f) moveDir.Normalize(); //diagonals arent faster(keisi-combat did it first :))
        //we remember when space was pressed, the move that wants it checks the time later (buffah)
        if (Input.GetKeyDown(KeyCode.Space)) lastJumpPressedTime = Time.time;
    }
    //space was pressed in the last jumpBufferTime seconds and hasnt been used yet
    public bool JumpBuffered()
    {
        return Time.time - lastJumpPressedTime <= jumpBufferTime;
    }
    public void ConsumeJump()
    {
        lastJumpPressedTime = -999f; //use up the press so one press = one jump
    }
    //the wall jump gives the double jump back
    public void ResetAirJumps()
    {
        airJumpsUsed = 0;
    }
    //used by every move: go sideways with the given speed + our up and down speed with gravity
    public void MoveWithGravity(Vector3 horizontalVelocity)
    {
        ApplyGravity();
        Vector3 velocity = horizontalVelocity + Vector3.up * VerticalSpeed;
        CollisionFlags flags = cc.Move(velocity * Time.deltaTime);
        bool hitCeiling = (flags & CollisionFlags.Above) != 0;
        if (hitCeiling && VerticalSpeed > 0f) VerticalSpeed = 0f; //bumped the ceiling, stop going up
    }
    void ApplyGravity()
    {
        if (IsGroundedNow && VerticalSpeed <= 0f)
        {
            VerticalSpeed = -2f; //small downward push keeps playe glued to slopes (same trick as the base controller)
            return;
        }

        float g = Gravity;
        if (VerticalSpeed < 0f) g = g * fallMultiplier; //falling = pull harder so it isnt floaty
        VerticalSpeed -= g * Time.deltaTime;
    }

    //char controlled . is grounded flickers on edges sooo look just under the feet
    bool CheckGrounded()
    {
        if (cc.isGrounded) return true;

        //a ball the size of the capsule's bottom, pushed a little bit down
        Vector3 bottomSphere = transform.position + cc.center - Vector3.up * (cc.height * 0.5f - cc.radius);
        float distance = groundCheckDistance + cc.skinWidth;
        RaycastHit[] hits = Physics.SphereCastAll(bottomSphere, cc.radius * 0.9f, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore);
        //had to scour the internet for this but here it is i guess, stackoverflow still GOTS IT 
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; //our own body doesnt count as floor
            return true;
        }
        return false;
    }

    //normal movement + jump
    void NormalMove()
    {
        float speed = acroStats.GetMoveSpeed();
        bool moving = moveDir.sqrMagnitude > 0.01f;
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);
        //Sprint, same idea as the base controller(press to start, release or stop moving to end), costs stamina
        if (shiftHeld && moving && IsGroundedNow)
        {
            float cost = acroSprintStaminaPerSecond * Time.deltaTime;
            if (acroStats.TrySpendStamina(cost, false))
            {
                IsSprintingNow = true;
                speed = speed * acroSprintMultiplier;
            }
            else
            {
                IsSprintingNow = false; //out of stamina
            }
        }
        else if (!shiftHeld || !moving)
        {
            IsSprintingNow = false;
        }
        HandleJump();
        FaceDirection(moving);
        MoveWithGravity(moveDir * speed);
    }
    void HandleJump()
    {
        if (!JumpBuffered()) return;

        bool canGroundJump = Time.time - lastGroundedTime <= coyoteTime; //on the floor or only just left it
        if (canGroundJump)
        {
            VerticalSpeed = JumpSpeed;
            lastGroundedTime = -999f; // the next press in the air counts as an air jump
            IsGroundedNow = false;
            ConsumeJump();
        }
        else if (airJumpsUsed < airJumps)
        {
            //double jumpt moment 
            ConsumeJump();
            if (acroStats.TrySpendStamina(airJumpStaminaCost))
            {
                airJumpsUsed++;
                VerticalSpeed = JumpSpeed; //a double jump from a fast fall is still a full jump
            }
        }
    }

    void FaceDirection(bool moving)
    {
        float turn = acroStats.turnSpeed.Value * Time.deltaTime;
        Transform target = LockTarget;

        // Locked on and not sprinting means u keep facing enemy and using a or d just circles around it (same as the base controller)
        //elden ring style 
        if (target != null && !IsSprintingNow)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turn);
            }
        }
        else if (moving)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), turn);
        }
    }
    void HandleActions()
    {
        if (Input.GetMouseButtonDown(0) && !IsBusy)
        {
            if (IsGroundedNow || whip == null)
            {
                acroCombat.TryAttack();//whip crack = the equipped weapon's normal attack
            }
            else
            {
                whip.TryStartSlam();//n the air = plunge slam
            }
        }
        if (Input.GetMouseButtonDown(1) && !IsBusy && whip != null)
        {
            whip.TryPull();
        }
        if (Input.GetKeyDown(KeyCode.E) && !IsBusy && interact != null)
        {
            interact.TryInteract();
        }
    }

    //Called by AcrobatWhip when it wants to move the player itself (zip to a heavy enemy, plunge slam).
    public void BeginExternalControl()
    {
        if (dodge != null) dodge.CancelAll();
        if (wall != null) wall.EndWall();
        ExternalControl = true;
    }
    public void EndExternalControl()
    {
        ExternalControl = false;
    }
    public void MoveExternal(Vector3 velocity)
    {
        cc.Move(velocity * Time.deltaTime);
        VerticalSpeed = velocity.y; //so gravity carries on smoothly afterwards
    }
}
