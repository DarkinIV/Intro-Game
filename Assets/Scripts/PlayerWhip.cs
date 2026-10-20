using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the acrobat's whip, goes on the player
//left click on the ground = CRACK: a long thin hit in front (or at the locked target)
//left click in the air = PLUNGE SLAM: dive straight down and hit everything around where u land
//right click = PULL: light enemy gets yanked to u, heavy enemy yanks u to it
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Health))]
public class PlayerWhip : MonoBehaviour
{
    [Header("Crack (left click)")]
    [SerializeField] private float crackDamage = 12f; //lower than the sword, bcz the whip hits from far away
    [SerializeField] private float crackRange = 7f; //how far the whip reaches
    [SerializeField] private float crackWidth = 0.5f; //how thick the hit line is (radius)
    [SerializeField] private float crackCooldown = 0.45f;

    [Header("Pull (right click)")]
    [SerializeField] private float pullStaminaCost = 25f;
    [SerializeField] private float pullRange = 10f;
    [SerializeField] private float pullCooldown = 1f;
    [SerializeField] private float pullStopDistance = 3f; //light enemies land this far in front of u
    [SerializeField] private float enemyPullTime = 0.25f; //how long the yank takes
    [SerializeField] private float pullStunTime = 0.6f; //enemy is dazed this long after being yanked
    [SerializeField] private float zipSpeed = 25f; //how fast u fly to a heavy enemy
    [SerializeField] private float zipStopDistance = 3f; //stop this far from a heavy enemy
    [SerializeField] private float zipMaxTime = 0.6f; //safety: stop zipping after this long even if we didnt arrive

    [Header("Plunge Slam (left click in air)")]
    [SerializeField] private float slamSpeed = 30f; //how fast u dive down
    [SerializeField] private float slamDamage = 30f;
    [SerializeField] private float slamRadius = 3.5f; //everything this close to where u land gets hit
    [SerializeField] private float slamRecovery = 0.3f; //stuck for a moment after landing (risk for the reward)
    [SerializeField] private float slamMaxTime = 2f; //safety: give up if we somehow never land

    [Header("Look")]
    [SerializeField] private Color whipColor = new Color(0.45f, 0.25f, 0.1f); //brown
    [SerializeField] private float whipThickness = 0.08f;
    [SerializeField] private float crackShowTime = 0.12f; //how long the whip line stays visible after a crack

    private PlayerMovement movement;
    private PlayerLockOn lockOn;
    private PlayerDodge dodge;
    private Health health;
    private Stamina stamina;
    private Rigidbody rb;
    private Collider col;
    private LineRenderer line;

    private float nextCrackTime;
    private float nextPullTime;
    private bool leftClicked; //clicks are noticed in Update but used in FixedUpdate (physics)
    private bool rightClicked;

    //what the whip is doing rn
    private enum Action { None, Zip, Slam, SlamRecover }
    private Action action = Action.None;
    private float actionTimer;
    private Transform zipTarget;
    private Transform lineTarget; //while pulling, the whip line stays attached to this
    private float lineHideTime; //for the crack, hide the line at this time
    private Vector3 lineEnd;

