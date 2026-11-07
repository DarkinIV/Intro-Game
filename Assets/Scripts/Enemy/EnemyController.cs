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
//   Return         lost the player or got pulled too far from home, walks back and heals
//   Busy           playing a special animation, like a boss roaring
//   Dead
// The attacks are a list on this component, so the same brain works for every enemy.
// Only a couple of enemies attack at once (maxAttackers), the rest circle and wait their turn.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(EnemyCombat))]
public class EnemyController : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Circle, Attack, Recover, Staggered, Stunned, Busy, Dead }

    // How many enemies can be attacking the player at the same time
    public static int maxAttackers = 2;

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

    [Header("Movement")]
    [SerializeField] float stoppingDistance = 1.5f;
    [SerializeField] float walkSpeed = 2f;
    [SerializeField] float rotationSpeed = 10f;

    [Header("Attacks")]
    [SerializeField] List<EnemyAttack> attacks = new List<EnemyAttack>();
    [SerializeField] float recoveryTime = 0.8f; // pause after every attack

    [Header("Group Fights")]
    [SerializeField] bool waitsForTurn = true;
    [SerializeField] float turnRequestDistance = 6f; // asks for a turn once this close to the player
    [SerializeField] float circleDistance = 3.5f; // how far away it waits
    [SerializeField] float turnCooldown = 1.5f; // after its turn, gives the others a chance first

    [Header("Poise")]
    [SerializeField] float maxPoise = 30f;
    [SerializeField] float poiseRecoveryDelay = 2f; // starts recovering this long after the last hit
    [SerializeField] float poiseRecoveryRate = 15f; // per second
    [SerializeField] float staggerTime = 0.7f;
    [SerializeField] bool isHeavy;
    [SerializeField] bool isBoss; // no health bar above it, BossPhases shows the big one at the bottom of the screen

    [Header("UI")]
    [SerializeField] Transform uiAnchor;

    [Header("Death")]
    [SerializeField] int deathAnimations = 5; // how many clips are in the death blend tree
    [SerializeField] float corpseTime = 6f;

    // Every enemy in the scene, so they can alert each other
    static List<EnemyController> allEnemies = new List<EnemyController>();

    // The enemies that currently have a turn to attack
    static List<EnemyController> attackers = new List<EnemyController>();

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
    float poise;
    float lastHurtTime;
    float staggeredUntil;
    float busyUntil;

    public bool IsHeavy => isHeavy;
    public bool IsStunned => Time.time < stunnedUntil;

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
    }

    void OnDisable()
    {
        allEnemies.Remove(this);
        stats.OnDamaged -= OnHurt;
        ReleaseTurn();
    }

    void Start()
    {
        agent.speed = stats.GetMoveSpeed();
        agent.stoppingDistance = stoppingDistance;

        // We turn the enemy ourselves so it can face the player while attacking
        agent.updateRotation = false;

        homeRotation = transform.rotation;
        poise = maxPoise;

        stats.OnHealthReachedZero += Die;

        if (!isBoss)
            EnemyUIManager.instance.Create(this, uiAnchor);

    }

    void Update()
    {
        if (currentState == State.Dead)
            return;

        RecoverPoise();

        if (IsStunned)
        {
            SetState(State.Stunned);
            UpdateAnimator();
            return;
        }

        if (currentState == State.Staggered && Time.time < staggeredUntil)
        {
            UpdateAnimator();
            return;
        }

        // Let the roar (or other special animation) play out
        if (currentState == State.Busy && Time.time < busyUntil)
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
        animator.SetBool("isStunned", IsStunned);

        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        animator.SetFloat("Speed", velocity.magnitude);

        // Movement relative to where it's facing, so circling sideways plays the strafe animations
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        animator.SetFloat("MoveX", localVelocity.x);
        animator.SetFloat("MoveZ", localVelocity.z);
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
            // Walking home ignores the player (unless they hit it, see OnHurt)
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

        // Close enough to hear them, whichever way we're facing
        if (distance <= hearingRadius)
            return true;

        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f)
            return false;

        // Anything solid in between blocks the view (characters don't)
        foreach (RaycastHit hit in Physics.RaycastAll(eyes, toPlayer / distance, distance, sightBlockers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<CharacterStats>() == null)
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

    void OnHurt(int damage)
    {
        // Getting hit always gets its attention, even from behind
        if (target == null && Player.instance != null)
            Notice(Player.instance.transform, true);

        // Hits wear down poise. When it runs out the enemy staggers (not on the killing blow).
        lastHurtTime = Time.time;
        if (stats.currentHealth <= 0 || IsStunned || currentState == State.Staggered || currentState == State.Busy)
            return;

        poise -= damage;
        if (poise <= 0f)
            Stagger();
    }

    void Stagger()
    {
        poise = maxPoise;
        staggeredUntil = Time.time + staggerTime;

        combat.CancelAttack();
        StopMoving();
        ReleaseTurn();
        SetState(State.Staggered);
        animator.SetTrigger("Stagger");
    }

    void RecoverPoise()
    {
        if (Time.time - lastHurtTime >= poiseRecoveryDelay)
            poise = Mathf.Min(maxPoise, poise + poiseRecoveryRate * Time.deltaTime);
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
        float range = currentAttack != null ? currentAttack.maxRange : stoppingDistance;

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

        if (TryTakeTurn())
        {
            SetState(State.Chase);
            return;
        }

        // Pick a spot circleDistance away from the player and a little to the side, and walk there
        Vector3 away = -toTarget.normalized;
        Vector3 side = Vector3.Cross(Vector3.up, away) * circleDirection;
        MoveTo(target.position + away * circleDistance + side * 1.5f, walkSpeed);
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
        agent.SetDestination(position);
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
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * speedScale * Time.deltaTime);
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
        combat.CancelAttack();
        StopMoving();
        ReleaseTurn();
        SetState(State.Stunned);

        // Getting stunned by the player means it knows where they are
        if (target == null && Player.instance != null)
            Notice(Player.instance.transform, true);
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
        combat.CancelAttack();
        StopMoving();
        ReleaseTurn();
        SetState(State.Busy);
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
        animator.SetBool("isStunned", false);
        animator.ResetTrigger("Stagger");
        animator.SetFloat("DeathID", Random.Range(0, deathAnimations));
        animator.SetTrigger("Die");

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
