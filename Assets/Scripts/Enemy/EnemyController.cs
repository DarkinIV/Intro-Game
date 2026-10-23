// AI use: Claude added stun and simplified the rest
using UnityEngine;
using UnityEngine.AI;

// Simple enemy AI: stand around, chase the player when they get close, attack when in range.
// Can be stunned (whip pull, parry) and moved around by the whip.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(EnemyCombat))]
public class EnemyController : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Stunned }

    public State currentState;   // just so you can see what it's doing in the inspector

    [Header("Detection")]
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float loseTargetRadius = 13f;

    [Header("Movement")]
    [SerializeField] float stoppingDistance = 1.5f;

    [Header("Attack")]
    [SerializeField] float attackRange = 1.8f;
    [SerializeField] float rotationSpeed = 10f;

    [Header("Weight")]
    [Tooltip("Heavy enemies can't be pulled by the whip, the Acrobat flies to them instead")]
    [SerializeField] bool isHeavy;

    [Header("UI")]
    [Tooltip("Where the health bar and stunned icon go, above the head")]
    [SerializeField] Transform uiAnchor;

    NavMeshAgent agent;
    CharacterStats stats;
    EnemyCombat combat;
    Transform target;
    float stunnedUntil;

    public bool IsHeavy => isHeavy;
    public bool IsStunned => Time.time < stunnedUntil;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<CharacterStats>();
        combat = GetComponent<EnemyCombat>();
    }

    void Start()
    {
        agent.speed = stats.GetMoveSpeed();
        agent.stoppingDistance = stoppingDistance;

        // We turn the enemy ourselves so it can face the player while attacking
        agent.updateRotation = false;

        stats.OnHealthReachedZero += Die;

        EnemyUIManager.instance.Create(this, uiAnchor);
    }

    void Update()
    {
        if (stats.IsDead)
            return;

        if (IsStunned)
        {
            currentState = State.Stunned;
            return;
        }

        UpdateTarget();

        if (target == null)
        {
            Idle();
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= attackRange)
            Attack(toTarget);
        else
            Chase(toTarget);
    }

    // Notice the player when they come close, and give up once they're far enough away
    void UpdateTarget()
    {
        if (Player.instance == null)
        {
            target = null;
            return;
        }

        Transform player = Player.instance.transform;
        float distance = Vector3.Distance(transform.position, player.position);

        if (target == null && distance <= detectionRadius)
            target = player;
        else if (target != null && distance > loseTargetRadius)
            target = null;
    }

    void Idle()
    {
        currentState = State.Idle;
        StopMoving();
    }

    void Chase(Vector3 toTarget)
    {
        currentState = State.Chase;

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.SetDestination(target.position);

        TurnTowards(toTarget);
    }

    void Attack(Vector3 toTarget)
    {
        currentState = State.Attack;
        StopMoving();
        TurnTowards(toTarget);

        combat.Target = target;
        combat.TryAttack();
    }

    void StopMoving()
    {
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    void TurnTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }

    // Stops the enemy and cancels its attack (whip pull, parry)
    public void Stun(float duration)
    {
        if (stats.IsDead)
            return;

        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
        currentState = State.Stunned;
        combat.CancelAttack();
        StopMoving();
    }

    // Moves the enemy along the NavMesh so it doesn't get pulled through walls (whip pull)
    public void Displace(Vector3 offset)
    {
        if (agent.enabled && agent.isOnNavMesh)
            agent.Move(offset);
        else
            transform.position += offset;
    }

    void Die()
    {
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
