using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

//the knight now has a shield, holding right click to block and tapping it just before a hit lands to parry(yes i know parry is usually done with a sword but we didnt have the brain cells for it rn)
//  UNKILLABLE:  (the ultimate, switched on by KnightAbilities) whatever gets through can never take him below 1 HP
//when BHF returns true, the base skips the hit and reduced damage is applied here instead (so damage numbers still show)
//!!i beg of u do not rename "baseDamage" in CharacterCombat.cs!
//the shield reads the attacker's baseDamage by name (hehe reflection, since its protected) to work out how hard the hit would have been 
//do not rename u get error 
[RequireComponent(typeof(PlayerStats))]
public class KnightShield : MonoBehaviour, IInvulnerable
{
    [Header("Block (hold right click)")]
    [SerializeField] int blockButton = 1;//1 = right mouse button
    [SerializeField, Range(0f, 360f)] float blockAngle = 140f; //how wide the protected area in front is
    [SerializeField, Range(0f, 1f)] float chipDamagePercent = 0.2f; //part of the damage that still gets through a block
    [SerializeField] float blockStaminaCost = 12f;
    [SerializeField, Range(0f, 1f)] float blockSlowPercent = 0.4f;  //move this much slower with the shield up(might have to mkae this higher lets see)
    [SerializeField] float guardBreakTime = 1.2f;
    [Header("Parry (tap right click just before the hit)")]
    [SerializeField] float parryWindow = 0.2f;
    [SerializeField] float staggerTime = 1.2f;//how long a parried enemy is stunned
    [SerializeField] float riposteMultiplier = 1f; //next hit after a parry deals this x Strength extra
    [SerializeField] float riposteWindow = 2f;
    [Header("Look")]
    [SerializeField] Color shieldColor = new Color(0.55f, 0.6f, 0.68f);
    [SerializeField] Color blockFlash = new Color(0.4f, 0.7f, 1f);
    [SerializeField] Color parryFlash = new Color(1f, 0.85f, 0.2f);

    //read by the hud (knightabilkities)
    public bool IsBlocking { get; private set; }
    public bool GuardBroken
    {
        get { return Time.time < guardBrokenUntil; }
    }
    public bool RiposteArmed
    {
        get { return Time.time < riposteUntil; }
    }
    public bool UnkillableActive
    {
        get { return Time.time < unkillableUntil; }
    }
    public float UnkillableRemaining
    {
        get { return Mathf.Max(0f, unkillableUntil - Time.time); }
    }
    //the attacker baseDamage is protected in the base, so we look it up by name (see warning at the top)
    static readonly FieldInfo BaseDamageField =
        typeof(CharacterCombat).GetField("baseDamage", BindingFlags.Instance | BindingFlags.NonPublic);
    //an enemy has a hitbox which touches us for several physics steps and the base asks each time againa and ahain
    class RecentAnswer
    {
        public CharacterCombat attacker;
        public float until;
        public bool blocked;
    }
    List<RecentAnswer> recentAnswers = new List<RecentAnswer>();

