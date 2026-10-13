using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterStats))]
public class EnemyController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Chase,
        Dead
    }
    public State CurrentState;

    [Header("Detection")]
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float loseTargetRadius = 13f;

    [Header("Movement")]
    [SerializeField] float stoppingDistance = 1f;

    NavMeshAgent agent;
    CharacterStats stats;
    public Transform target;

    public bool IsMoving => agent.enabled && agent.velocity.sqrMagnitude > 0.01f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<CharacterStats>();
    }

    void Start()
    {
        agent.speed = stats.GetMoveSpeed();
        stats.OnHealthReachedZero += Die;
    }

    void OnDestroy()
    {
        stats.OnHealthReachedZero -= Die;
    }

    void Update()
    {
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

        Chase();
    }

    void UpdateTarget()
    {
        if (Player.instance == null)
        {
            target = null;
            return;
        }

        Transform player = Player.instance.transform;
        float distance = Vector3.Distance(transform.position, player.position);

        // Detect player
        if (target == null && distance <= detectionRadius)
        {
            target = player;
        }

        // Lose player
        if (target != null && distance > loseTargetRadius)
        {
            target = null;
        }
    }

    void Idle()
    {
        CurrentState = State.Idle;

        if (agent.isOnNavMesh)
            agent.isStopped = true;
    }

    void Chase()
    {
        CurrentState = State.Chase;

        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(target.position);
    }

    void Die()
    {
        if (CurrentState == State.Dead)
            return;

        CurrentState = State.Dead;

        if (agent.isOnNavMesh)
            agent.isStopped = true;

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);
    }
}