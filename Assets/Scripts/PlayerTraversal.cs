using UnityEngine;

//the acrobat's movement tricks, goes on the player
//SLIDE: ctrl while sprinting = drop low and shoot forward, fits under things, jump out of it to keep the speed
//WALL RUN: in the air, moving along a wall = run on the wall for a bit, space = jump off it
//(double jump lives in PlayerMovement bcz it shares the jump code)
[RequireComponent(typeof(PlayerMovement))]
public class PlayerTraversal : MonoBehaviour
{
    [Header("Slide")]
    [SerializeField] private bool canSlide = false; //the class sheet switches these on
    [SerializeField] private float slideStaminaCost = 15f;
    [SerializeField] private float slideDuration = 0.6f;
    [SerializeField] private float slideBoost = 1.15f; //starts 15% faster than u were going
    [SerializeField, Range(0.3f, 0.9f)] private float slideHeight = 0.5f; //how tall u are while sliding (0.5 = half)

    [Header("Wall Run")]
    [SerializeField] private bool canWallRun = false;
    [SerializeField] private float wallRunStaminaPerSecond = 15f;
    [SerializeField] private float wallRunMaxTime = 1.2f;
    [SerializeField] private float wallRunMinSpeed = 4f; //need to be moving at least this fast to grab a wall
    [SerializeField] private float wallRunUpSpeed = 2f; //starts moving up a bit...
    [SerializeField] private float wallRunEndFallSpeed = 4f; //...and slowly slides down to this by the end
    [SerializeField] private float wallCheckDistance = 0.6f; //how far from our side a wall can be to grab it
    [SerializeField] private float minHeightAboveGround = 1f; //cant wall run right next to the floor
    [SerializeField] private float wallJumpPush = 8f; //how hard the wall jump pushes u away from the wall
    [SerializeField] private LayerMask wallLayers = ~0;

    public bool IsSliding { get; private set; }
    public bool IsWallRunning { get; private set; }

    private PlayerMovement movement;
    private PlayerDodge dodge;
    private PlayerWhip whip;
    private Stamina stamina;
    private Rigidbody rb;
    private Collider col; //any collider, for the wall checks
    private BoxCollider box; //the slide needs a box collider to make it shorter
    private Vector3 standingSize, standingCenter; //the collider's normal shape, to put it back after sliding

    private float timer;
    private Vector3 slideDirection;
    private float slideStartSpeed;

