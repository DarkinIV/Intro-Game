using UnityEngine;
using UnityEngine.AI; //for NavMeshAgent, comes from the AI Navigation package

//enemy brain, a state machine: the enemy is always in exactly ONE state and rules decide when it switches
//Patrol -> sees player -> Chase -> close enough -> Attack
//Chase -> lost the player -> Search (go to where we last saw them, look around) -> Patrol
//anything -> pulled by the acrobat's whip -> Pulled (dragged + dazed) -> Chase
//anything -> health hits 0 -> Dead
[RequireComponent(typeof(NavMeshAgent))] //unity adds a NavMeshAgent automatically if its missing
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Search, Pulled, Dead } //enum = a fixed list of named options

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints; //empty objects to walk between, leave empty to wander randomly
    [SerializeField] private float wanderRadius = 8f; //how far from the start it wanders when there are no patrol points
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float patrolWaitTime = 2f; //stands still this long at each point

    [Header("Senses")]
    [SerializeField] private float sightRange = 15f; //how far it can see
    [SerializeField, Range(0f, 360f)] private float viewAngle = 120f; //how wide its view cone is
    [SerializeField] private float hearingRange = 3f; //notices the player this close even from behind
    [SerializeField] private LayerMask sightBlockers = ~0; //what blocks its view (walls etc)

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f; //a bit slower than the player's walk (6) so u can get away
    [SerializeField] private float loseSightTime = 3f; //keeps chasing this long after losing sight before giving up

    [Header("Search")]
    [SerializeField] private float searchTime = 4f; //looks around at the last seen spot this long

    [Header("Attack")]
    [SerializeField] private float attackRange = 3f; //starts an attack when the player is this close (middle to middle)
    [SerializeField] private float damage = 20f;
    [SerializeField] private float windUpTime = 0.6f; //the warning before the hit, gives the player time to dodge
    [SerializeField] private float recoverTime = 0.8f; //stands still after hitting, a chance for the player to hit back
    [SerializeField] private float attackReach = 1.5f; //how far in front the hit area is
    [SerializeField] private float attackRadius = 1.2f;
    [SerializeField] private Color windUpColor = Color.red; //turns this colour while winding up so u can see it coming

    [Header("Reference")]
    [SerializeField] private Transform player; //left empty = finds the player by itself

    public State CurrentState { get; private set; } = State.Patrol; //public so we can see it / other scripts can read it

    private NavMeshAgent agent;
    private Health health;
    private Health playerHealth;
    private Collider playerCollider;
    private Renderer rend;
    private Color normalColor;

    private Vector3 startPosition;
    private int patrolIndex; //which patrol point we are heading to
    private float stateTimer; //general timer, each state uses it for its own waiting
    private float lastSeenTime; //when we last saw the player
    private Vector3 lastSeenPosition; //where we last saw the player
    private bool hasStruck; //has the current attack already done its hit

    //whip pull (filled in by Pull())
    private Vector3 pullDestination; //where we get dragged to
    private float pullSpeed;
    private float pullDuration; //how long the drag takes
    private float pullStunTime; //how long we stay dazed after the drag

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) normalColor = rend.material.color;

        if (player == null)
        {
            //find the object with the PlayerMovement script on it
            PlayerMovement found = FindFirstObjectByType<PlayerMovement>();
            if (found != null) player = found.transform;
        }
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            playerCollider = player.GetComponent<Collider>();
        }

        startPosition = transform.position;
    }

    private void OnEnable()
    {
        health.onDied.AddListener(OnDied);
        health.onDamaged.AddListener(OnDamaged);
    }

    private void OnDisable()
    {
        health.onDied.RemoveListener(OnDied);
        health.onDamaged.RemoveListener(OnDamaged);
    }

    private void Start()
    {
        EnterState(State.Patrol);
    }

    void Update()
    {
        if (player == null || CurrentState == State.Dead) return;

        //each state has its own method, the switch picks the one for the state we are in
        switch (CurrentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Attack: UpdateAttack(); break;
            case State.Search: UpdateSearch(); break;
            case State.Pulled: UpdatePulled(); break;
        }
    }

    //the whip calls this: drag this enemy to "destination" over "duration" seconds, then it stays dazed for "stunTime"
    public void Pull(Vector3 destination, float duration, float stunTime)
    {
        if (CurrentState == State.Dead) return;

        pullDestination = destination;
        pullDestination.y = transform.position.y; //only drag along the floor
        pullDuration = Mathf.Max(duration, 0.05f); //never 0, we divide by it
        pullSpeed = Vector3.Distance(transform.position, pullDestination) / pullDuration; //speed = distance / time
        pullStunTime = stunTime;

        //being whipped = it definitely knows where the player is
        lastSeenTime = Time.time;
        lastSeenPosition = player.position;
        EnterState(State.Pulled);
    }

    //=============== STATE CHANGES ===============

    //everything that should happen ONCE when we switch state goes here
    private void EnterState(State newState)
    {
        CurrentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case State.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0f; //walk right up to the patrol point
                GoToNextPatrolPoint();
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                //stop a bit before the player instead of walking into them, still inside attack range
                agent.stoppingDistance = attackRange * 0.8f;
                break;

            case State.Attack:
                agent.isStopped = true; //stand still to attack
                agent.velocity = Vector3.zero; //stop sliding straight away
                hasStruck = false;
                break;

            case State.Search:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0f;
                agent.SetDestination(lastSeenPosition);
                break;

            case State.Pulled:
                agent.isStopped = true; //no walking by itself, the whip moves it
                agent.velocity = Vector3.zero;
                SetColor(normalColor); //cancels a wind up that was going on
                break;

            case State.Dead:
                agent.isStopped = true;
                agent.enabled = false; //stop pathfinding completely
                SetColor(normalColor);
                break;
        }
    }

    //=============== STATES ===============

    private void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            EnterState(State.Chase);
            return;
        }

        //remainingDistance = how far left to walk, pathPending = still working out the path
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            stateTimer += Time.deltaTime; //arrived, wait a bit
            if (stateTimer >= patrolWaitTime)
            {
                stateTimer = 0f;
                GoToNextPatrolPoint();
            }
        }
    }

    private void UpdateChase()
    {
        if (PlayerIsDead())
        {
            EnterState(State.Patrol);
            return;
        }

        if (CanSeePlayer())
        {
            lastSeenTime = Time.time;
            lastSeenPosition = player.position;
        }
        else if (Time.time - lastSeenTime > loseSightTime)
        {
            EnterState(State.Search); //lost them for too long, go look where they were
            return;
        }

        //keep heading to where the player is (or was last seen), the navmesh works out the route
        agent.SetDestination(lastSeenPosition);

        if (DistanceToPlayer() <= attackRange && CanSeePlayer())
        {
            EnterState(State.Attack);
        }
    }

    private void UpdateAttack()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer < windUpTime)
        {
            //WIND UP: face the player and slowly turn red, this is the warning
            FacePlayer();
            SetColor(Color.Lerp(normalColor, windUpColor, stateTimer / windUpTime));
        }
        else if (!hasStruck)
        {
            //STRIKE: happens once, right as the wind up ends
            hasStruck = true;
            SetColor(normalColor);
            Strike();
        }
        else if (stateTimer >= windUpTime + recoverTime)
        {
            //RECOVER done: attack again if still close, otherwise chase
            if (PlayerIsDead()) EnterState(State.Patrol);
            else if (DistanceToPlayer() <= attackRange) EnterState(State.Attack);
            else EnterState(State.Chase);
        }
    }

    private void UpdateSearch()
    {
        if (CanSeePlayer())
        {
            EnterState(State.Chase);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            //at the last seen spot, turn on the spot to look around
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);
            stateTimer += Time.deltaTime;
            if (stateTimer >= searchTime)
            {
                EnterState(State.Patrol); //give up
            }
        }
    }

    private void UpdatePulled()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer < pullDuration)
        {
            //DRAG: slide towards the destination
            //agent.Move moves us but keeps us on the navmesh, so we cant get dragged through walls or off edges
            Vector3 next = Vector3.MoveTowards(transform.position, pullDestination, pullSpeed * Time.deltaTime);
            agent.Move(next - transform.position);
        }
        else if (stateTimer >= pullDuration + pullStunTime)
        {
            //DAZED is over, go after the player
            if (PlayerIsDead()) EnterState(State.Patrol);
            else EnterState(State.Chase);
        }
        //in between = dazed, just stands there (free hits for the player)
    }

    //=============== EVENTS ===============

    private void OnDamaged()
    {
        //got hit = we know where the player is, even if they snuck up from behind
        if (CurrentState == State.Patrol || CurrentState == State.Search)
        {
            lastSeenTime = Time.time;
            lastSeenPosition = player.position;
            EnterState(State.Chase);
        }
    }

    private void OnDied()
    {
        EnterState(State.Dead);
    }

    //=============== HELPERS ===============

    private bool CanSeePlayer()
    {
        if (PlayerIsDead()) return false;

        Vector3 eyes = transform.position;
        Vector3 target = playerCollider != null ? playerCollider.bounds.center : player.position;
        Vector3 toPlayer = target - eyes;
        float distance = toPlayer.magnitude;

        if (distance > sightRange) return false; //too far

        //outside the view cone and not close enough to hear = cant notice
        //Vector3.Angle = angle between where we face and where the player is
        if (distance > hearingRange && Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f) return false;

        //line of sight, anything in the way that isnt us or the player = blocked
        RaycastHit[] hits = Physics.RaycastAll(eyes, toPlayer.normalized, distance, sightBlockers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(player)) continue;
            return false;
        }
        return true;
    }

    private void Strike()
    {
        //same idea as the player's attack, a ball in front, but it only hurts the player (no hitting other enemies)
        Vector3 center = transform.position + transform.forward * attackReach;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(player))
            {
                playerHealth?.TakeDamage(damage, transform.position); //?. = only if playerHealth isnt null
                return; //only hit once
            }
        }
    }

    private void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f; //flat, dont tilt
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[patrolIndex].position);
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length; //% wraps back to 0 after the last point
        }
        else
        {
            //no points: pick a random spot near the start, SamplePosition snaps it onto the navmesh
            Vector3 random = startPosition + Random.insideUnitSphere * wanderRadius;
            if (NavMesh.SamplePosition(random, out NavMeshHit navHit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }
        }
    }

    private float DistanceToPlayer()
    {
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = player.position; b.y = 0f;
        return Vector3.Distance(a, b); //along the floor only, height differences dont count
    }

    private bool PlayerIsDead() => playerHealth != null && playerHealth.IsDead;

    private void SetColor(Color c)
    {
        if (rend != null) rend.material.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        //DEBUG ONLY: yellow = sight range, white lines = view cone, cyan = hearing, red = attack hit area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.white;
        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * sightRange);
        Gizmos.DrawLine(transform.position, transform.position + right * sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackReach, attackRadius);
    }
}
