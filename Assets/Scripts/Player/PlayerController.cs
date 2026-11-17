// AI use: Helped with writing the jump, slide, and wall climb and some simplification.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Movement and input for all the player characters.
// Each character has its own controller that inherits from this (AcrobatController etc)
// and overrides the virtual methods at the bottom for its abilities.
// Things like air jumps and wall run time come from the character's Agility rank.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Animator parameters, turned into numbers once instead of looking up the name every frame
    static readonly int MoveStateParam = Animator.StringToHash("MoveState");
    static readonly int HorizontalParam = Animator.StringToHash("Horizontal");
    static readonly int VerticalParam = Animator.StringToHash("Vertical");
    static readonly int GroundedParam = Animator.StringToHash("isGrounded");
    static readonly int JumpTrigger = Animator.StringToHash("Jump");
    static readonly int DashTrigger = Animator.StringToHash("Dash");
    static readonly int DrinkTrigger = Animator.StringToHash("Drink");
    static readonly int StaggerTrigger = Animator.StringToHash("Stagger");

    // Reused by the ground, wall and ceiling checks so they don't make new arrays every frame
    static RaycastHit[] castHits = new RaycastHit[16];
    static Collider[] overlapHits = new Collider[16];

    [Header("Movement")]
    [SerializeField] float sprintMultiplier = 1.5f;
    [SerializeField] float sprintStaminaPerSecond = 15f;

    [Header("Jump")]
    [SerializeField] float jumpHeight = 1.4f;
    [Min(0.05f)][SerializeField] float timeToApex = 0.375f;
    [SerializeField] float fallMultiplier = 1.5f;     // fall faster than you go up
    [SerializeField] float coyoteTime = 0.1f;         // you can still jump just after walking off an edge
    [SerializeField] float jumpBufferTime = 0.1f;     // pressing jump just before landing still counts
    [SerializeField] float airJumpStaminaCost = 15f;
    [SerializeField] float groundCheckDistance = 0.15f;

    [Header("Dash")]
    [SerializeField] float dashDistance = 4f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float dashCooldown = 0.4f;
    [SerializeField] float dashStaminaCost = 20f;

    [Header("Slide (dash while sprinting)")]
    [SerializeField] float slideDuration = 0.6f;
    [SerializeField] float slideBoost = 1.15f;
    [SerializeField] float slideStaminaCost = 15f;
    [Range(0.3f, 0.9f)][SerializeField] float slideHeightPercent = 0.5f;

    [Header("Wall Run / Climb")]
    [SerializeField] float wallRunMaxTime = 0.8f;
    [SerializeField] float wallRunUpSpeed = 2f;
    [SerializeField] float wallRunEndFallSpeed = 4f;
    [SerializeField] float wallClimbMaxTime = 0.55f;
    [SerializeField] float wallClimbSpeed = 6f;
    [Tooltip("If you run at a wall within this angle you climb it, otherwise you run along it")]
    [Range(0, 90)][SerializeField] float climbAngle = 35f;
    [SerializeField] float wallStaminaPerSecond = 20f;
    [SerializeField] float wallCheckDistance = 0.5f;
    [SerializeField] float wallMinHeightAboveGround = 0.8f;
    [SerializeField] float wallJumpPush = 7f;
    [SerializeField] LayerMask wallLayers = ~0;

    [Header("Potion")]
    [SerializeField] float drinkTime = 2f;   // length of the Drinking animation

    [Header("Combat")]
    [SerializeField] float lockOnRange = 12f;
    [SerializeField] float lockLossRange = 24f;
    
    protected CharacterController controller;
    protected PlayerStats stats;
    protected PlayerCombat combat;
    PlayerInteractor interactor;
    Camera cam;
    CameraController cameraController;
    Animator animator;

    Vector3 moveDirection;
    Vector3 lastMoveDirection = Vector3.forward;
    float verticalVelocity;
    bool isSprinting;
    Transform lockedTarget;
    CharacterStats lockedEnemyStats; // the locked enemy's stats, so we don't look them up every frame
    float nextDrinkTime;

    // Jumping
    float lastGroundedTime = -999f;
    float lastJumpPressedTime = -999f;
    int airJumpsUsed;

    // Dashing
    float nextDashTime;
    Coroutine dashRoutine;
    HashSet<CharacterCombat> perfectDodgedBy = new HashSet<CharacterCombat>();
    float dodgeIFramesUntil;

    // Sliding
    float slideTimer;
    float slideStartSpeed;
    Vector3 slideDirection;
    float standHeight;
    Vector3 standCenter;

    // Wall running and climbing
    float wallTimer;
    float wallSpeed;
    Vector3 wallNormal;
    Vector3 wallDirection;
    Collider currentWall;
    Collider lastWall;
    float lastWallJumpTime = -999f;
    float nextWallTime;
    Vector3 wallKickVelocity;
    float wallKickUntil;

    public bool IsDashing { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsWallRunning { get; private set; }
    public bool IsWallClimbing { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool ExternalControl { get; private set; }

    public bool IsOnWall => IsWallRunning || IsWallClimbing;
    public bool IsBusy => ExternalControl || IsDashing || IsSliding || IsOnWall || stats.IsStaggered;
    public Transform LockTarget => lockedTarget;
    public Vector3 MoveDirection => moveDirection;
    public int AirJumpsLeft => Mathf.Max(0, stats.AirJumps - airJumpsUsed);
    public float DashCooldownRemaining => Mathf.Max(0, nextDashTime - Time.time);
    public float DashCooldownDuration => dashCooldown;

    // Gravity is worked out so a normal jump reaches jumpHeight in timeToApex seconds.
    // Agility then makes the jump itself a bit higher for some characters.
    float Gravity => 2f * jumpHeight / (timeToApex * timeToApex);
    float JumpSpeed => Mathf.Sqrt(2f * Gravity * jumpHeight * stats.JumpHeightMultiplier);
    bool JumpBuffered => Time.time - lastJumpPressedTime <= jumpBufferTime;

    protected virtual void Awake()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
        combat = GetComponent<PlayerCombat>();
        interactor = GetComponent<PlayerInteractor>();
        animator = GetComponent<Animator>();

        standHeight = controller.height;
        standCenter = controller.center;
    }

    protected virtual void Start()
    {
        cam = Camera.main;
        cameraController = cam.GetComponent<CameraController>();
    }

    protected virtual void OnEnable()
    {
        stats.OnHitAvoided += HandleHitAvoided;
        stats.OnStaggered += Stagger;
    }

    protected virtual void OnDisable()
    {
        stats.OnHitAvoided -= HandleHitAvoided;
        stats.OnStaggered -= Stagger;

        CancelDash();
        if (IsSliding)
            EndSlide();

        IsWallRunning = false;
        IsWallClimbing = false;
        ExternalControl = false;
    }

    protected virtual void Update()
    {
        // Dead, or the game is paused (a tutorial is open)
        if (stats.IsDead || Time.timeScale == 0f)
            return;

        ReadMoveInput();
        UpdateGrounded();

        // Only one of these moves the player each frame.
        // Abilities that move the player call MoveExternal, and the dash moves us in its coroutine.
        if (ExternalControl || IsDashing) { }
        else if (IsSliding) ContinueSlide();
        else if (IsOnWall || TryStartWall()) ContinueWall();
        else Move();

        if (Input.GetKeyDown(KeyCode.LeftControl))
            Dash();

        UpdateLockOn();

        if (!IsBusy)
        {
            if (Input.GetMouseButtonDown(0))
                Attack();

            if (Input.GetKeyDown(KeyCode.E))
                interactor.TryInteract();

            // Can't start another drink until this one has finished
            if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextDrinkTime && stats.CanDrinkPotion)
            {
                nextDrinkTime = Time.time + drinkTime;
                animator.SetTrigger(DrinkTrigger);
            }
        }

        HandleAbilityKeys();
        UpdateMoveAnimation();
    }

    void UpdateMoveAnimation()
    {
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        bool moving = velocity.magnitude > 0.1f && moveDirection.magnitude > 0.1f;

        float moveState = 0f; //0 idle, 1 walk, 2 run, 3 sprint
        if (moving)
            moveState = isSprinting ? 3f : 2f;

        animator.SetFloat(MoveStateParam, moveState, 0.1f, Time.deltaTime);
        if (lockedTarget)
        {
            animator.SetFloat(HorizontalParam, Input.GetAxis("Horizontal"), 0.1f, Time.deltaTime);
            animator.SetFloat(VerticalParam, Input.GetAxis("Vertical"), 0.1f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat(HorizontalParam, 0f, 0.1f, Time.deltaTime);
            animator.SetFloat(VerticalParam, 0f, 0.1f, Time.deltaTime);
        }
    }

    // The ability keys are set in the character's data
    void HandleAbilityKeys()
    {
        // No abilities while staggered and held ones stop
        bool canAct = !stats.IsStaggered;

        foreach (Ability ability in stats.Abilities)
        {
            if (ability is ChanneledAbility channel)
                UpdateChannel(channel, channel.Tick(canAct && Input.GetKey(channel.Key) && CanChannel(channel)));
            else if (canAct && Input.GetKeyDown(ability.Key))
                ability.TryUse(this);
        }

        if (canAct && stats.Ultimate != null && Input.GetKeyDown(stats.Ultimate.Key))
            stats.Ultimate.TryUse(this);
    }

    // Moving

    void ReadMoveInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Move relative to where the camera is facing
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        moveDirection = forward * vertical + right * horizontal;
        if (moveDirection.magnitude > 1)
            moveDirection.Normalize();

        // Meditating or reeling from a stagger
        if (MovementBlocked || stats.IsStaggered)
            moveDirection = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.Space) && !stats.IsStaggered)
            lastJumpPressedTime = Time.time;
    }

    void Move()
    {
        float speed = stats.GetMoveSpeed() * combat.MoveMultiplier;

        UpdateSprint();
        if (isSprinting)
            speed *= sprintMultiplier;

        HandleJump();

        if (!combat.IsCommitted && !stats.IsStaggered)
        {
            // Face the locked enemy, unless we're sprinting away
            if (lockedTarget != null && !isSprinting)
            {
                Vector3 toTarget = lockedTarget.position - transform.position;
                toTarget.y = 0f;
                TurnTowards(toTarget);
            }
            else if (moveDirection.magnitude > 0.1f)
            {
                lastMoveDirection = moveDirection;
                TurnTowards(moveDirection);
            }
        }

        Vector3 movement = moveDirection * speed;

        // Still being pushed away from a wall we jumped off
        if (Time.time < wallKickUntil)
            movement += wallKickVelocity;

        MoveWithGravity(movement);
    }

    void UpdateSprint()
    {
        // Sprinting only starts on a fresh Shift press, not by holding it from before
        if (combat.IsMovementLocked)
            isSprinting = false;
        else if (Input.GetKeyDown(KeyCode.LeftShift) && moveDirection.magnitude > 0.1f)
            isSprinting = true;

        if (!Input.GetKey(KeyCode.LeftShift) || moveDirection.magnitude <= 0.1f)
            isSprinting = false;

        // Stop sprinting when we run out of stamina
        if (isSprinting && !stats.TrySpendStamina(sprintStaminaPerSecond * Time.deltaTime, false))
            isSprinting = false;
    }

    void TurnTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, stats.turnSpeed * Time.deltaTime);
    }

    void MoveWithGravity(Vector3 horizontalVelocity)
    {
        ApplyGravity();

        Vector3 movement = horizontalVelocity;
        movement.y = verticalVelocity;

        CollisionFlags flags = controller.Move(movement * Time.deltaTime);

        // Bumped our head on a ceiling
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;
    }

    void ApplyGravity()
    {
        // Keep a small downward speed on the ground so we stay stuck to slopes
        if (IsGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -2f;
            return;
        }

        float gravity = Gravity;
        if (verticalVelocity < 0f)
            gravity *= fallMultiplier;

        verticalVelocity -= gravity * Time.deltaTime;
    }

    void UpdateGrounded()
    {
        // Not grounded while we're still going up from a jump
        IsGrounded = verticalVelocity <= 0f && CheckGrounded();

        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
            airJumpsUsed = 0;
        }

        animator.SetBool(GroundedParam, IsGrounded);
    }

    // controller.isGrounded flickers on edges so we also check a little below the feet
    bool CheckGrounded()
    {
        if (controller.isGrounded)
            return true;

        Vector3 bottomSphere = transform.position + controller.center - Vector3.up * (controller.height * 0.5f - controller.radius);
        float distance = groundCheckDistance + controller.skinWidth;

        int hitCount = Physics.SphereCastNonAlloc(bottomSphere, controller.radius * 0.9f, Vector3.down, castHits, distance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            if (!castHits[i].transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    // Jumping

    void HandleJump()
    {
        if (!JumpBuffered)
            return;

        if (Time.time - lastGroundedTime <= coyoteTime)
        {
            Jump();
        }
        else if (airJumpsUsed < stats.AirJumps)
        {
            if (stats.TrySpendStamina(airJumpStaminaCost))
            {
                airJumpsUsed++;
                Jump();
            }
            else
            {
                ConsumeJump();
            }
        }
    }

    void Jump()
    {
        ConsumeJump();
        verticalVelocity = JumpSpeed;
        lastGroundedTime = -999f;
        IsGrounded = false;
        animator.SetTrigger(JumpTrigger);
    }

    void ConsumeJump()
    {
        lastJumpPressedTime = -999f;
    }

    // Dashing

    void Dash()
    {
        if (IsDashing || IsSliding || ExternalControl || combat.IsCommitted || stats.IsStaggered || Time.time < nextDashTime)
            return;

        // Pressing dash while sprinting does a slide instead
        if (isSprinting && IsGrounded && moveDirection.magnitude > 0.1f)
        {
            TryStartSlide();
            return;
        }

        if (!stats.TrySpendStamina(dashStaminaCost))
            return;

        // Dash where we're moving, or where we last moved if standing still
        Vector3 direction = moveDirection.magnitude > 0.1f ? moveDirection : lastMoveDirection;
        StartDash(direction);
    }

    // Abilities can call this for a free dash (no stamina or cooldown check)
    protected void StartDash(Vector3 direction)
    {
        if (IsSliding)
            EndSlide();
        if (IsOnWall)
            EndWall();

        combat.CancelAttack();

        direction.y = 0;
        direction.Normalize();
        transform.rotation = Quaternion.LookRotation(direction);

        nextDashTime = Time.time + dashCooldown;

        if (dashRoutine != null)
            StopCoroutine(dashRoutine);
        dashRoutine = StartCoroutine(DashMovement(direction));

        animator.SetTrigger(DashTrigger);

        OnDashStarted();
    }

    IEnumerator DashMovement(Vector3 direction)
    {
        IsDashing = true;
        perfectDodgedBy.Clear();

        // Invulnerable from the start of the dash, for as long as the i-frame stat says
        dodgeIFramesUntil = Time.time + stats.IFrameDuration;
        stats.GrantInvulnerability(stats.IFrameDuration);

        // Don't keep going up if we dashed during a jump
        verticalVelocity = Mathf.Min(verticalVelocity, 0f);

        float speed = dashDistance / dashDuration;
        for (float elapsed = 0f; elapsed < dashDuration; elapsed += Time.deltaTime)
        {
            MoveWithGravity(direction * speed);
            yield return null;
        }

        IsDashing = false;
        dashRoutine = null;
    }

    void CancelDash()
    {
        if (dashRoutine != null)
            StopCoroutine(dashRoutine);

        dashRoutine = null;
        IsDashing = false;
    }

    // An enemy hit us while the dash made us invulnerable (the i-frames can last a bit longer than the dash itself).
    // Only counts once per enemy per dash.
    void HandleHitAvoided(CharacterCombat attacker)
    {
        if (Time.time < dodgeIFramesUntil && perfectDodgedBy.Add(attacker))
            OnPerfectDodge(attacker);
    }

    void Stagger()
    {
        combat.CancelAttack();
        CancelDash();
        if (IsSliding)
            EndSlide();
        if (IsOnWall)
            EndWall();

        isSprinting = false;
        animator.SetTrigger(StaggerTrigger);
    }

    // Sliding

    void TryStartSlide()
    {
        if (!stats.TrySpendStamina(slideStaminaCost))
            return;

        combat.CancelAttack();

        slideDirection = moveDirection.normalized;
        slideStartSpeed = stats.GetMoveSpeed() * sprintMultiplier * slideBoost;
        slideTimer = 0f;
        IsSliding = true;

        // Make the capsule shorter but keep the feet in the same place
        float height = standHeight * slideHeightPercent;
        controller.height = height;
        controller.center = standCenter - Vector3.up * (standHeight - height) * 0.5f;

        transform.rotation = Quaternion.LookRotation(slideDirection);
    }

    void ContinueSlide()
    {
        slideTimer += Time.deltaTime;
        float t = Mathf.Clamp01(slideTimer / slideDuration);

        // Slow down to walking speed over the slide
        MoveWithGravity(slideDirection * Mathf.Lerp(slideStartSpeed, stats.GetMoveSpeed(), t));

        // Can't stand up under something low, so keep sliding
        if (!HasRoomToStand())
            return;

        if (JumpBuffered || t >= 1f || !IsGrounded)
            EndSlide();
    }

    void EndSlide()
    {
        IsSliding = false;
        controller.height = standHeight;
        controller.center = standCenter;
    }

    bool HasRoomToStand()
    {
        float radius = controller.radius;
        Vector3 bottom = transform.position + standCenter - Vector3.up * (standHeight * 0.5f - radius);
        Vector3 top = transform.position + standCenter + Vector3.up * (standHeight * 0.5f - radius);

        int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius * 0.95f, overlapHits, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            if (!overlapHits[i].transform.IsChildOf(transform))
                return false;
        }

        return true;
    }

    // Wall running and climbing

    bool TryStartWall()
    {
        if (IsGrounded || Time.time < nextWallTime || stats.currentStamina < 1f || moveDirection.sqrMagnitude < 0.1f)
            return false;

        Vector3 centre = transform.position + controller.center;

        // Too close to the floor
        if (Physics.Raycast(centre, Vector3.down, controller.height * 0.5f + wallMinHeightAboveGround, wallLayers, QueryTriggerInteraction.Ignore))
            return false;

        Vector3 direction = moveDirection.normalized;
        float reach = controller.radius + wallCheckDistance;

        Vector3 flatVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        if (flatVelocity.magnitude < stats.GetMoveSpeed() * 0.5f)
            flatVelocity = direction * stats.GetMoveSpeed();

        // Wall in front of us, so climb it
        if (FindWall(centre, direction, reach, out RaycastHit hit) && Vector3.Angle(direction, -hit.normal) <= climbAngle)
        {
            if (JustJumpedOff(hit.collider))
                return false;

            BeginWall(hit, true, flatVelocity.magnitude, Vector3.zero);
            return true;
        }

        // Wall to the side, so run along it
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        return TryStartWallRun(centre, right, reach, flatVelocity) || TryStartWallRun(centre, -right, reach, flatVelocity);
    }

    bool TryStartWallRun(Vector3 centre, Vector3 side, float reach, Vector3 flatVelocity)
    {
        if (!FindWall(centre, side, reach, out RaycastHit hit) || JustJumpedOff(hit.collider))
            return false;

        Vector3 along = Vector3.ProjectOnPlane(flatVelocity, hit.normal);
        along.y = 0f;
        if (along.sqrMagnitude < 0.01f)
            return false;

        BeginWall(hit, false, Mathf.Max(along.magnitude, stats.GetMoveSpeed()), along.normalized);
        return true;
    }

    // Stops us grabbing straight back onto the wall we just jumped off
    bool JustJumpedOff(Collider wall)
    {
        return wall == lastWall && Time.time - lastWallJumpTime < 0.5f;
    }

    // Finds the closest wall in that direction
    bool FindWall(Vector3 from, Vector3 direction, float reach, out RaycastHit wallHit)
    {
        wallHit = new RaycastHit();
        bool found = false;

        int hitCount = Physics.RaycastNonAlloc(from, direction, castHits, reach, wallLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = castHits[i];

            // Skip ourselves, other characters and anything that isn't upright enough to be a wall
            if (hit.transform.IsChildOf(transform)) continue;
            if (Mathf.Abs(hit.normal.y) > 0.3f) continue;
            if (found && hit.distance >= wallHit.distance) continue;
            if (hit.collider.GetComponentInParent<CharacterStats>() != null) continue;

            wallHit = hit;
            found = true;
        }

        return found;
    }

    void BeginWall(RaycastHit hit, bool climbing, float speed, Vector3 alongDirection)
    {
        wallNormal = hit.normal;
        currentWall = hit.collider;
        wallTimer = 0f;
        wallSpeed = speed;
        wallDirection = alongDirection;
        IsWallClimbing = climbing;
        IsWallRunning = !climbing;
        isSprinting = false;
        verticalVelocity = climbing ? wallClimbSpeed : wallRunUpSpeed;
    }

    void ContinueWall()
    {
        wallTimer += Time.deltaTime;
        stats.TrySpendStamina(wallStaminaPerSecond * Time.deltaTime, false);

        if (JumpBuffered)
        {
            WallJump();
            return;
        }

        Vector3 centre = transform.position + controller.center;
        bool wallThere = FindWall(centre, -wallNormal, controller.radius + wallCheckDistance, out RaycastHit hit);

        // Climbed past the top of the wall, so hop up over the edge
        if (IsWallClimbing && !wallThere && wallTimer > 0.1f)
        {
            wallKickVelocity = -wallNormal * 2f;
            wallKickUntil = Time.time + 0.25f;
            EndWall();
            verticalVelocity = JumpSpeed * 0.7f;
            airJumpsUsed = 0;
            return;
        }

        float maxTime = (IsWallClimbing ? wallClimbMaxTime : wallRunMaxTime) * stats.WallTimeMultiplier;
        bool letGo = moveDirection.sqrMagnitude < 0.1f;
        bool landed = IsGrounded && wallTimer > 0.1f;

        if (!wallThere || wallTimer >= maxTime || letGo || landed || stats.currentStamina < 1f)
        {
            EndWall();
            return;
        }

        wallNormal = hit.normal;
        Vector3 velocity;

        // Both slow down over time. The small push into the wall keeps us stuck to it.
        if (IsWallClimbing)
        {
            verticalVelocity = Mathf.Lerp(wallClimbSpeed, 0f, wallTimer / maxTime);
            velocity = Vector3.up * verticalVelocity - wallNormal;
            transform.rotation = Quaternion.LookRotation(-wallNormal);
        }
        else
        {
            wallDirection = Vector3.ProjectOnPlane(wallDirection, wallNormal).normalized;
            verticalVelocity = Mathf.Lerp(wallRunUpSpeed, -wallRunEndFallSpeed, wallTimer / maxTime);
            velocity = wallDirection * wallSpeed + Vector3.up * verticalVelocity - wallNormal;
            transform.rotation = Quaternion.LookRotation(wallDirection);
        }

        controller.Move(velocity * Time.deltaTime);
    }

    void WallJump()
    {
        lastWall = currentWall;
        lastWallJumpTime = Time.time;

        // Push away from the wall and keep some of the speed if we were running along it
        Vector3 push = wallNormal * wallJumpPush;
        if (IsWallRunning)
            push += wallDirection * wallSpeed * 0.6f;

        EndWall();
        ConsumeJump();
        verticalVelocity = JumpSpeed;
        airJumpsUsed = 0; //double jump is available again after a wall jump

        transform.rotation = Quaternion.LookRotation(new Vector3(wallNormal.x, 0f, wallNormal.z));

        wallKickVelocity = push;
        wallKickUntil = Time.time + 0.25f;
    }

    void EndWall()
    {
        IsWallRunning = false;
        IsWallClimbing = false;
        currentWall = null;
        nextWallTime = Time.time + 0.2f; //tiny wait before grabbing another wall
    }

    // For abilities that move the player themselves (like the Acrobat's zip and slam)
    protected void BeginExternalControl()
    {
        CancelDash();
        if (IsSliding)
            EndSlide();
        if (IsOnWall)
            EndWall();

        ExternalControl = true;
    }

    protected void EndExternalControl()
    {
        ExternalControl = false;
    }

    protected void MoveExternal(Vector3 velocity)
    {
        controller.Move(velocity * Time.deltaTime);
        verticalVelocity = velocity.y;
    }

    // Lock on

    void UpdateLockOn()
    {
        if (Input.GetKeyDown(KeyCode.Mouse2))
            ToggleLockOn();

        // Drop the lock if the enemy died or got too far away
        if (lockedTarget != null)
        {
            bool tooFar = Vector3.Distance(transform.position, lockedTarget.position) > lockLossRange;

            if (lockedEnemyStats.IsDead || tooFar)
                SetLockTarget(null);
        }
    }

    // The camera and combat need to know the target too, so they're all set here together
    void SetLockTarget(Transform target)
    {
        lockedTarget = target;
        lockedEnemyStats = target != null ? target.GetComponentInParent<CharacterStats>() : null;

        if (cameraController != null)
            cameraController.lockOnTarget = target;

        combat.Target = target;
    }

    // Locks on to the enemy closest to the middle of the screen
    void ToggleLockOn()
    {
        if (lockedTarget != null)
        {
            SetLockTarget(null);
            return;
        }

        Transform closest = null;
        float closestToCentre = Mathf.Infinity;

        foreach (EnemyCombat enemy in FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None))
        {
            if (enemy.GetComponent<CharacterStats>().IsDead)
                continue;

            if (Vector3.Distance(transform.position, enemy.transform.position) > lockOnRange)
                continue;

            Vector3 screenPos = cam.WorldToViewportPoint(enemy.transform.position + Vector3.up);

            // Behind the camera
            if (screenPos.z <= 0)
                continue;

            float distanceToCentre = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), new Vector2(0.5f, 0.5f));

            if (distanceToCentre < closestToCentre)
            {
                closestToCentre = distanceToCentre;
                closest = enemy.lockOnTransform;
            }
        }

        SetLockTarget(closest);
    }

    // Each character overrides these for its own moves

    // True while an ability stops the player moving (like meditating)
    protected virtual bool MovementBlocked => false;

    protected virtual void Attack()
    {
        combat.TryAttack();
    }

    protected virtual void OnDashStarted() { }

    protected virtual void OnPerfectDodge(CharacterCombat attacker) { }

    // Return false if the ability didn't do anything, so it doesn't go on cooldown
    public virtual bool UseAbility(Ability ability) => false;
    public virtual bool UseUltimate(UltimateAbility ultimate) => false;

    // For held abilities (Crystal Sight, Meditate)
    protected virtual bool CanChannel(ChanneledAbility ability) => true;
    protected virtual void UpdateChannel(ChanneledAbility ability, bool active) { }

    // Extra line of text for the debug HUD
    public virtual string DebugStatus => null;
}
