using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Walking")]
    [SerializeField] private float walkSpeed = 6f; //walking speed in units per second
    //serialized field means that it will show up in inspector so i can change it but priv bcz no other script should
    [SerializeField] private float sprintSpeed = 11f; //speed while holding the sprint key
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift; //which key sprints, can be changed in inspector
    [SerializeField] private float sprintStaminaPerSecond = 10f; //only used if the player has a Stamina script
    //walk speed, sprint speed and sprint stamina get replaced by the class sheet if the player has PlayerClass
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 40f;

    [Header("Turning")]
    [SerializeField] private float turnSpeed = 720f; //two full spins per sec

    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 2f; //how high the jump goes in units, easier to think about than a force
    [SerializeField, Min(0.05f)] private float timeToApex = 0.35f; //seconds to reach the top of the jump, smaller = snappier
    [SerializeField] private float fallMultiplier = 1.5f; //gravity is this many times stronger when falling so you come down quicker
    [SerializeField] private float coyoteTime = 0.1f; //can still jump this long after walking off a ledge
    [SerializeField] private float jumpBufferTime = 0.1f; //pressing jump this long before landing still counts
    [SerializeField] private int maxAirJumps = 0; //extra jumps in the air, 1 = double jump (the class sheet sets this)
    [SerializeField] private float airJumpStaminaCost = 15f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayers = ~0; //which layers count as floor, ~0 means everything
    [SerializeField] private float groundCheckDistance = 0.1f; //how far below the feet we look for floor

    [Header("Reference")]
    [SerializeField] private Transform cameraTransform; //draggin the main cam here later 
    //box that holds links to cam. tranform bcz we only need the cam's direction 
    //we fill by dragging cam into the slot 


    //PRIVATE BOX USED ONLY INSIDE THIS SCRIPT 

    private Rigidbody rb; //box to hold link to this obj's rigidbody 
    private Vector2 input; //box holding two numbers, keys for pushing ledt and right 
                           //SO X = LEFT/RIGHT Y = FORWARD/BACK
    private Collider col; //link to the box collider, used to find where the feet are
    private bool isGrounded; //is the player standing on something rn
    private float lastGroundedTime = -1f; //last time we were on the floor, for coyote time
    private float lastJumpPressedTime = -1f; //last time space was pressed, for the jump buffer
    private bool sprintHeld; //is the sprint key held down rn
    private PlayerLockOn lockOn; //link to the lock on script, to face the locked enemy (can be missing)
    private Stamina stamina; //link to stamina, sprinting uses it (can be missing, then sprint is free)

    //other scripts can read these
    public bool IsGrounded => isGrounded;
    public bool IsSprinting { get; private set; } //actually sprinting rn (key held, moving, and has stamina)

    //when true, this script stops controlling walking, turning and jumping (gravity still works)
    //the dodge turns this on so it can move the player itself, then turns it off again
    public bool ControlsLocked { get; set; }
    public bool SuppressGravity { get; set; } //true = no gravity at all (the wall run does its own up/down)

    private int airJumpsUsed; //how many air jumps we used since we last touched the floor
    public int AirJumpsLeft => Mathf.Max(maxAirJumps - airJumpsUsed, 0);
    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => sprintSpeed;
    public float JumpSpeed => JumpGravity * timeToApex; //the up speed of a normal jump, the wall jump uses it too

    //WHY WE LINK TO RIGIDBODY
    //answer: they sit on the same object but they dont automatically communicate unless linked 

    private void Awake() //you never call this urself, unity does it for u 
                         // void so it doesnt produce answer just carries out instructions and finishes
                         //awake runs as soon as the object comes into existence, runs before start 
    {
        rb = GetComponent<Rigidbody>(); //find rigidbody
        col = GetComponent<Collider>(); //find the collider on this same object
        lockOn = GetComponent<PlayerLockOn>(); //null if the player doesnt have the lock on script, thats fine
        stamina = GetComponent<Stamina>();
        rb.freezeRotation = true; //dont tip over, stops the physics
        rb.interpolation = RigidbodyInterpolation.Interpolate; //unity has 2 clocks running at diff speeds
                                                               // interpolation fixes the two physics steps problem by reducing the jitter and placing the object in the in between position of the two phys steps
                                                               //ask teacher why this happens
        rb.useGravity = false; //we do our own gravity in fixedupdate so the jump can be snappier than real life

        //no friction on the player, our acceleration/deceleration already handles speeding up and stopping
        //with friction, the stronger gravity presses us into the floor so hard that the floor grips us and we cant walk
        //Minimum means use the lowest friction of the two things touching, so the floor's friction cant add any back
        //this also stops us sticking to walls when jumping into them
        PhysicMaterial noFriction = new PhysicMaterial("PlayerNoFriction");
        noFriction.dynamicFriction = 0f;
        noFriction.staticFriction = 0f;
        noFriction.frictionCombine = PhysicMaterialCombine.Minimum;
        col.material = noFriction;

    }
    //the class script calls this to put in the speeds from the class sheet
    public void SetSpeeds(float walk, float sprint, float sprintCost)
    {
        walkSpeed = walk;
        sprintSpeed = sprint;
        sprintStaminaPerSecond = sprintCost;
    }

    //the class script calls this, 1 = double jump, 0 = none
    public void SetAirJumps(int count, float staminaCost)
    {
        maxAirJumps = count;
        airJumpStaminaCost = staminaCost;
    }

    //give the air jumps back (the wall jump does this so u can double jump after leaving a wall)
    public void ResetAirJumps()
    {
        airJumpsUsed = 0;
    }

    //for other scripts (wall run, slide): "was jump pressed just now?" and if yes, use up the press
    public bool ConsumeJumpPress()
    {
        if (!JumpBuffered) return false;
        lastJumpPressedTime = -1f;
        return true;
    }

    //jump was pressed in the last jumpBufferTime seconds and hasnt been used yet
    public bool JumpBuffered => Time.time - lastJumpPressedTime <= jumpBufferTime;

    // Start is called before the first frame update
    //void Start //other objects need start so we are not concerned w other objects hence we comment it out 

    // Update is called once per frame
    void Update()
    {
        //unity alr knows horizontal = A and D and vert S and W 
        //its in project manager ur just telling it to look it up 
        //connected to the box using vec2 at the top, its at the top so both update and fixedupdate can reach it 
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f); //diags tend to be faster so stop that
        // input = what to limit, what to limit it to = 1

        //"Jump" is space by default, also in project manager
        //GetButtonDown is only true on the one frame the key goes down, so we remember WHEN it happened
        //fixedupdate might not run on that exact frame so we cant just jump here
        if (Input.GetButtonDown("Jump"))
        {
            lastJumpPressedTime = Time.time;
        }

        //GetKey is true every frame the key is held, not just the first one
        sprintHeld = Input.GetKey(sprintKey);
    }

    //runs 50 times a swcond 
    private void FixedUpdate()
    {
        CheckGround();

        if (ControlsLocked)
        {
            //something else (the dodge) is moving us, only do gravity
            IsSprinting = false;
            if (!SuppressGravity) ApplyGravity();
            return; //return = stop here, skip the rest of FixedUpdate
        }

        HandleJump(); //before reading velocity below so the jump speed doesnt get overwritten

        Vector3 moveDir = GetMoveDirection();
        bool hasInput = moveDir.sqrMagnitude > 0.01f; //is player pressing anything

        //sprint only if: key held, actually moving, and (if we have stamina) not out of breath
        bool hasStaminaToSprint = stamina == null || (!stamina.IsExhausted && stamina.Current > 0f);
        IsSprinting = sprintHeld && hasInput && hasStaminaToSprint;
        if (IsSprinting && stamina != null)
        {
            stamina.Drain(sprintStaminaPerSecond * Time.fixedDeltaTime); //a bit every physics step = per second cost
        }

        Vector3 velocity = rb.velocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        //x = sideways, y = up and down, z = forward and back
        //y set to 0 in this case bcz gravity is dealing with it 

        float speed = IsSprinting ? sprintSpeed : walkSpeed; //if sprinting then sprint speed, otherwise walk speed
        Vector3 target = moveDir * speed; //the speed and dir we want to reach
        float rate = hasInput ? acceleration : deceleration; //the rate at which speed changes
        //if input, then accelerate, if no input then decelerate 

        horizontal = Vector3.MoveTowards(horizontal, target, rate * Time.fixedDeltaTime);
        // character speeds up gradually, last step is working out how big that step towards target should be  

        rb.velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        // new speed goes to rigidbody 

        //locked on = keep facing the enemy so A/D strafes round it, unless sprinting (then face where we run, like elden ring)
        bool faceLockTarget = lockOn != null && lockOn.CurrentTarget != null && !IsSprinting;
        Vector3 faceDir = moveDir;
        if (faceLockTarget)
        {
            faceDir = lockOn.CurrentTarget.LockPoint - rb.position;
            faceDir.y = 0f; //flat, so we dont tilt up/down at the enemy
        }

        if (faceDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(faceDir);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
        } //which way to face, moves the front where the nose is towards movedir so that it doesnt look weird
          // second line is for uniform tuening

        if (!SuppressGravity) ApplyGravity();
    }

    //physics: to go up jumpHeight in timeToApex seconds you need gravity = 2 * height / time²
    //so the gravity comes from the jump settings instead of the other way round
    private float JumpGravity => 2f * jumpHeight / (timeToApex * timeToApex);

    private void ApplyGravity()
    {
        float gravity = JumpGravity;
        if (!isGrounded && rb.velocity.y < 0f)
        {
            gravity *= fallMultiplier; //coming down, pull harder so the fall is quicker than the rise
        }
        //the !isGrounded matters: standing still, physics wobbles the y speed slightly below 0
        //without it we would be pressed into the floor even harder while just standing there
        //Acceleration = ignore the mass, just change the speed directly like real gravity does
        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
    }

    private void CheckGround()
    {
        Bounds b = col.bounds; //invisible box around the collider in world space, already includes the scale
        //a thin flat box, a bit smaller than the player so walls next to us dont count as floor
        Vector3 halfSize = new Vector3(b.extents.x * 0.9f, 0.05f, b.extents.z * 0.9f);

        //slide that thin box down from the middle of the player to just under the feet
        //boxcast ignores anything the box is already inside when it starts, so it never hits our own collider
        float castDistance = b.extents.y - 0.05f + groundCheckDistance;

        isGrounded = Physics.BoxCast(b.center, halfSize, Vector3.down, Quaternion.identity, castDistance, groundLayers, QueryTriggerInteraction.Ignore);
        //ignore triggers so invisible trigger zones (pickups etc later) dont count as floor

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            airJumpsUsed = 0; //touched the floor = air jumps come back
        }
    }

    private void HandleJump()
    {
        bool jumpBuffered = JumpBuffered; //pressed jump recently
        bool canJump = Time.time - lastGroundedTime <= coyoteTime; //on the floor or only just left it

        if (jumpBuffered && canJump)
        {
            DoJump();
            lastGroundedTime = -1f; //use up coyote time so the next press in the air counts as an AIR jump
            isGrounded = false;
        }
        else if (jumpBuffered && !isGrounded && airJumpsUsed < maxAirJumps)
        {
            //DOUBLE JUMP: in the air, with an air jump left. costs stamina if we have a stamina script
            lastJumpPressedTime = -1f; //use up the press either way
            if (stamina == null || stamina.TrySpend(airJumpStaminaCost))
            {
                airJumpsUsed++;
                DoJump();
            }
        }
    }

    private void DoJump()
    {
        //physics: speed needed to reach the top in timeToApex seconds is gravity * time
        Vector3 v = rb.velocity;
        rb.velocity = new Vector3(v.x, JumpSpeed, v.z); //replace up/down speed, keep the walking speed
        //replacing (not adding) matters for the double jump: falling fast + jump = full jump, not a tiny one
        lastJumpPressedTime = -1f; //use up the press so one press = one jump
    }

    private void OnDrawGizmosSelected()
    {
        //DEBUG ONLY, doesnt change the game. shows in the SCENE view (not game view) when Player is selected
        //and the Gizmos button in the scene view is on
        //a see-through box around the feet: green = grounded, red = in the air
        //only turns green in play mode bcz the ground check only runs while the game is playing
        Collider c = GetComponent<Collider>();
        if (c == null) return;
        Bounds b = c.bounds;

        //from the bottom of the check (under the feet) to a bit above the feet so it pokes out of the floor and we can see it
        float bottom = b.min.y - groundCheckDistance;
        float top = b.min.y + 0.3f;
        Vector3 center = new Vector3(b.center.x, (bottom + top) * 0.5f, b.center.z);
        Vector3 size = new Vector3(b.size.x * 0.9f, top - bottom, b.size.z * 0.9f);

        Color colour = isGrounded ? Color.green : Color.red;
        Gizmos.color = new Color(colour.r, colour.g, colour.b, 0.35f); //0.35 alpha = see-through
        Gizmos.DrawCube(center, size);
        Gizmos.color = colour;
        Gizmos.DrawWireCube(center, size); //solid outline on top so the edges are clear
    }

    //public so the dodge can roll in the direction the player is pressing
    public Vector3 GetMoveDirection()
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        //2 boxes filled with the world's directions , forward north and right east    
        //backup for the camera 

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            //throw away the backup and use the direction the cam is looking
            forward.y = 0f; //flatten it on the floor and not walking into floor 
            forward.Normalize(); //stretches to length 1, walking at full sleep nomatter how much the cam tilts 
            right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
        }

        return forward * input.y + right * input.x;

    }
}
