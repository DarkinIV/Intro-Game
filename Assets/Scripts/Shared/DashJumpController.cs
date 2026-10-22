using System.Reflection;
using UnityEngine;
    //Shared by FT and knight :  like Asim's PlayerController with two main chngs
    //Ctrl is now dash, same dash and everything (its diff for acrobat as it was taken directly from my woorking version
      //Space = jump,wasnt here before but i figure it would be useful
    //Everything else is the same as his WASD move, shift sprint, left click attack, E interact.
    //lock on is middle click only and lives in the shared LockOn script (it calls that targeting), Q was dropped
    //this is a subclass of PlayerController with its own Update, so unity runs this update instead of Asim's
    //(PlayerController.cs is not changed). that Start still runs and sets everything up.  the previous dash is a private method, so it's called through "reflection" (asking C# for a private method by name)
    //If he ever renames those private members, prints red error feel free to test it but i suffered so pls refrain urself 
    //not changing it right now, need approval and seemed a bit too complicated to mess with right now given the deadlines

public class DashJumpController : PlayerController
{
    [Header("Keys")]
    [SerializeField] KeyCode dashKey = KeyCode.LeftControl;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [Header("Jump")]
    [SerializeField] float jumpHeight = 1.4f; //units
    [SerializeField] float gravity = 20f;  //same as the base controller's gravity
    [SerializeField] float coyoteTime = 0.1f;//can still jump this long after walking off an edge

    //private members we need, looking at them once.
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo DashMethod = typeof(PlayerController).GetMethod("Dash", Private);
    static readonly FieldInfo MoveDirField = typeof(PlayerController).GetField("moveDirection", Private);
    static readonly FieldInfo LastMoveDirField = typeof(PlayerController).GetField("lastMoveDirection", Private);
    static readonly FieldInfo SprintMultField = typeof(PlayerController).GetField("sprintMultiplier", Private);
    static readonly FieldInfo SprintCostField = typeof(PlayerController).GetField("sprintStaminaPerSecond", Private);

    CharacterController cc;
    PlayerStats ftStats;
    PlayerCombat ftCombat;
    PlayerInteract interact;
    LockOn lockOn;   //the shared lock on (middle click), can be missing
    Camera cam;
    Vector3 moveDir;
    Vector3 lastMoveDir = Vector3.forward;
    float vy;                              //up and down speed
    float lastGroundedTime = -999f;
    bool sprinting;
    //the locked on enemy, null if not locked (read by the fortune teller / knight scripts)
    public Transform LockTarget
    {
        get
        {
            if (lockOn == null) return null;
            return lockOn.LockTarget;
        }
    }
    void Awake()
    {
        cc = GetComponent<CharacterController>();
        ftStats = GetComponent<PlayerStats>();
        ftCombat = GetComponent<PlayerCombat>();
        interact = GetComponent<PlayerInteract>();
        lockOn = GetComponent<LockOn>();

        //if any of the lookups came back empty, one of the private names changed: say which one, added cuz i now know a new C# concept, never thought id see the day!!!
        if (DashMethod == null || MoveDirField == null || LastMoveDirField == null || SprintMultField == null || SprintCostField == null)
        {
            Debug.LogError("DashJumpController: a private member of PlayerController was renamed "
                + "(Dash=" + (DashMethod != null)
                + ", moveDirection=" + (MoveDirField != null)
                + ", lastMoveDirection=" + (LastMoveDirField != null)
                + ", sprintMultiplier=" + (SprintMultField != null)
                + ", sprintStaminaPerSecond=" + (SprintCostField != null) + ").");
        }
    }

    void Update()
    {
        if (ftStats == null || ftStats.IsDead) return;

        //the camera spawns with the scene, so find it the first time we need it
        if (cam == null) cam = Camera.main;
        if (!IsDashing) Move(); //during that dash, that dash moves us (prev dash)
        if (Input.GetKeyDown(dashKey)) Dash();
        //lock on is handled by the LockOn component, it also tells the camera and combat about the target
        if (Input.GetMouseButtonDown(0) && !IsDashing && ftCombat != null) ftCombat.TryAttack();
        if (Input.GetKeyDown(KeyCode.E) && !IsDashing && interact != null) interact.TryInteract();
    }

    //same as Move, plus the jump
    void Move()
    {
        //forward and right come from the camera, flattened so we dont walk into the floor
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (cam != null)
        {
            forward = cam.transform.forward;
            right = cam.transform.right;
        }
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        moveDir = forward * Input.GetAxisRaw("Vertical") + right * Input.GetAxisRaw("Horizontal");
        if (moveDir.magnitude > 1f) moveDir.Normalize();
        if (moveDir.magnitude > 0.1f) lastMoveDir = moveDir;

        //prev dash reads these two private fields to decide which way to go
        if (MoveDirField != null) MoveDirField.SetValue(this, moveDir);
        if (LastMoveDirField != null) LastMoveDirField.SetValue(this, lastMoveDir);
        float speed = ftStats.GetMoveSpeed();
        // sprint follows identical rules to the previous
        if (Input.GetKeyDown(KeyCode.LeftShift) && moveDir.magnitude > 0.1f) sprinting = true;
        if (Input.GetKeyUp(KeyCode.LeftShift) || moveDir.magnitude <= 0.1f) sprinting = false;
        if (sprinting)
        {
            float costPerSecond = 15f;
            if (SprintCostField != null) costPerSecond = (float)SprintCostField.GetValue(this);

            float multiplier = 1.5f;
            if (SprintMultField != null) multiplier = (float)SprintMultField.GetValue(this);

            if (ftStats.TrySpendStamina(costPerSecond * Time.deltaTime, false))
            {
                speed = speed * multiplier;
            }
            else
            {
                sprinting = false; //out of stamina
            }
        }

        //facing is locked on and not sprinting then u  face the enemy, otherwise face where we move (same as before)
        float turn = ftStats.turnSpeed.Value * Time.deltaTime;
        Transform lockedTarget = LockTarget;
        if (lockedTarget != null && !sprinting)
        {
            Vector3 dir = lockedTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turn);
            }
        }
        else if (moveDir.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), turn);
        }

        // gravity + Jjumo
        if (cc.isGrounded)
        {
            lastGroundedTime = Time.time;
            if (vy < 0f) vy = -2f;//small push down keeps us glued to slopes
        }
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (Input.GetKeyDown(jumpKey) && canJump)
        {
            vy = Mathf.Sqrt(2f * gravity * jumpHeight);           //physics: speed needed to reach jumpHeight(as previous veresion see keisi-combat)
            lastGroundedTime = -999f;                            //one jump per press
        }

        if (!cc.isGrounded || vy > 0f) vy -= gravity * Time.deltaTime;

        Vector3 movement = moveDir * speed;
        movement.y = vy;
        CollisionFlags flags = cc.Move(movement * Time.deltaTime);

        bool hitCeiling = (flags & CollisionFlags.Above) != 0;
        if (hitCeiling && vy > 0f) vy = 0f; // bumped the ceiling
    }

    //calls previous (asim) dash
    void Dash()
    {
        if (DashMethod == null) return;
        vy = 0f;    //a dash in mid air shouldnt keep the jump's up speed
        DashMethod.Invoke(this, null);   //run his private Dash() on this object
    }
}
