using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// The enemy's brain. It's always in one of these states:
//   Idle           hasn't noticed the player yet
//   Chase          runs at the player until it's in range of the attack it picked
//   Circle         in a group fight, walks round the player while it waits for its turn to attack
//   Attack         plays the attack, only tracking the player for the first moment
//   Recover        a short pause after attacking, the player's chance to hit back
//   Staggered      ran out of poise from taking hits
//   Stunned        whip pull, parry
//   Busy           playing a special animation, like a boss roaring
//   Dead
// The attacks are a list on this component, so the same brain works for every enemy.
// Only a couple of enemies attack at once (maxAttackers), the rest circle and wait their turn.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(EnemyCombat))]
public class EnemyController : MonoBehaviour
{
    public enum State { Idle, Chase, Circle, Attack, Recover, Staggered, Stunned, Busy, Dead }

    // How many enemies can be attacking the player at the same time
    public static int maxAttackers = 2;

    // Animator parameters, turned into numbers once instead of looking up the name every frame
    static readonly int SpeedParam = Animator.StringToHash("Speed");
    static readonly int HorizontalParam = Animator.StringToHash("Horizontal");
    static readonly int VerticalParam = Animator.StringToHash("Vertical");
    static readonly int StunnedParam = Animator.StringToHash("isStunned");
    static readonly int StaggerTrigger = Animator.StringToHash("Stagger");
    static readonly int DeathIDParam = Animator.StringToHash("DeathID");
    static readonly int DieTrigger = Animator.StringToHash("Die");

    public State currentState; // just so you can see what it's doing in the inspector

    [Header("Sight")]
    [SerializeField] float detectionRadius = 10f; // how far it can see
    [SerializeField] float viewAngle = 120f;
    [SerializeField] float hearingRadius = 3f; // notices the player this close even from behind
    [SerializeField] float eyeHeight = 1.6f;
    [SerializeField] LayerMask sightBlockers = ~0;
    [SerializeField] float loseTargetRadius = 13f;
    [SerializeField] float forgetTime = 5f; // gives up after not seeing the player for this long
    [SerializeField] float allyAlertRadius = 8f; // enemies this close join in when one spots the player

    // Run speed and turn speed are on CharacterStats, stopping distance is on the NavMeshAgent
    [Header("Movement")]
    [Tooltip("Speed while circling the player, as a fraction of its run speed (Move Speed Base on CharacterStats)")]
    [Range(0f, 1f)][SerializeField] float walkMultiplier = 0.5f;

    [Header("Attacks")]
    [SerializeField] List<EnemyAttack> attacks = new List<EnemyAttack>();
    [SerializeField] float recoveryTime = 0.8f; // pause after every attack

    [Header("Spotting")]
    [Tooltip("Animator trigger played the first time it sees the player, like a roar. Leave empty for none")]
    [SerializeField] string spotAnimation;
    [SerializeField] float spotAnimationTime = 1.5f;

    [Header("Group Fights")]
    [SerializeField] bool waitsForTurn = true;
    [SerializeField] float turnRequestDistance = 6f; // asks for a turn once this close to the player
    [SerializeField] float circleDistance = 3.5f; // how far away it waits
    [SerializeField] float turnCooldown = 1.5f; // after its turn, gives the others a chance first
    [Tooltip("Seconds (min and max) it walks round the player after each attack, like a duel. 0 for never")]
    [SerializeField] Vector2 circleBetweenAttacks;

    [Header("Type")]
    [SerializeField] bool isHeavy; // the whip can't pull it, the Acrobat zips over to it instead
    [SerializeField] bool isBoss;

    [Header("UI")]
    [SerializeField] Transform uiAnchor;

    [Header("Death")]
    [SerializeField] int deathAnimations = 5; // how many clips are in the death blend tree
    [SerializeField] float corpseTime = 6f;

    // Every enemy in the scene, so they can alert each other
    static List<EnemyController> allEnemies = new List<EnemyController>();

    // The enemies that currently have a turn to attack
    static List<EnemyController> attackers = new List<EnemyController>();

    // Shared by every enemy's sight check so it doesn't make a new array each time
    static RaycastHit[] sightHits = new RaycastHit[8];

    NavMeshAgent agent;
    Animator animator;
    CharacterStats stats;
    EnemyCombat combat;

    Transform target;
    EnemyAttack currentAttack;
    Quaternion homeRotation;
    float stateStartTime;
    float stunnedUntil;
    float lastSeenTime;
    float nextSightCheck;
    float nextTurnTime;
    float circleDirection = 1f;
    float nextCircleFlip;
    float busyUntil; // when the special animation ends
    float circleUntil;
    Vector3 lastDestination;
    bool hasSpottedPlayer;