    PlayerStats stats;
    PlayerCombat combat;
    PlayerController controller;
    Transform shield;
    Renderer shieldRenderer;
    float pressedTime = -999f;
    float guardBrokenUntil;
    float riposteUntil;
    float unkillableUntil;
    bool slowApplied;
    bool flashing;
    Coroutine flashRoutine;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        combat = GetComponent<PlayerCombat>();
        controller = GetComponent<PlayerController>();
        if (BaseDamageField == null) Debug.LogError("KnightShield: CharacterCombat.baseDamage was renamed; blocked damage falls back to 10.");
        CreateShield();
    }
    void OnEnable()
    {
        if (combat != null) combat.OnHit += OnHitLanded;
    }
    void OnDisable()
    {
        if (combat != null) combat.OnHit -= OnHitLanded;
        SetSlow(false);
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(blockButton)) pressedTime = Time.time;

        bool attacking = combat != null && combat.IsAttacking;
        bool dashing = controller != null && controller.IsDashing;
        bool holding = Input.GetMouseButton(blockButton);
        IsBlocking = holding && !GuardBroken && !attacking && !dashing && !stats.IsDead;

        SetSlow(IsBlocking);
        PoseShield(false);
    }
    //called by KnightAbilities when the ultimate is clicked
    public void ActivateUnkillable(float seconds)
    {
        unkillableUntil = Time.time + seconds;
    }

    
    //the base calls this before every hit on the knight
    public bool BlocksHitFrom(CharacterCombat attacker)
    {
        if (attacker == null || stats.IsDead) return false;
        //if same swing same answer!
        foreach (RecentAnswer r in recentAnswers)
        {
            if (r.attacker == attacker && Time.time < r.until) return r.blocked;
        }

        bool blocked = Decide(attacker);

        RecentAnswer answer = new RecentAnswer();
        answer.attacker = attacker;
        answer.until = Time.time + 0.5f;
        answer.blocked = blocked;
        recentAnswers.Add(answer);
        ForgetOldAnswers();

        return blocked;
    }

    void ForgetOldAnswers()
    {
        for (int i = recentAnswers.Count - 1; i >= 0; i--)
        {
            if (Time.time >= recentAnswers[i].until || recentAnswers[i].attacker == null) recentAnswers.RemoveAt(i);
        }
    }
    //works out what happens to this hit. true then we handled it then base musr not apply it 
    bool Decide(CharacterCombat attacker)
    {
        int damage = EstimateDamage(attacker);
        int final = damage;
        bool inFront = IsInFront(attacker.transform.position);
        //parry meaning right click was tapped just before the hit
        bool tappedInTime = Time.time - pressedTime <= parryWindow;
        if (inFront && tappedInTime && !GuardBroken)
        {
            Parry(attacker);
            return true;//no damage to us at alll
        }
        //block
        if (IsBlocking && inFront)
        {
            if (stats.TrySpendStamina(blockStaminaCost))
            {
                final = Mathf.CeilToInt(damage * chipDamagePercent);
                Flash(blockFlash);
                Debug.Log("<color=#66b3ff>Blocked</color>: " + damage + " -> " + final);
            }
            else
            {
                guardBrokenUntil = Time.time + guardBreakTime;
                Debug.Log("<color=red>Guard broken!</color> (no stamina)");
            }
        }

        //unkillable which means never below 1 HP
        if (UnkillableActive)
        {
            int maxAllowed = stats.currentHealth - 1;
            if (final > maxAllowed) final = maxAllowed;
        }
        if (final >= damage) return false;  //nothing changed so we let the base apply the normal hit
        if (final > 0)
        {
            stats.TakeDamage(final); //the reduced hit (damage numbers will appear anyway)
        }
        else if (UnkillableActive && damage > 0)
        {
            Debug.Log("<color=yellow>Unkillable</color>: held on at 1 HP");
        }
        return true;
    }

    //the same formula the base uses for an enemy's hit: its baseDamage + its weapon bonus damage
    int EstimateDamage(CharacterCombat attacker)
    {
        int dmg = 10;
        if (BaseDamageField != null) dmg = (int)BaseDamageField.GetValue(attacker);

        WeaponInventory w = attacker.GetComponent<WeaponInventory>();
        if (w != null && w.CurrentWeapon != null) dmg += w.CurrentWeapon.bonusDamage;

        return Mathf.Max(0, dmg);
    }
    bool IsInFront(Vector3 point)
    {
        Vector3 to = point - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return true;
        return Vector3.Angle(transform.forward, to) <= blockAngle * 0.5f;
    }
    //parry plus riposte(a little gaming terminology here and there), u gotta play a few games to make a few games (not true but okay)
    void Parry(CharacterCombat attacker)
    {
        riposteUntil = Time.time + riposteWindow;
        Flash(parryFlash);
        RingMarker.Spawn(attacker.transform.position, 0.3f, 1.4f, 0.15f, 0.35f, parryFlash, null, 0.1f);
        Debug.Log("<color=yellow>PARRY!</color> " + attacker.name + " is staggered. Next hit: riposte");

        CharacterStats enemy = attacker.GetComponent<CharacterStats>();
        StartCoroutine(Stagger(enemy));
    }

    //pause the enemy for the stagger time (EnemyStun cancels swing, stops AI and agent then puts them back)
    IEnumerator Stagger(CharacterStats enemy)
    {
        EnemyStun stun = EnemyStun.Pause(enemy);
        yield return new WaitForSeconds(staggerTime);
        stun.Resume();
    }
    //the base fires OnHit after each of the knight's own hits, a riposte adds extra damage on top
    void OnHitLanded(CharacterStats victim)
    {
        if (!RiposteArmed || victim == null || victim.IsDead) return;
        riposteUntil = 0f;
        int extra = Mathf.Max(1, Mathf.RoundToInt(stats.strength.Value * riposteMultiplier));
        victim.TakeDamage(extra);
        Debug.Log("<color=yellow>Riposte</color> +" + extra);
    }

  //slow plus shield vis
    void SetSlow(bool on)
    {
        if (on == slowApplied || stats == null) return;
        if (on) stats.AddPacePercent(-blockSlowPercent);
        else stats.RemovePacePercent(-blockSlowPercent);
        slowApplied = on;
    }

    //placeholder shield: flatbx at leftside
    void CreateShield()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Shield (placeholder)";
        Destroy(go.GetComponent<Collider>());//visual only must not bump into anything
        shield = go.transform;
        shield.SetParent(transform, false);
        shield.localScale = new Vector3(0.08f, 0.75f, 0.55f);
        shieldRenderer = go.GetComponent<Renderer>();
        shieldRenderer.material.color = shieldColor;
        PoseShield(true);
    }
    void PoseShield(bool instant)
    {
        if (shield == null) return;

        Vector3 pos = new Vector3(-0.5f, 1f, 0.1f);//resting at his left side
        Quaternion rot = Quaternion.identity;
        if (IsBlocking)
        {
            pos = new Vector3(0f, 1.15f, 0.55f);  //held up in front
            rot = Quaternion.Euler(0f, 90f, 0f); //turned to face forward
        }

        float t = 1f;
        if (!instant) t = 1f - Mathf.Exp(-20f * Time.deltaTime); //smooth move towards the pose
        shield.localPosition = Vector3.Lerp(shield.localPosition, pos, t);
        shield.localRotation = Quaternion.Slerp(shield.localRotation, rot, t);

        if (!flashing)
        {
            if (GuardBroken) shieldRenderer.material.color = Color.Lerp(shieldColor, Color.red, 0.5f);
            else shieldRenderer.material.color = shieldColor;
        }
    }
    void Flash(Color c)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(c));
    }
    IEnumerator FlashRoutine(Color c)
    {
        flashing = true;
        shieldRenderer.material.color = c;
        yield return new WaitForSeconds(0.15f);
        flashing = false;
    }
}
