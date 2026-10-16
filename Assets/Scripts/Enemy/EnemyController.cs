
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(EnemyCombat))]
public class EnemyController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    public State CurrentState { get; private set; }

    [Header("Detection")]
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float loseTargetRadius = 13f;

    [Header("Movement")]
    [SerializeField] float stoppingDistance = 1.5f;

    [Header("Attack")]
    [SerializeField] float attackRange = 1.8f;
    [SerializeField] float rotationSpeed = 10f;

    NavMeshAgent agent;
    CharacterStats stats;
    EnemyCombat combat;

    public Transform target;

    public bool IsMoving =>
        agent != null &&
        agent.enabled &&
        agent.isOnNavMesh &&
        agent.velocity.sqrMagnitude > 0.01f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<CharacterStats>();
        combat = GetComponent<EnemyCombat>();
    }

    void Start()
    {
        agent.speed = stats.GetMoveSpeed();

        // Disable automatic rotation so we can
        // control facing during combat.
        agent.updateRotation = false;

        stats.OnHealthReachedZero += Die;
    }

    void OnDestroy()
    {
        if (stats != null)
            stats.OnHealthReachedZero -= Die;
    }

    void Update()
    {
        if (CurrentState == State.Dead)
            return;

        if (stats.IsDead)
        {
            Die();
            return;
        }

        UpdateTarget();

        if (target == null)
        {
            Idle();
            return;
        }

        // Horizontal distance between enemy and player.
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        // Attack when within range.
        if (distance <= attackRange)
        {
            Attack(direction);
        }
        else
        {
            Chase(direction);
        }
    }

    void UpdateTarget()
    {
        if (Player.instance == null)
        {
            target = null;
            return;
        }

        Transform player = Player.instance.transform;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (target == null && distance <= detectionRadius)
        {
            target = player;
        }

        if (target != null && distance > loseTargetRadius)
        {
            target = null;
        }
    }

    void Idle()
    {
        CurrentState = State.Idle;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    void Chase(Vector3 direction)
    {
        CurrentState = State.Chase;

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(target.position);

        RotateTowards(direction);
    }

    void Attack(Vector3 direction)
    {
        CurrentState = State.Attack;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Turn toward player before attacking.
        RotateTowards(direction);

        if (combat != null)
        {
            combat.Target = target;
            combat.TryAttack();
        }
    }

    void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion desiredRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    void Die()
    {
        if (CurrentState == State.Dead)
            return;

        CurrentState = State.Dead;
        target = null;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            loseTargetRadius
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}
