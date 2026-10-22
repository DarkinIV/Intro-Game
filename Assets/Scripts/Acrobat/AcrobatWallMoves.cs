using UnityEngine;

//the acrobat's wall run, wall climb and wall jump
//AcrobatController decides when these run (it calls TryStartWall pluh ContinueWall)
[RequireComponent(typeof(AcrobatController))]
public class AcrobatWallMoves : MonoBehaviour
{
    [Header("Wall Run / Wall Climb")]
    [SerializeField] float wallRunMaxTime = 1.2f;
    [SerializeField] float wallRunUpSpeed = 2f; //rises a little at the start
    [SerializeField] float wallRunEndFallSpeed = 4f;  //and sinks to this by the end
    [SerializeField] float wallClimbMaxTime = 0.8f;
    [SerializeField] float wallClimbSpeed = 6f;   //up speed at the start of a climb, fades to 0
    [SerializeField, Range(0f, 90f)] float climbAngle = 35f; //running within this angle of straight at the wall = climb, tweak it bcz it seems to be bugged OUT sometimes
    [SerializeField] float wallStaminaPerSecond = 15f;
    [SerializeField] float wallMinSpeed = 3f; //must be moving at least this fast to grab a wall
    [SerializeField] float wallCheckDistance = 0.5f;
    [SerializeField] float wallMinHeightAboveGround = 0.8f;
    [SerializeField] float wallJumpPush = 7f;
    [SerializeField] LayerMask wallLayers = ~0;
    public bool IsWallRunning { get; private set; }
    public bool IsWallClimbing { get; private set; }
    public bool IsOnWall
    {
        get { return IsWallRunning || IsWallClimbing; }
    }

    AcrobatController acro;
    PlayerStats stats;
    CharacterController cc;
    float wallTimer;
    float wallSpeed;
    Vector3 wallNormal; //arrow pointing out of the wall, towards us
    Vector3 wallDir; //the way we run along the wall
    Collider currentWall;
    Collider lastWall;
    float lastWallJumpTime = -999f;
    float nextWallTime;
    Vector3 wallKickVelocity;//leftover push after a wall jump, test cuz sometimes u fly off
    float wallKickUntil;

