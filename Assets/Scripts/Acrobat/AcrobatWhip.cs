using System.Collections;
using UnityEngine;

//the acrobats whip, the parts that are not a normal attack: so the pull and plunge slam
//damage goes through the base rules: TakeDamage (damage numbers), AddUltimateCharge
[RequireComponent(typeof(AcrobatController))]
public class AcrobatWhip : MonoBehaviour
{
    [Header("Pull (right click)")]
    [SerializeField] float pullStaminaCost = 25f;
    [SerializeField] float pullRange = 10f;
    [SerializeField] float pullCooldown = 1f;
    [SerializeField] float pullStopDistance = 2.5f;//light enemies land this far in front of the player
    [SerializeField] float enemyPullTime = 0.25f;
    [SerializeField] float pullStunTime = 0.6f; //the enemy's AI is paused this long after landing
    [SerializeField] float zipSpeed = 22f; //player flying to a heavy enemy
    [SerializeField] float zipStopDistance = 2.5f;
    [SerializeField] float zipMaxTime = 0.6f;
    [Header("Plunge Slam (left click in the air)")]
    [SerializeField] float slamSpeed = 28f;
    [SerializeField] float slamDamageMultiplier = 2.5f; //x Strength
    [SerializeField] float slamRadius = 3.5f;
    [SerializeField] float slamRecovery = 0.3f;
    [SerializeField] float slamMaxTime = 2f;
    [Header("Look")]
    [SerializeField] Color whipColor = new Color(0.45f, 0.25f, 0.1f);
    [SerializeField] float whipThickness = 0.05f;

    //what the whip is doing right now
    enum Action { None, Zip, Slam, SlamRecover }
    Action action = Action.None;
    float actionTimer;
    Transform zipTarget;

    AcrobatController acro;
    PlayerStats stats;
    PlayerCombat combat;
    CharacterController cc;
    WeaponInventory weapons;
    //the line we draw while pulling
    LineRenderer line;
    Transform lineTarget;
    float lineHideTime;
    float nextPullTime;
    public bool IsBusy
    {
        get { return action != Action.None; }
    }
    void Awake()
    {
        acro = GetComponent<AcrobatController>();
        stats = GetComponent<PlayerStats>();
        combat = GetComponent<PlayerCombat>();
        cc = GetComponent<CharacterController>();
        weapons = GetComponent<WeaponInventory>();
        CreateLine();
    }
    void Update()
    {
        if (action == Action.Zip) ContinueZip();
        else if (action == Action.Slam) ContinueSlam();
        else if (action == Action.SlamRecover) ContinueSlamRecover();

        UpdateLine();
    }

   //pull system