    private Vector3 wallNormal; //the direction pointing OUT of the wall (towards us)
    private Vector3 wallRunDirection; //along the wall, the way we are running
    private float wallRunSpeed;
    private Collider currentWall;
    private Collider lastWall; //the wall we just jumped off, cant grab it again straight away
    private float lastWallJumpTime = -999f;
    private float nextWallRunTime;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        dodge = GetComponent<PlayerDodge>();
        whip = GetComponent<PlayerWhip>();
        stamina = GetComponent<Stamina>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        box = GetComponent<BoxCollider>();
        if (box != null)
        {
            standingSize = box.size;
            standingCenter = box.center;
        }
    }

    //the class script calls this to switch tricks on/off and put in the numbers
    public void Setup(ClassData data)
    {
        canSlide = data.canSlide;
        slideStaminaCost = data.slideStaminaCost;
        canWallRun = data.canWallRun;
        wallRunStaminaPerSecond = data.wallRunStaminaPerSecond;
        if (!canSlide && IsSliding) EndSlide();
        if (!canWallRun && IsWallRunning) EndWallRun();
    }

    private void OnDisable() => CancelAll();

    //stop sliding / wall running straight away (switched off, or the player died)
    public void CancelAll()
    {
        if (IsSliding) EndSlide();
        if (IsWallRunning) EndWallRun();
    }

    private void FixedUpdate()
    {
        if (!movement.enabled) return; //movement is off while dead

        if (IsSliding) ContinueSlide();
        else if (IsWallRunning) ContinueWallRun();
        else TryStartWallRun(); //wall runs start by themselves, slides start from the ctrl key (see PlayerDodge)
    }

    //=============== SLIDE ===============

    //PlayerDodge calls this when ctrl is pressed while sprinting. true = we slid (so dont dodge)
    public bool TryStartSlide()
    {
        if (!canSlide || box == null || IsSliding || IsWallRunning) return false;
        if (!movement.IsGrounded || movement.ControlsLocked) return false;
        if (stamina != null && !stamina.TrySpend(slideStaminaCost)) return false;

        //slide the way we are already moving, a bit faster than we were going
        Vector3 flat = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        slideDirection = flat.sqrMagnitude > 0.01f ? flat.normalized : transform.forward;
        slideStartSpeed = Mathf.Max(flat.magnitude, movement.SprintSpeed) * slideBoost;

        //make the collider shorter, keeping the bottom where it is (so we dont float)
        //size and center are LOCAL (before the player's scale), same as when we set up the box collider
        box.size = new Vector3(standingSize.x, standingSize.y * slideHeight, standingSize.z);
        box.center = standingCenter - new Vector3(0f, standingSize.y * (1f - slideHeight) * 0.5f, 0f);

        IsSliding = true;
        timer = 0f;
        movement.ControlsLocked = true;
        return true;
    }

    private void ContinueSlide()
    {
        timer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(timer / slideDuration); //0 at the start, 1 at the end

        //slow down from the start speed to walking speed over the slide
        float speed = Mathf.Lerp(slideStartSpeed, movement.WalkSpeed, t);
        rb.velocity = new Vector3(slideDirection.x * speed, rb.velocity.y, slideDirection.z * speed);

        bool roomToStand = HasRoomToStand();

        //jump out of the slide: we just end the slide and leave the jump press alone,
        //so PlayerMovement does a normal jump straight after and keeps all this speed (slide jump)
        if (movement.JumpBuffered && roomToStand)
        {
            EndSlide();
            return;
        }

        //slid off an edge, or the slide is over
        if ((!movement.IsGrounded || t >= 1f) && roomToStand)
        {
            EndSlide();
        }
        //if there is NO room to stand (still under something) we keep sliding slowly until there is
    }

    private void EndSlide()
    {
        IsSliding = false;
        if (box != null)
        {
            box.size = standingSize; //back to full height
            box.center = standingCenter;
        }
        movement.ControlsLocked = false;
    }

    //is there space above us to stand up? checks the space our full height would take
    private bool HasRoomToStand()
    {
        Bounds b = box.bounds; //the short collider rn
        float standingHeight = standingSize.y * transform.lossyScale.y; //lossyScale = the real scale in the world
        float spaceBottom = b.max.y + 0.05f; //from just above our head now...
        float spaceTop = b.min.y + standingHeight; //...to where our head would be standing
        if (spaceTop <= spaceBottom) return true;

        Vector3 center = new Vector3(b.center.x, (spaceBottom + spaceTop) * 0.5f, b.center.z);
        Vector3 half = new Vector3(b.extents.x * 0.9f, (spaceTop - spaceBottom) * 0.5f, b.extents.z * 0.9f);
        Collider[] hits = Physics.OverlapBox(center, half, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (!hit.transform.IsChildOf(transform)) return false; //something that isnt us (the nose) is there
        }
        return true;
    }

    //=============== WALL RUN ===============

    private void TryStartWallRun()
    {
        if (!canWallRun || movement.IsGrounded || movement.ControlsLocked || !movement.enabled) return;
        if (Time.time < nextWallRunTime) return;
        if (dodge != null && dodge.IsDodging) return;
        if (whip != null && whip.IsBusy) return;
        if (stamina != null && (stamina.Current <= 0f || stamina.IsExhausted)) return;

        //must be moving fast enough AND holding a direction (letting go of the keys = dont grab walls)
        Vector3 flat = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flat.magnitude < wallRunMinSpeed) return;
        if (movement.GetMoveDirection().sqrMagnitude < 0.1f) return;

        //high enough above the floor? shoot a ray down
        Bounds b = col.bounds;
        if (Physics.Raycast(b.center, Vector3.down, b.extents.y + minHeightAboveGround, wallLayers, QueryTriggerInteraction.Ignore))
            return;

        if (!FindWall(flat.normalized, out RaycastHit hit)) return;
        if (hit.collider == lastWall && Time.time - lastWallJumpTime < 0.5f) return; //just jumped off this one

        //ProjectOnPlane = take our movement and remove the part going INTO the wall, whats left is along the wall
        Vector3 along = Vector3.ProjectOnPlane(flat, hit.normal);
        along.y = 0f;
        if (along.sqrMagnitude < 0.01f) return; //running straight at the wall, not along it

        wallNormal = hit.normal;
        wallRunDirection = along.normalized;
        wallRunSpeed = Mathf.Max(flat.magnitude, movement.WalkSpeed);
        currentWall = hit.collider;

        IsWallRunning = true;
        timer = 0f;
        movement.ControlsLocked = true;
        movement.SuppressGravity = true; //we do the up/down ourselves
    }

    //look to our left and right (compared to the way we move) for a wall
    private bool FindWall(Vector3 moveDir, out RaycastHit wallHit)
    {
        Bounds b = col.bounds;
        float reach = Mathf.Max(b.extents.x, b.extents.z) + wallCheckDistance; //from our middle to past our side
        Vector3 right = Vector3.Cross(Vector3.up, moveDir); //Cross of up and forward = the right side

        foreach (Vector3 side in new[] { right, -right })
        {
            if (Physics.Raycast(b.center, side, out RaycastHit hit, reach, wallLayers, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(transform)
                && hit.collider.GetComponentInParent<Health>() == null //enemies arent walls
                && Mathf.Abs(hit.normal.y) < 0.3f) //normal.y near 0 = the surface is upright, a wall (not a floor or ramp)
            {
                wallHit = hit;
                return true;
            }
        }
        wallHit = default;
        return false;
    }

    private void ContinueWallRun()
    {
        timer += Time.fixedDeltaTime;
        if (stamina != null) stamina.Drain(wallRunStaminaPerSecond * Time.fixedDeltaTime);

        //wall jump!
        if (movement.ConsumeJumpPress())
        {
            WallJump();
            return;
        }

        //is the wall still there? shoot a ray back into it
        Bounds b = col.bounds;
        float reach = Mathf.Max(b.extents.x, b.extents.z) + wallCheckDistance;
        bool wallStillThere = Physics.Raycast(b.center, -wallNormal, out RaycastHit hit, reach, wallLayers, QueryTriggerInteraction.Ignore)
            && Mathf.Abs(hit.normal.y) < 0.3f;

        bool outOfStamina = stamina != null && stamina.Current <= 0f;
        bool letGo = movement.GetMoveDirection().sqrMagnitude < 0.1f; //stopped holding a direction

        if (!wallStillThere || timer >= wallRunMaxTime || outOfStamina || letGo || movement.IsGrounded)
        {
            EndWallRun(); //fall off
            return;
        }

        //follow the wall if it curves a bit
        wallNormal = hit.normal;
        wallRunDirection = Vector3.ProjectOnPlane(wallRunDirection, wallNormal).normalized;

        //up speed goes from wallRunUpSpeed at the start to -wallRunEndFallSpeed at the end (a slow arc)
        float upSpeed = Mathf.Lerp(wallRunUpSpeed, -wallRunEndFallSpeed, timer / wallRunMaxTime);
        //a little push into the wall keeps us stuck to it
        rb.velocity = wallRunDirection * wallRunSpeed + Vector3.up * upSpeed - wallNormal * 1f;

        rb.MoveRotation(Quaternion.LookRotation(wallRunDirection)); //face the way we run
    }

    private void WallJump()
    {
        lastWall = currentWall;
        lastWallJumpTime = Time.time;
        EndWallRun();

        //away from the wall + up + keep some of the running speed = a jump out at an angle
        rb.velocity = wallNormal * wallJumpPush + wallRunDirection * wallRunSpeed * 0.6f + Vector3.up * movement.JumpSpeed;
        movement.ResetAirJumps(); //double jump is available again after a wall jump
    }

    private void EndWallRun()
    {
        IsWallRunning = false;
        currentWall = null;
        movement.ControlsLocked = false;
        movement.SuppressGravity = false;
        nextWallRunTime = Time.time + 0.2f; //tiny wait before grabbing another wall
    }
}
