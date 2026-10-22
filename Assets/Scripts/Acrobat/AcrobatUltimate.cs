using UnityEngine;

//the acrobat's ult untouchable and the perfect dodge
//when Q pressed and meter full then for a few seconds every enemy swing near us triggers an automatic sidestep (think undertaker from nightreign)
//perfect dodge when an enemy near us starts a swing while we are mid dodge then ult meter goes up
//the Q key itself is read by the shared AbilityKeys script, which calls the ult through the base ability system
//enemy swings come from the shared EnemyAttackListener (it listens to every enemy's CharacterCombat.OnAttack for us)
[RequireComponent(typeof(AcrobatController))]
public class AcrobatUltimate : MonoBehaviour
{
    [Header("Ultimate (Q)")]
    [SerializeField] float perfectDodgeRange = 3.5f;//perfect dodge
    [SerializeField] int perfectDodgeCharge = 60; //ult meter gained per perfect dodge (same units as damage dealt)
    [SerializeField] float autoDodgeRange = 4f; //during ult the enemies that are this close trigger an automatic sidestep

    public bool UltimateActive { get; private set; }
    public float UltimateRemaining
    {
        get
        {
            if (!UltimateActive) return 0f;
            return Mathf.Max(0f, untouchableUntil - Time.time);
        }
    }
    AcrobatController acro;
    PlayerStats stats;
    AcrobatDodge dodge;
    AcrobatWallMoves wall;
    AcrobatInvulnerability invuln;
    EnemyAttackListener enemyAttacks;
    float untouchableUntil;
    void Awake() //when started
    {
        acro = GetComponent<AcrobatController>();
        stats = GetComponent<PlayerStats>();
        dodge = GetComponent<AcrobatDodge>();
        wall = GetComponent<AcrobatWallMoves>();

        invuln = GetComponent<AcrobatInvulnerability>();
        if (invuln == null)
        {
            invuln = gameObject.AddComponent<AcrobatInvulnerability>();
        }
        enemyAttacks = GetComponent<EnemyAttackListener>();
        if (enemyAttacks == null)
        {
            enemyAttacks = gameObject.AddComponent<EnemyAttackListener>();
        }
    }

    void OnEnable()
    {
        if (enemyAttacks != null) enemyAttacks.OnEnemyAttack += OnEnemyAttack;
    }
    void OnDisable()
    {
        if (enemyAttacks != null) enemyAttacks.OnEnemyAttack -= OnEnemyAttack;
    }
    void Update()
    {
        if (stats == null || stats.IsDead) return;
        if (UltimateActive && Time.time >= untouchableUntil)
        {
            UltimateActive = false;
            Debug.Log("Untouchable over"); //wehn over
        }
    }
    //called by AutoDodgeUltimate when Q clicked with full meter
    public void ActivateUntouchable(float duration)
    {
        UltimateActive = true;
        untouchableUntil = Time.time + duration;
        invuln.MakeInvulnerable(duration); //so nothing slips through even if a sidestep is late
        Debug.Log("<color=magenta>UNTOUCHABLE</color> for " + duration + "s");
    }
    void OnEnemyAttack(EnemyCombat enemy)
    {
        if (enemy == null || stats == null || stats.IsDead) return;
        Vector3 toEnemy = enemy.transform.position - transform.position;
        toEnemy.y = 0f;
        float dist = toEnemy.magnitude;
        if (UltimateActive && dist <= autoDodgeRange && !acro.ExternalControl && dodge != null)
        {
            //automatic sidestep, away to the side of the attacker whichh is free of charge
            Vector3 side = Vector3.Cross(Vector3.up, toEnemy.normalized);
            if (Vector3.Dot(side, acro.MoveDir) < 0f) side = -side; //pick the side the player is leaning towards
            if (wall != null) wall.EndWall();
            dodge.StartDodge(side);
            Debug.Log("<color=magenta>Auto-dodge!</color>");
        }
        else if (dodge != null && dodge.IsDodging && invuln.IsInvulnerable && dist <= perfectDodgeRange)
        {
            stats.AddUltimateCharge(perfectDodgeCharge);
            Debug.Log("<color=yellow>Perfect dodge!</color> +" + perfectDodgeCharge + " ult charge");
        }
    }
    void OnDrawGizmosSelected()
    {
        //debug only magenta then auto dodge range, yellow then perfect dodge range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, autoDodgeRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, perfectDodgeRange);
    }
}