    public bool TryPull()
    {
        if (IsBusy) return false;
        if (Time.time < nextPullTime) return false;
        if (stats.IsDead) return false;
        CharacterStats target = FindPullTarget();
        if (target == null)
        {
            Debug.Log("Whip pull: nothing to pull");
            return false; //no stamina spent on a miss
        }
        if (!stats.TrySpendStamina(pullStaminaCost))
        {
            Debug.Log("Whip pull: no stamina");
            return false;
        }
        nextPullTime = Time.time + pullCooldown;
        bool heavy = false;
        Pullable pullable = target.GetComponentInParent<Pullable>();
        if (pullable != null) heavy = pullable.IsHeavy;

        if (heavy)
        {
            //too big to move, we go to it 
            Debug.Log("Whip pull: " + target.name + " is heavy, zipping to it");
            zipTarget = target.transform;
            action = Action.Zip;
            actionTimer = 0f;
            acro.BeginExternalControl();
            ShowLine(zipTarget, zipMaxTime);
        }
        else
        {
            //light means yank it to a spot just in front of us
            Debug.Log("Whip pull: yanking " + target.name);
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            Vector3 destination = transform.position + toTarget.normalized * pullStopDistance;
            StartCoroutine(DragEnemy(target, destination));
            ShowLine(target.transform, enemyPullTime);
        }
        return true;
    }
    //who cna be pulled is baicallu the locked target if its in range or otherwise he closest damageable thing straight ahead
    CharacterStats FindPullTarget()
    {
        if (acro.LockTarget != null)
        {
            CharacterStats locked = acro.LockTarget.GetComponentInParent<CharacterStats>();
            if (locked == null || locked.IsDead) return null;
            if (Vector3.Distance(transform.position, locked.transform.position) > pullRange) return null;
            return locked;
        }
        //spehrecast all wokrs like rolling a ball forward and list everything it touches
        Vector3 origin = transform.position + cc.center;
        RaycastHit[] hits = Physics.SphereCastAll(origin, 0.8f, transform.forward, pullRange, ~0, QueryTriggerInteraction.Ignore);
        CharacterStats best = null;
        float bestDist = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; //not us
            CharacterStats s = hit.collider.GetComponentInParent<CharacterStats>();
            if (s == null || s.IsDead) continue;
            if (!combat.CanDamage(s)) continue; //base rule, no friendly fire(dont know if this is needed rn but might be good for the ally system discussed)
            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                best = s;
            }
        }
        return best;
    }
    //drags an enemy along the navmesh (agent.Move keeps it out of walls), cancels its swing,
    //then pauses its AI for the daze, nothing in the enemy's own scripts is changed 
    IEnumerator DragEnemy(CharacterStats victim, Vector3 destination)
    {
        EnemyStun stun = EnemyStun.Pause(victim); //swing cancelled, AI off, agent stopped
        destination.y = victim.transform.position.y; //only drag along the floor
        float time = Mathf.Max(enemyPullTime, 0.05f);
        float speed = Vector3.Distance(victim.transform.position, destination) / time; //speed = distance / time
        float t = 0f;
        while (t < enemyPullTime && victim != null) //victim could die mid pull
        {
            t += Time.deltaTime;
            Vector3 next = Vector3.MoveTowards(victim.transform.position, destination, speed * Time.deltaTime);
            if (stun.CanMoveAgent)
            {
                stun.Agent.Move(next - victim.transform.position); //stays on the navmesh meaning cant go through walls
            }
            else
            {
                victim.transform.position = next;
            }
            yield return null; //wait one frame
        }
        yield return new WaitForSeconds(pullStunTime); //if dazed u got free hits
        stun.Resume(); //safe even if it died meanwhile
    }
    void ContinueZip()
    {
        actionTimer += Time.deltaTime;
        if (zipTarget == null || actionTimer > zipMaxTime)
        {
            EndZip(); //target gone or took too long
            return;
        }
        Vector3 toTarget = zipTarget.position + Vector3.up * 0.5f - (transform.position + cc.center);
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flat.magnitude <= zipStopDistance)
        {
            EndZip(); //arrived
            return;
        }
        acro.MoveExternal(toTarget.normalized * zipSpeed); //fly straight at it, test test test pls 
        transform.rotation = Quaternion.LookRotation(flat.normalized);
    }
    void EndZip()
    {
        action = Action.None;
        zipTarget = null;
        acro.EndExternalControl();
        HideLine();
    }

    //plunge slam, inspired by arkham knight catwoman
    public bool TryStartSlam()
    {
        if (IsBusy) return false;
        if (acro.IsGroundedNow) return false; //only in the air
        if (stats.IsDead) return false;

        action = Action.Slam;
        actionTimer = 0f;
        acro.BeginExternalControl();
        return true;
    }
    void ContinueSlam()
    {
        actionTimer += Time.deltaTime;
        acro.MoveExternal(Vector3.down * slamSpeed); //straight down
        bool landed = acro.IsGroundedNow || cc.isGrounded;
        if (landed || actionTimer > slamMaxTime)
        {
            SlamImpact();
        }
    }
    void SlamImpact()
    {
        Vector3 feet = transform.position + cc.center - Vector3.up * (cc.height * 0.5f);
        int damage = Mathf.Max(1, Mathf.RoundToInt(stats.strength.Value * slamDamageMultiplier));
        //hurt everything around the landing point ( we got shared helper, same rules as every other area hit)
        int count = DamageArea.Hit(feet, slamRadius, damage, combat, stats);
        Debug.Log("Plunge slam! " + damage + " damage to " + count + " target(s)");

        action = Action.SlamRecover; //stuck for a moment
        actionTimer = 0f;
    }
    void ContinueSlamRecover()
    {
        actionTimer += Time.deltaTime;
        acro.MoveExternal(Vector3.down * 2f); //stand still, stay glued to the floor
        if (actionTimer >= slamRecovery)
        {
            action = Action.None;
            acro.EndExternalControl();
        }
    }

//whip visual (someone pls make a better one, although this one is not too bad)
    void CreateLine()
    {
        //a LineRenderer draws a line between points, we use a 2 point line from the hand to the enemy
        GameObject go = new GameObject("WhipLine");
        go.transform.SetParent(transform, false);
        line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = whipThickness;
        line.endWidth = whipThickness * 0.4f;
        line.material = new Material(Shader.Find("Sprites/Default")); //simple shader that just shows a color
        line.startColor = whipColor;
        line.endColor = whipColor;
        line.enabled = false;
    }
    void ShowLine(Transform target, float duration)
    {
        lineTarget = target;
        lineHideTime = Time.time + duration;
        line.enabled = true;
    }
    void HideLine()
    {
        line.enabled = false;
        lineTarget = null;
    }
    void UpdateLine()
    {
        if (line == null || !line.enabled) return;

        if (Time.time >= lineHideTime || lineTarget == null)
        {
            HideLine();
            return;
        }
        //start at the weapon's head if there is one, otherwise the middle of the player
        Vector3 start = transform.position + cc.center;
        if (weapons != null && weapons.Muzzle != null) start = weapons.Muzzle.position;

        line.SetPosition(0, start);
        line.SetPosition(1, lineTarget.position + Vector3.up * 0.8f);
    }
    void OnDisable()
    {
        //switched off mid action meaning give control back and hide the line
        if (action != Action.None && acro != null) acro.EndExternalControl();
        action = Action.None;
        if (line != null) line.enabled = false;
    }
    void OnDrawGizmosSelected()
    {
        //debug purposes only so purple is slam area and orange is pull reach
        Gizmos.color = new Color(0.7f, 0.3f, 1f);
        Gizmos.DrawWireSphere(transform.position, slamRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward * pullRange);
    }
}