    void Awake()
    {
        acro = GetComponent<AcrobatController>();
        stats = GetComponent<PlayerStats>();
        cc = GetComponent<CharacterController>();
    }
    void Update()
    {
        //leftover push from a wall jump, for 1/4 of a second after it
        if (Time.time < wallKickUntil && !acro.IsBusy)
        {
            cc.Move(wallKickVelocity * Time.deltaTime);
        }
    }
    //called by the controller when nothing else is moving the player, true means wall was grabbed!
    public bool TryStartWall()
    {
        if (acro.IsGroundedNow) return false;
        if (Time.time < nextWallTime) return false;
        if (stats.currentStamina < 1f) return false;
        Vector3 moveDir = acro.MoveDir;
        if (moveDir.sqrMagnitude < 0.1f) return false; //must be holding a direction
        //how fast are we moving along the floor plane
        Vector3 flatVel = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
        if (flatVel.magnitude < wallMinSpeed)
        {
            flatVel = moveDir * Mathf.Max(wallMinSpeed, stats.GetMoveSpeed());
        }
        //are we high enough above the floor?
        Vector3 centre = transform.position + cc.center;
        float floorCheck = cc.height * 0.5f + wallMinHeightAboveGround;
        if (Physics.Raycast(centre, Vector3.down, floorCheck, wallLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        float reach = cc.radius + wallCheckDistance;
        Vector3 moveN = moveDir.normalized;
        RaycastHit hit;

        //climb when: a wall straight ahead, and we are running into it
        if (FindWall(centre, moveN, reach, out hit))
        {
            float angleIntoWall = Vector3.Angle(moveN, -hit.normal);
            if (angleIntoWall <= climbAngle)
            {
                if (JustJumpedOff(hit.collider)) return false;
                BeginWall(hit, true, flatVel.magnitude, Vector3.zero);
                return true;
            }
        }

        //run when a wall on our left or right that we are moving along
        Vector3 right = Vector3.Cross(Vector3.up, moveN); //the direction to our right
        if (TryStartWallRunOnSide(centre, right, reach, flatVel)) return true;
        if (TryStartWallRunOnSide(centre, -right, reach, flatVel)) return true;
        return false;
    }
    bool TryStartWallRunOnSide(Vector3 centre, Vector3 side, float reach, Vector3 flatVel)
    {
        RaycastHit hit;
        if (!FindWall(centre, side, reach, out hit)) return false;
        if (JustJumpedOff(hit.collider)) return false;
        //ProjectOnPlane meaning take our movement and remove the part going into the wall, whats left is along the wall
        Vector3 along = Vector3.ProjectOnPlane(flatVel, hit.normal);
        along.y = 0f;
        if (along.sqrMagnitude < 0.01f) return false; //running straight at it and not along it
        float speed = Mathf.Max(along.magnitude, stats.GetMoveSpeed());
        BeginWall(hit, false, speed, along.normalized);
        return true;
    }

    //cant grab the wall we just jumped off for half a second (so we dont stick straight back on)
    bool JustJumpedOff(Collider wall)
    {
        return wall == lastWall && Time.time - lastWallJumpTime < 0.5f;
    }
    bool FindWall(Vector3 from, Vector3 dir, float reach, out RaycastHit wallHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(from, dir, reach, wallLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; //not us
            if (hit.collider.GetComponentInParent<CharacterStats>() != null) continue; //enemies are not walls
            if (Mathf.Abs(hit.normal.y) > 0.3f) continue;  //must be upright
            wallHit = hit;
            return true;
        }
        wallHit = new RaycastHit();
        return false;
    }
    void BeginWall(RaycastHit hit, bool climbing, float speed, Vector3 alongDir)
    {
        wallNormal = hit.normal;
        currentWall = hit.collider;
        wallTimer = 0f;
        wallSpeed = speed;
        IsWallClimbing = climbing;
        IsWallRunning = !climbing;
        wallDir = alongDir;
        if (climbing) acro.VerticalSpeed = wallClimbSpeed;
        else acro.VerticalSpeed = wallRunUpSpeed;
    }
    public void ContinueWall()
    {
        wallTimer += Time.deltaTime;
        stats.TrySpendStamina(wallStaminaPerSecond * Time.deltaTime, false);

        if (acro.JumpBuffered())
        {
            acro.ConsumeJump();
            WallJump();
            return;
        }

        //is the wall still there, hopefully if ur trynna do this
        Vector3 centre = transform.position + cc.center;
        float reach = cc.radius + wallCheckDistance;
        RaycastHit hit;
        bool wallThere = FindWall(centre, -wallNormal, reach, out hit);

        float maxTime = wallRunMaxTime;
        if (IsWallClimbing) maxTime = wallClimbMaxTime;

        bool letGo = acro.MoveDir.sqrMagnitude < 0.1f;
        bool outOfStamina = stats.currentStamina < 1f;
        bool landed = acro.IsGroundedNow && wallTimer > 0.1f;

        if (IsWallClimbing && !wallThere && wallTimer > 0.1f)
        {
            //reached the top of the wall: a little hop up and over the edge (mantle)
            EndWall();
            acro.VerticalSpeed = acro.JumpSpeed * 0.7f;
            cc.Move((-wallNormal * 2f + Vector3.up * acro.VerticalSpeed) * Time.deltaTime);
            acro.ResetAirJumps();
            return;
        }
        if (!wallThere || wallTimer >= maxTime || letGo || outOfStamina || landed)
        {
            EndWall(); //fall off
            return;
        }
        wallNormal = hit.normal;// follow natural curves, test test test

        Vector3 velocity;
        if (IsWallClimbing)
        {
            acro.VerticalSpeed = Mathf.Lerp(wallClimbSpeed, 0f, wallTimer / wallClimbMaxTime);//up speed fades to 0
            velocity = Vector3.up * acro.VerticalSpeed - wallNormal * 1f;  //thewallNormal keeps us stuck to it
            transform.rotation = Quaternion.LookRotation(-wallNormal);
        }
        else
        {
            wallDir = Vector3.ProjectOnPlane(wallDir, wallNormal).normalized;
            acro.VerticalSpeed = Mathf.Lerp(wallRunUpSpeed, -wallRunEndFallSpeed, wallTimer / wallRunMaxTime); //rises then sinks
            velocity = wallDir * wallSpeed + Vector3.up * acro.VerticalSpeed - wallNormal * 1f;
            transform.rotation = Quaternion.LookRotation(wallDir);
        }
        cc.Move(velocity * Time.deltaTime);
    }

    void WallJump()
    {
        lastWall = currentWall;
        lastWallJumpTime = Time.time;

        Vector3 away = wallNormal * wallJumpPush;
        Vector3 along = Vector3.zero;
        if (IsWallRunning) along = wallDir * wallSpeed * 0.6f; //keep some of the running speed

        EndWall();
        acro.VerticalSpeed = acro.JumpSpeed;
        acro.ResetAirJumps(); //a wall jump gives the double jump back
        cc.Move((away + along + Vector3.up * acro.VerticalSpeed) * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(new Vector3(wallNormal.x, 0f, wallNormal.z));
        //keep some sideways momentum for the next frames
        wallKickVelocity = away + along;
        wallKickUntil = Time.time + 0.25f;
    }
    public void EndWall()
    {
        IsWallRunning = false;
        IsWallClimbing = false;
        currentWall = null;
        nextWallTime = Time.time + 0.2f; //tiny wait before grabbing another wall cant make it too cracked
    }
}