    public bool IsBusy => action != Action.None; //other scripts can check if the whip is in the middle of something

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        lockOn = GetComponent<PlayerLockOn>();
        dodge = GetComponent<PlayerDodge>();
        health = GetComponent<Health>();
        stamina = GetComponent<Stamina>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        CreateWhipLine();
    }

    private void OnDisable()
    {
        //switched off (class changed or died): stop whatever we were doing and hide the whip
        if (action != Action.None && movement != null) movement.ControlsLocked = false;
        action = Action.None;
        if (line != null) line.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) leftClicked = true; //0 = left mouse button
        if (Input.GetMouseButtonDown(1)) rightClicked = true; //1 = right mouse button
        UpdateWhipLine();
    }

    private void FixedUpdate()
    {
        //finish what we are doing first
        switch (action)
        {
            case Action.Zip: ContinueZip(); break;
            case Action.Slam: ContinueSlam(); break;
            case Action.SlamRecover: ContinueSlamRecover(); break;
        }

        bool left = leftClicked, right = rightClicked;
        leftClicked = rightClicked = false; //use up the clicks

        if (!CanAct()) return;

        if (left)
        {
            if (movement.IsGrounded) Crack();
            else StartSlam();
        }
        else if (right)
        {
            Pull();
        }
    }

    //not dead, not dodging, not already busy with the whip, and nothing else has taken over movement
    private bool CanAct()
    {
        return !health.IsDead
            && movement.enabled
            && action == Action.None
            && !movement.ControlsLocked
            && (dodge == null || !dodge.IsDodging);
    }

    //=============== CRACK ===============

    private void Crack()
    {
        if (Time.time < nextCrackTime) return;
        nextCrackTime = Time.time + crackCooldown;

        Vector3 origin = col.bounds.center;
        Vector3 dir = AimDirection(origin);

        //SphereCastAll = slide a ball along the line and give back EVERYTHING it touches on the way
        RaycastHit[] hits = Physics.SphereCastAll(origin, crackWidth, dir, crackRange, ~0, QueryTriggerInteraction.Ignore);
        HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>(); //each enemy only once per crack
        float lineLength = crackRange;

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; //skip ourselves
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target == null || !alreadyHit.Add(target)) continue;

            target.TakeDamage(crackDamage, transform.position);
            //distance 0 means it was already touching at the start, dont shorten the line for that
            if (hit.distance > 0f) lineLength = Mathf.Min(lineLength, hit.distance + crackWidth);
        }

        ShowLine(origin + dir * lineLength, null, crackShowTime);
    }

    //locked on = aim at the target (even a bit up or down), otherwise straight ahead
    private Vector3 AimDirection(Vector3 origin)
    {
        if (lockOn != null && lockOn.CurrentTarget != null)
        {
            return (lockOn.CurrentTarget.LockPoint - origin).normalized;
        }
        return transform.forward;
    }

    //=============== PULL ===============

    private void Pull()
    {
        if (Time.time < nextPullTime) return;

        Transform target = FindPullTarget();
        if (target == null)
        {
            Debug.Log("Whip pull: nothing to pull");
            return; //no stamina spent if we missed
        }

        if (stamina != null && !stamina.TrySpend(pullStaminaCost))
        {
            Debug.Log("Whip pull failed: no stamina");
            return;
        }
        nextPullTime = Time.time + pullCooldown;

        Pullable pullable = target.GetComponentInParent<Pullable>();
        bool heavy = pullable != null && pullable.IsHeavy;

        if (heavy)
        {
            //too big to move: WE fly to IT
            Debug.Log($"Whip pull: {target.name} is heavy, zipping to it");
            zipTarget = target;
            action = Action.Zip;
            actionTimer = 0f;
            movement.ControlsLocked = true;
            ShowLine(target.position, target, zipMaxTime);
        }
        else
        {
            //light: yank it to a spot just in front of us
            Debug.Log($"Whip pull: yanking {target.name}");
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            Vector3 destination = transform.position + toTarget.normalized * pullStopDistance;

            EnemyAI ai = target.GetComponentInParent<EnemyAI>();
            if (ai != null) ai.Pull(destination, enemyPullTime, pullStunTime); //the enemy drags itself on the navmesh
            else StartCoroutine(DragObject(target, destination, enemyPullTime)); //things without AI (dummies)

            ShowLine(target.position, target, enemyPullTime);
        }
    }

    //who do we pull? the locked target if there is one and its in range, otherwise the first enemy straight ahead
    private Transform FindPullTarget()
    {
        if (lockOn != null && lockOn.CurrentTarget != null)
        {
            Transform locked = lockOn.CurrentTarget.transform;
            if (Vector3.Distance(transform.position, locked.position) <= pullRange) return locked;
            return null;
        }

        Vector3 origin = col.bounds.center;
        RaycastHit[] hits = Physics.SphereCastAll(origin, 0.8f, transform.forward, pullRange, ~0, QueryTriggerInteraction.Ignore);
        Transform closest = null;
        float closestDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;
            Health h = hit.collider.GetComponentInParent<Health>(); //only things with health (enemies), not walls
            if (h == null || h.IsDead) continue;
            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closest = h.transform;
            }
        }
        return closest;
    }

    //for objects without an EnemyAI: slide them over "time" seconds
    private IEnumerator DragObject(Transform target, Vector3 destination, float time)
    {
        Vector3 start = target.position;
        destination.y = start.y; //stay at the same height
        float t = 0f;
        while (t < 1f && target != null) //target != null bcz it could die/disappear mid pull
        {
            t += Time.deltaTime / time;
            target.position = Vector3.Lerp(start, destination, t); //Lerp = the point t of the way from start to destination
            yield return null; //wait one frame
        }
    }

    private void ContinueZip()
    {
        actionTimer += Time.fixedDeltaTime;

        //target died/disappeared, or took too long = stop
        if (zipTarget == null || actionTimer > zipMaxTime)
        {
            EndZip();
            return;
        }

        Vector3 toTarget = zipTarget.position - rb.position;
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flat.magnitude <= zipStopDistance)
        {
            EndZip(); //arrived
            return;
        }

        rb.velocity = toTarget.normalized * zipSpeed; //fly straight at it
    }

    private void EndZip()
    {
        action = Action.None;
        zipTarget = null;
        movement.ControlsLocked = false;
        rb.velocity *= 0.3f; //dont fly past it (no friction remember)
        HideLine();
    }

    //=============== PLUNGE SLAM ===============

    private void StartSlam()
    {
        action = Action.Slam;
        actionTimer = 0f;
        movement.ControlsLocked = true;
    }

    private void ContinueSlam()
    {
        actionTimer += Time.fixedDeltaTime;
        rb.velocity = Vector3.down * slamSpeed; //straight down, no sideways movement

        if (movement.IsGrounded || actionTimer > slamMaxTime)
        {
            SlamImpact();
        }
    }

    private void SlamImpact()
    {
        rb.velocity = Vector3.zero;
        Vector3 feet = new Vector3(col.bounds.center.x, col.bounds.min.y, col.bounds.center.z);

        Collider[] hits = Physics.OverlapSphere(feet, slamRadius, ~0, QueryTriggerInteraction.Ignore);
        HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();
        int count = 0;
        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !alreadyHit.Add(target)) continue;
            target.TakeDamage(slamDamage, feet);
            count++;
        }
        Debug.Log($"Plunge slam! Hit {count} target(s)");

        action = Action.SlamRecover; //stuck for a moment
        actionTimer = 0f;
    }

    private void ContinueSlamRecover()
    {
        actionTimer += Time.fixedDeltaTime;
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f); //stand still
        if (actionTimer >= slamRecovery)
        {
            action = Action.None;
            movement.ControlsLocked = false;
        }
    }

    //=============== WHIP LINE (the visible whip) ===============

    //a LineRenderer draws a line between points, we use it as a simple stand in for a real whip model
    private void CreateWhipLine()
    {
        GameObject lineObject = new GameObject("WhipLine");
        lineObject.transform.SetParent(transform, false);
        line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2; //a straight line, start and end
        line.startWidth = whipThickness;
        line.endWidth = whipThickness * 0.4f; //thinner at the tip
        line.material = new Material(Shader.Find("Sprites/Default")); //a simple shader that just shows a colour
        line.startColor = whipColor;
        line.endColor = whipColor;
        line.enabled = false;
    }

    //target = keep the end stuck to this object (for pulls), null = fixed end point (for cracks)
    private void ShowLine(Vector3 end, Transform target, float duration)
    {
        lineEnd = end;
        lineTarget = target;
        lineHideTime = Time.time + duration;
        line.enabled = true;
        UpdateWhipLine();
    }

    private void HideLine()
    {
        line.enabled = false;
        lineTarget = null;
    }

    private void UpdateWhipLine()
    {
        if (line == null || !line.enabled) return;
        if (Time.time >= lineHideTime)
        {
            HideLine();
            return;
        }
        if (lineTarget != null) lineEnd = lineTarget.position; //follow the thing we are pulling
        line.SetPosition(0, col.bounds.center); //start at the player
        line.SetPosition(1, lineEnd);
    }

    private void OnDrawGizmosSelected()
    {
        //DEBUG ONLY: orange line = crack reach, purple ring at the feet = slam area
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * crackRange);
        Gizmos.color = new Color(0.7f, 0.3f, 1f);
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}
