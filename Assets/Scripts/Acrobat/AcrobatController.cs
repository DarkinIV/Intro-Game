using UnityEngine;

// The Acrobat. She's better at moving around because of her Agility rank (S) in her character data.
// F: whip pull. Attacking in the air does a plunge slam.
// Perfect dodges charge her ultimate (R: Untouchable), which makes her dodge enemy attacks by herself.
[RequireComponent(typeof(AcrobatCombat))]
public class AcrobatController : PlayerController
{
    [SerializeField] int perfectDodgeCharge = 60;

    [Header("Plunge Slam")]
    [SerializeField] float slamSpeed = 28f;
    [SerializeField] float slamRecovery = 0.3f;
    [SerializeField] float slamMaxTime = 2f;

    [Header("Untouchable")]
    [SerializeField] float autoDodgeRange = 4f;

    // Moves where this script takes over from the normal movement
    enum WhipMove { None, Zip, Slam, SlamRecover }

    AcrobatCombat acrobatCombat;
    WhipMove whipMove;
    float whipMoveTimer;
    Transform zipTarget;
    WhipPullData zipPull;
    float untouchableUntil;

    public bool UntouchableActive => Time.time < untouchableUntil;

    protected override void Awake()
    {
        base.Awake();
        acrobatCombat = GetComponent<AcrobatCombat>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnemyCombat.OnAnyEnemyAttack += HandleEnemyAttack;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EnemyCombat.OnAnyEnemyAttack -= HandleEnemyAttack;
        whipMove = WhipMove.None;
    }

    protected override void Update()
    {
        base.Update();

        if (whipMove == WhipMove.Zip)
            ContinueZip();
        else if (whipMove == WhipMove.Slam)
            ContinueSlam();
        else if (whipMove == WhipMove.SlamRecover)
            ContinueSlamRecover();
    }

    // Whip pull (F)
    public override bool UseAbility(Ability ability)
    {
        if (!(ability.Data is WhipPullData pull) || whipMove != WhipMove.None)
            return false;

        if (!acrobatCombat.TryWhipPull(pull, LockTarget, out Transform heavyTarget))
            return false;

        // Heavy enemies can't be pulled, so we fly over to them instead
        if (heavyTarget != null)
        {
            zipTarget = heavyTarget;
            zipPull = pull;
            StartWhipMove(WhipMove.Zip);
        }

        return true;
    }

    protected override void Attack()
    {
        // In the air the attack becomes a plunge slam (if we can pay for it)
        if (!IsGrounded && whipMove == WhipMove.None)
        {
            if (acrobatCombat.TryPaySlam())
                StartWhipMove(WhipMove.Slam);
        }
        else
        {
            base.Attack();
        }
    }

    void ContinueZip()
    {
        whipMoveTimer += Time.deltaTime;

        if (zipTarget == null || whipMoveTimer > zipPull.zipMaxTime)
        {
            EndWhipMove();
            return;
        }

        Vector3 toTarget = zipTarget.position + Vector3.up * 0.5f - (transform.position + controller.center);
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);

        if (flat.magnitude <= zipPull.zipStopDistance)
        {
            EndWhipMove();
            return;
        }

        MoveExternal(toTarget.normalized * zipPull.zipSpeed);
        transform.rotation = Quaternion.LookRotation(flat);
    }

    void ContinueSlam()
    {
        whipMoveTimer += Time.deltaTime;
        MoveExternal(Vector3.down * slamSpeed);

        if (controller.isGrounded || whipMoveTimer > slamMaxTime)
        {
            Vector3 feet = transform.position + controller.center - Vector3.up * (controller.height * 0.5f);
            acrobatCombat.SlamImpact(feet);

            whipMove = WhipMove.SlamRecover;
            whipMoveTimer = 0f;
        }
    }

    // Stay on the ground for a moment after landing
    void ContinueSlamRecover()
    {
        whipMoveTimer += Time.deltaTime;
        MoveExternal(Vector3.down * 2f);

        if (whipMoveTimer >= slamRecovery)
            EndWhipMove();
    }

    void StartWhipMove(WhipMove move)
    {
        BeginExternalControl();
        whipMove = move;
        whipMoveTimer = 0f;
    }

    void EndWhipMove()
    {
        whipMove = WhipMove.None;
        zipTarget = null;
        EndExternalControl();
    }

    protected override void OnPerfectDodge(CharacterCombat attacker)
    {
        // Dodges done by the ultimate itself don't charge it
        if (!UntouchableActive)
            stats.AddUltimateCharge(perfectDodgeCharge);
    }

    // Untouchable (R)
    public override bool UseUltimate(UltimateAbility ultimate)
    {
        if (!(ultimate.Data is AutoDodgeUltimateData untouchable))
            return false;

        untouchableUntil = Time.time + untouchable.duration;
        stats.GrantInvulnerability(untouchable.duration);
        return true;
    }

    // While Untouchable is on, sidestep any nearby enemy that starts an attack
    void HandleEnemyAttack(EnemyCombat enemy)
    {
        if (!UntouchableActive || ExternalControl)
            return;

        Vector3 toEnemy = enemy.transform.position - transform.position;
        toEnemy.y = 0f;

        if (toEnemy.magnitude > autoDodgeRange)
            return;

        // Dodge to whichever side the player is already moving towards
        Vector3 side = Vector3.Cross(Vector3.up, toEnemy.normalized);
        if (Vector3.Dot(side, MoveDirection) < 0f)
            side = -side;

        StartDash(side);
    }

    public override string DebugStatus
    {
        get
        {
            if (!UntouchableActive)
                return null;

            return "UNTOUCHABLE " + (untouchableUntil - Time.time).ToString("F1") + "s";
        }
    }
}