    public bool IsHeavy => isHeavy;
    public bool IsStunned => Time.time < stunnedUntil;
    public Transform Target => target;

    // BossPhases uses these to show and hide the boss health bar
    public event System.Action OnSpottedPlayer;
    public event System.Action OnLostPlayer;

    float TimeInState => Time.time - stateStartTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
        combat = GetComponent<EnemyCombat>();
    }

    void OnEnable()
    {
        allEnemies.Add(this);
        stats.OnDamaged += OnHurt;
        stats.OnStaggered += Stagger;
    }

    void OnDisable()
    {
        allEnemies.Remove(this);
        stats.OnDamaged -= OnHurt;
        stats.OnStaggered -= Stagger;
        ReleaseTurn();
    }

    void Start()
    {
        // We turn the enemy ourselves so it can face the player while attacking
        agent.updateRotation = false;

        homeRotation = transform.rotation;

        // Spread out the sight checks so the enemies don't all raycast on the same frame
        nextSightCheck = Time.time + Random.value * 0.2f;

        stats.OnHealthReachedZero += Die;

        if (!isBoss)
            EnemyUIManager.instance.Create(this, uiAnchor);
    }

    void Update()
    {
        if (currentState == State.Dead)
            return;

        if (IsStunned)
        {
            SetState(State.Stunned);
            UpdateAnimator();
            return;
        }

        // Let the stagger or special animation (like a boss roar) play out
        if ((currentState == State.Staggered && stats.IsStaggered) || (currentState == State.Busy && Time.time < busyUntil))
        {
            UpdateAnimator();
            return;
        }

        // The stun, stagger or special animation just ran out
        if (currentState == State.Stunned || currentState == State.Staggered || currentState == State.Busy)
            SetState(target != null ? State.Chase : State.Idle);

        UpdateSight();

        switch (currentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Chase: UpdateChase(); break;
            case State.Circle: UpdateCircle(); break;
            case State.Attack: UpdateAttack(); break;
            case State.Recover: UpdateRecover(); break;
        }

        UpdateAnimator();
    }

    void SetState(State state)
    {
        if (currentState == state)
            return;

        currentState = state;
        stateStartTime = Time.time;
    }

    // The agent moves the enemy normally. During attacks we use the animation's own movement
    // instead (lunges and steps), passed through the agent so it can't go through walls.
    void OnAnimatorMove()
    {
        if (currentState != State.Attack || !agent.enabled || !agent.isOnNavMesh)
            return;

        Vector3 step = animator.deltaPosition;
        step.y = 0f;
        agent.Move(step);
    }

    void UpdateAnimator()
    {
        animator.SetBool(StunnedParam, IsStunned);

        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        animator.SetFloat(SpeedParam, velocity.magnitude);

        // Movement relative to where it's facing, so circling sideways plays the strafe animations
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        animator.SetFloat(HorizontalParam, localVelocity.x);
        animator.SetFloat(VerticalParam, localVelocity.z);
    }

    // Noticing and losing the player

    void UpdateSight()
    {
        // Looking uses a raycast, so only check a few times a second
        if (Time.time < nextSightCheck)
            return;

        nextSightCheck = Time.time + 0.2f;

        if (Player.instance == null || Player.instance.playerStats.IsDead)
        {
            if (target != null)
                LoseTarget();
            return;
        }

        Transform player = Player.instance.transform;
        bool seen = CanSee(player);

        if (target == null)
        {
            if (seen)
                Notice(player, true);
            return;
        }

        if (seen)
            lastSeenTime = Time.time;

        // Don't give up halfway through a swing
        if (currentState == State.Attack)
            return;

        bool tooFar = Vector3.Distance(transform.position, target.position) > loseTargetRadius;
        if (tooFar || Time.time - lastSeenTime > forgetTime)
            LoseTarget();
    }

    bool CanSee(Transform player)
    {
        Vector3 eyes = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position + Vector3.up - eyes;
        float distance = toPlayer.magnitude;

        if (distance > detectionRadius)
            return false;

        // Close enough to hear them regardless direction faced
        if (distance <= hearingRadius)
            return true;

        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f)
            return false;

        // Anything solid in between blocks the view (characters don't)
        int hitCount = Physics.RaycastNonAlloc(eyes, toPlayer / distance, sightHits, distance, sightBlockers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            if (sightHits[i].collider.GetComponentInParent<CharacterStats>() == null)
                return false;
        }

        return true;
    }

    void Notice(Transform player, bool alertAllies)
    {
        if (target != null || currentState == State.Dead)
            return;

        target = player;
        lastSeenTime = Time.time;
        currentAttack = null;
        SetState(State.Chase);
        OnSpottedPlayer?.Invoke();

        // Only the first time
        if (!hasSpottedPlayer && !string.IsNullOrEmpty(spotAnimation))
            PlayAnimation(spotAnimation, spotAnimationTime);
        hasSpottedPlayer = true;

        if (!alertAllies)
            return;

        foreach (EnemyController ally in allEnemies)
        {
            if (ally != this && Vector3.Distance(transform.position, ally.transform.position) <= allyAlertRadius)
                ally.Notice(player, false);
        }
    }

    void LoseTarget()
    {
        target = null;
        combat.Target = null;
        currentAttack = null;
        ReleaseTurn();
        SetState(State.Idle);
        OnLostPlayer?.Invoke();
    }

    // Getting hit or stunned by the player always gets its attention
    void NoticeAttacker()
    {
        if (target == null && Player.instance != null)
            Notice(Player.instance.transform, true);
    }

    void OnHurt(int damage)
    {
        NoticeAttacker();
    }

    // It stays staggered for the Stagger Time.
    void Stagger()
    {
        Interrupt(State.Staggered);
        animator.SetTrigger(StaggerTrigger);
    }

    // Stops whatever it was doing and switches to a stagger, stun or special animation
    void Interrupt(State state)
    {
        combat.CancelAttack();
        StopMoving();
        ReleaseTurn();
        SetState(state);
    }

    // Taking turns to attack

    bool HasTurn => !waitsForTurn || attackers.Contains(this);

    bool TryTakeTurn()
    {
        if (HasTurn)
            return true;

        if (attackers.Count >= maxAttackers)
            return false;

        // Just had a turn, so let anyone else who's waiting go first
        if (Time.time < nextTurnTime && SomeoneElseIsWaiting())
            return false;

        attackers.Add(this);
        return true;
    }

    bool SomeoneElseIsWaiting()
    {
        foreach (EnemyController enemy in allEnemies)
        {
            if (enemy != this && enemy.currentState == State.Circle)
                return true;
        }

        return false;
    }

    void ReleaseTurn()
    {
        if (attackers.Remove(this))
            nextTurnTime = Time.time + turnCooldown;
    }

    // States

    void UpdateIdle()
    {
        // Slowly turn back to the way it was facing when it spawned
        TurnTowards(homeRotation * Vector3.forward, 0.3f);
    }

    void UpdateChase()
    {
        Vector3 toTarget = Flat(target.position - transform.position);
        float distance = toTarget.magnitude;

        // Close to the player: only go in if it's our turn, otherwise wait nearby
        if (distance <= turnRequestDistance && !TryTakeTurn())
        {
            SetState(State.Circle);
            return;
        }

        // The player ran off, let someone else have the turn
        if (distance > turnRequestDistance + 2f)
            ReleaseTurn();

        // Plan the next attack. Prefer one that fits the distance we're at, otherwise any that's ready.
        if (currentAttack == null || !currentAttack.IsReady)
            currentAttack = PickAttack(distance) ?? PickAttack(-1f);

        // With every attack on cooldown it just gets close and waits
        float range = currentAttack != null ? currentAttack.maxRange : agent.stoppingDistance;

        if (distance > range)
        {
            MoveTo(target.position, stats.GetMoveSpeed());
            TurnTowards(agent.desiredVelocity);
            return;
        }

        // In range: stop, turn to face the player, and attack once facing them
        StopMoving();
        TurnTowards(toTarget);

        if (currentAttack != null && currentAttack.IsReady && Vector3.Angle(transform.forward, toTarget) <= currentAttack.maxAngle)
            BeginAttack();
    }

    void BeginAttack()
    {
        SetState(State.Attack);
        StopMoving();

        currentAttack.readyTime = Time.time + currentAttack.cooldown;
        combat.Target = target;
        combat.StartAttack(currentAttack);
    }

    void UpdateAttack()
    {
        // Keep tracking the player for the first moment, then commit, so a well timed sidestep works
        if (TimeInState < currentAttack.trackTime)
            TurnTowards(Flat(target.position - transform.position));

        if (!combat.IsAttacking)
        {
            currentAttack = null;
            SetState(State.Recover);
        }
    }

    void UpdateRecover()
    {
        // A slow turn, so the player has a window to punish it
        TurnTowards(Flat(target.position - transform.position), 0.3f);

        if (TimeInState >= recoveryTime)
        {
            // Turn over, give someone else a go
            ReleaseTurn();

            // Duelists back off and walk round the player for a bit before going in again
            if (circleBetweenAttacks.y > 0f)
            {
                circleUntil = Time.time + Random.Range(circleBetweenAttacks.x, circleBetweenAttacks.y);
                SetState(State.Circle);
            }
            else
                SetState(State.Chase);
        }
    }

    // Waits at a distance, walking round the player and facing them, until a turn is free
    void UpdateCircle()
    {
        Vector3 toTarget = Flat(target.position - transform.position);
        float distance = toTarget.magnitude;

        // The player moved away, follow them again
        if (distance > turnRequestDistance + 1f)
        {
            SetState(State.Chase);
            return;
        }

        if (Time.time >= circleUntil && TryTakeTurn())
        {
            SetState(State.Chase);
            return;
        }

        // Pick a spot circleDistance away from the player and a little to the side, and walk there
        Vector3 away = -toTarget.normalized;
        Vector3 side = Vector3.Cross(Vector3.up, away) * circleDirection;
        MoveTo(target.position + away * circleDistance + side * 1.5f, stats.GetMoveSpeed() * walkMultiplier);
        TurnTowards(toTarget);

        // Change direction every few seconds so it doesn't look robotic
        if (Time.time >= nextCircleFlip)
        {
            circleDirection = Random.value < 0.5f ? -1f : 1f;
            nextCircleFlip = Time.time + Random.Range(2f, 5f);
        }
    }

    // Picks an attack at random. Attacks with a higher weight come up more often.
    // With a distance it only picks attacks in range, with -1 it picks any attack that's ready.
    EnemyAttack PickAttack(float distance)
    {
        float totalWeight = 0f;
        foreach (EnemyAttack attack in attacks)
        {
            if (CanUse(attack, distance))
                totalWeight += attack.weight;
        }

        float roll = Random.value * totalWeight;
        EnemyAttack picked = null;

        foreach (EnemyAttack attack in attacks)
        {
            if (!CanUse(attack, distance))
                continue;

            picked = attack;
            roll -= attack.weight;
            if (roll <= 0f)
                break;
        }

        return picked;
    }

    bool CanUse(EnemyAttack attack, float distance)
    {
        if (!attack.IsReady)
            return false;

        return distance < 0f || (distance >= attack.minRange && distance <= attack.maxRange);
    }

    // Moving and turning

    void MoveTo(Vector3 position, float speed)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        agent.speed = speed;
        agent.isStopped = false;

        // Working out a path is slow, so only ask for a new one when the spot has moved a bit (or it has no path)
        if ((position - lastDestination).sqrMagnitude > 0.25f || (!agent.hasPath && !agent.pathPending))
        {
            agent.SetDestination(position);
            lastDestination = position;
        }
    }

    void StopMoving()
    {
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    void TurnTowards(Vector3 direction, float speedScale = 1f)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, stats.turnSpeed * speedScale * Time.deltaTime);
    }

    Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    // Called by the player's abilities

    // Stops the enemy and cancels its attack (whip pull, parry)
    public void Stun(float duration)
    {
        if (currentState == State.Dead)
            return;

        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
        stats.LockPoise(duration); // hits while it's stunned don't count towards a stagger
        Interrupt(State.Stunned);
        NoticeAttacker();
    }

    // Moves the enemy along the NavMesh so it doesn't get pulled through walls (whip pull)
    public void Displace(Vector3 offset)
    {
        if (agent.enabled && agent.isOnNavMesh)
            agent.Move(offset);
        else
            transform.position += offset;
    }

    // Called by BossPhases

    // Stands still and plays an animation for a moment, like a boss roaring at the start of the fight or a new phase
    public void PlayAnimation(string trigger, float duration)
    {
        if (currentState == State.Dead)
            return;

        busyUntil = Time.time + duration;
        stats.LockPoise(duration); // a boss can't be staggered out of its roar or heal
        Interrupt(State.Busy);

        if (!string.IsNullOrEmpty(trigger))
            animator.SetTrigger(trigger);
    }

    public void SetAttacks(List<EnemyAttack> newAttacks)
    {
        attacks = newAttacks;

        // Plan the next attack from the new list (unless it's in the middle of one)
        if (currentState != State.Attack)
            currentAttack = null;
    }

    void Die()
    {
        SetState(State.Dead);
        target = null;
        ReleaseTurn();

        // Corpses shouldn't block anyone or get in the way of the NavMesh
        StopMoving();
        agent.enabled = false;
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        // isStunned off first, otherwise the animator jumps back to the stunned animation
        animator.SetBool(StunnedParam, false);
        animator.ResetTrigger(StaggerTrigger);
        animator.SetFloat(DeathIDParam, Random.Range(0, deathAnimations));
        animator.SetTrigger(DieTrigger);

        Destroy(gameObject, corpseTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Edges of the view cone
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward * detectionRadius);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * detectionRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, circleDistance);
    }
}
