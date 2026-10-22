using System.Collections.Generic;
using UnityEngine;

//the fortune teller's extra buttons and effects. she keeps the base PlayerController's movement (through the shared DashJumpController: ctrl dash, space jump, left click attack, E interact)
[RequireComponent(typeof(PlayerStats))] //bo need to be repetitive
public class FortuneTellerAbilities : MonoBehaviour
{
    public enum Card { None, Sun, Tower, Death, Star, Fool }
    [Header("Keys")]
    [SerializeField] KeyCode sightKey = KeyCode.C;
    [SerializeField] KeyCode meditateKey = KeyCode.V;
    [Header("Crystal sight (hold C)")]
    [SerializeField] float sightRange = 20f;
    [SerializeField] float sightStaminaPerSecond = 8f;
    [Header("Mana refills")]
    [SerializeField] float fatedKillMana = 30f; //an ult affected enemy dying
    [SerializeField] float meditateManaPerSecond = 35f;
    [SerializeField] float meditateStartDelay = 0.4f;//must be still this long before mana starts flowing(maybe lets increase this depending on how gameplay progresses)
    [Header("Look")]
    [SerializeField] Color fateColor = new Color(0.65f, 0.2f, 1f, 1f);
    [SerializeField] Color towerColor = new Color(1f, 0.55f, 0.1f, 1f);

    //read by the hud
    public Card NextCard { get; private set; }
    public Card LastCard { get; private set; }
    public bool SightActive { get; private set; }
    public bool IsMeditating { get; private set; }//fortune teller combat blocks attacks while this is true
    public bool TowerReady
    {
        get { return Time.time < towerUntil; } //using get this much reminds me of java procedural programming lol
    }
    public bool DeathReady
    {
        get { return Time.time < deathUntil; }
    }
    public bool Cursed
    {
        get { return Time.time < curseUntil; }
    }
    public bool FateActive
    {
        get { return Time.time < fateUntil; }
    }
    public float FateRemaining
    {
        get { return Mathf.Max(0f, fateUntil - Time.time); }
    }
    public int MarkedCount
    {
        get { return marked.Count; }
    }
    //one enemy marked by Fate Sealed
    class MarkedEnemy
    {
        public CharacterStats stats;
        public RingMarker ring;
        public System.Action onDeath; //the listener we attached, so we can remove it again
    }
    PlayerStats stats;
    PlayerCombat combat;
    PlayerController controller;
    WeaponInventory weapons;
    Mana mana;
    Camera cam;

    //card effects waiting for the next hit
    float towerUntil;
    float deathUntil;
    float towerRadius;
    float towerMultiplier;
    float deathThreshold;
    //the Fool curse
    float curseUntil;
    float cursePercent;
    bool curseApplied;
    //Fate Sealed
    float fateUntil;
    float critMultiplier;
    List<MarkedEnemy> marked = new List<MarkedEnemy>();
    //meditation
    float meditateHeldSince = -1f;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        combat = GetComponent<PlayerCombat>();
        controller = GetComponent<PlayerController>();
        weapons = GetComponent<WeaponInventory>();
        mana = GetComponent<Mana>();
    }

    void OnEnable()
    {
        if (combat != null) combat.OnHit += OnHitLanded;
        if (stats != null) stats.OnDamaged += BreakMeditation;
    }
    void OnDisable()
    {
        if (combat != null) combat.OnHit -= OnHitLanded;
        if (stats != null) stats.OnDamaged -= BreakMeditation;
        EndCurse();
        ClearMarks();
        SetSight(false);
        IsMeditating = false;
    }
    void Start()
    {
        cam = Camera.main;
        //roll the first next card straight away so the player can see whats coming before the first draw (fairer id say)
        TarotDraw tarot = stats.BasicAbility as TarotDraw;
        if (tarot != null) NextCard = Roll(tarot.Settings);
    }
    void Update()
    {
        if (stats.IsDead)
        {
            SetSight(false);
            return;
        }

        UpdateSight(Input.GetKey(sightKey));
        UpdateMeditation(Input.GetKey(meditateKey));

        if (curseApplied && !Cursed) EndCurse();
        if (marked.Count > 0 && !FateActive) ClearMarks();
    }

    //tarot (my fav concept actually for this game)
    //called by TarotDraw when the B.A. clicked 
    public void DrawCard(TarotDrawData data)
    {
        Card card = NextCard;
        if (card == Card.None) card = Roll(data);

        NextCard = Roll(data); //the following card is decided now so it can be shown in advance
        LastCard = card;
        ApplyCard(card, data, true);
    }
    //weighted random, check out the code for that its just like weights in nn
    Card Roll(TarotDrawData d)
    {
        int total = d.sunWeight + d.towerWeight + d.deathWeight + d.starWeight + d.foolWeight;
        if (total <= 0) return Card.Sun;

        int r = Random.Range(0, total); //a number from 0 to total-1
        r -= d.sunWeight;
        if (r < 0) return Card.Sun;
        r -= d.towerWeight;
        if (r < 0) return Card.Tower;
        r -= d.deathWeight;
        if (r < 0) return Card.Death;
        r -= d.starWeight;
        if (r < 0) return Card.Star;
        return Card.Fool;
    }

    //allowFool must be false when the Fool turned into another card, so it cant roll itself again
    void ApplyCard(Card card, TarotDrawData d, bool allowFool)
    {
        if (card == Card.Sun)
        {
            int heal = Mathf.RoundToInt(stats.maxHealth.Value * d.sunHealPercent);
            stats.Heal(heal);
            Debug.Log("<color=yellow>The Sun</color>: healed " + heal);
        }
        else if (card == Card.Tower)
        {
            towerUntil = Time.time + d.chargedCardDuration;
            towerRadius = d.towerRadius;
            towerMultiplier = d.towerMagicMultiplier;
            Debug.Log("<color=orange>The Tower</color>: your next hit explodes");
        }
        else if (card == Card.Death)
        {
            deathUntil = Time.time + d.chargedCardDuration;
            deathThreshold = d.deathThreshold;
            Debug.Log("<color=grey>Death</color>: your next hit kills an enemy under " + d.deathThreshold.ToString("P0") + " health");
        }
        else if (card == Card.Star)
        {
            if (mana != null) mana.AddPercent(d.starManaPercent, "The Star");
            else Debug.Log("The Star: no Mana component on the player");
        }
        else if (card == Card.Fool)
        {
            bool cursed = allowFool && Random.value < d.foolCurseChance;
            if (cursed)
            {
                StartCurse(d.foolSlowPercent, d.foolCurseDuration);
                Debug.Log("<color=red>The Fool</color>: cursed! " + d.foolSlowPercent.ToString("P0") + " slower for " + d.foolCurseDuration + "s");
            }
            else
            {
                Card[] good = { Card.Sun, Card.Tower, Card.Death, Card.Star };
                Card lucky = good[Random.Range(0, good.Length)];
                Debug.Log("<color=green>The Fool</color>: lucky! it turns into " + lucky);
                ApplyCard(lucky, d, false);
            }
        }
    }
    void StartCurse(float percent, float duration)
    {
        EndCurse(); //never stack two slows, too evil
        cursePercent = percent;
        stats.AddPacePercent(-percent); //base helper, 30% slower
        curseApplied = true;
        curseUntil = Time.time + duration;
    }
    void EndCurse()
    {
        if (!curseApplied || stats == null) return;
        stats.RemovePacePercent(-cursePercent);
        curseApplied = false;
    }

//fate sealed ult 

    //called by fatesealed when the ultimate clicked
    public void ActivateFateSealed(FateSealedData data)
    {
        ClearMarks();
        fateUntil = Time.time + data.duration;
        critMultiplier = data.critMultiplier;
        if (cam == null) cam = Camera.main;
        EnemyCombat[] enemies = FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None);
        foreach (EnemyCombat enemy in enemies)
        {
            CharacterStats s = enemy.GetComponent<CharacterStats>();
            if (s == null || s.IsDead) continue;
            if (Vector3.Distance(transform.position, enemy.transform.position) > data.range) continue;
            if (!IsOnScreen(enemy.transform.position + Vector3.up)) continue;
            MarkedEnemy m = new MarkedEnemy();
            m.stats = s;
            m.ring = RingMarker.Spawn(enemy.transform.position, 0.9f, 0.9f, 0f, data.duration, fateColor, enemy.transform, 0.07f);

            //fated kill: when a marked enemy dies, she gains mana
            //we keep the listener so we can remove it again in ClearMarks(cant lock on th guy forever)
            CharacterStats victim = s;
            m.onDeath = () => OnMarkedEnemyDied(victim);
            s.OnHealthReachedZero += m.onDeath;
            marked.Add(m);
        }
        Debug.Log("<color=magenta>FATE SEALED</color>: " + marked.Count + " enemies marked for " + data.duration + "s");
    }
    //viewport = the screen as 0 to 1, z <= 0 means behind the camera
    bool IsOnScreen(Vector3 worldPoint)
    {
        if (cam == null) return true;
        Vector3 vp = cam.WorldToViewportPoint(worldPoint);
        if (vp.z <= 0f) return false;
        return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }
    bool IsMarked(CharacterStats victim)
    {
        foreach (MarkedEnemy m in marked)
        {
            if (m.stats == victim) return true;
        }
        return false;
    }
    void OnMarkedEnemyDied(CharacterStats victim)
    {
        if (!FateActive || !IsMarked(victim)) return;
        if (mana != null) mana.Add(fatedKillMana, "fated kill");
    }
    void ClearMarks()
    {
        foreach (MarkedEnemy m in marked)
        {
            if (m.stats != null && m.onDeath != null) m.stats.OnHealthReachedZero -= m.onDeath;
            if (m.ring != null) Destroy(m.ring.gameObject);
        }
        marked.Clear();
    }

//on hit so: tower, death or criticals 
//the base combat fires OnHit after the hit's own damage is done, so extra damage here never interrupts it
    void OnHitLanded(CharacterStats victim)
    {
        if (victim == null) return;

        //fate sealed: hit again for the extra crit damage (same formula as her normal hit so Magic + weapon bonus)
        if (FateActive && IsMarked(victim) && !victim.IsDead)
        {
            int bonus = 0;
            if (weapons != null && weapons.CurrentWeapon != null) bonus = weapons.CurrentWeapon.bonusDamage;

            int baseHit = stats.strength.Value; //if she somehow uses the plain PlayerCombat
            if (combat is FortuneTellerCombat) baseHit = stats.magic.Value;

            int extra = Mathf.Max(1, Mathf.RoundToInt((baseHit + bonus) * (critMultiplier - 1f)));
            victim.TakeDamage(extra);
            Debug.Log("<color=magenta>Fated crit</color> +" + extra);
        }
        //Death executes the victim if its weak enough (waits for a weak target, until the card runs out)
        if (DeathReady && !victim.IsDead && victim.currentHealth <= victim.maxHealth.Value * deathThreshold)
        {
            deathUntil = 0f;
            Debug.Log("<color=grey>Death</color> claims " + victim.name);
            victim.TakeDamage(victim.currentHealth);
        }
        //The Tower has the hit explode, damaging everything around the victim and the vic too
        if (TowerReady)
        {
            towerUntil = 0f;
            Explode(victim.transform.position);
        }
    }
    void Explode(Vector3 center)
    {
        int damage = Mathf.Max(1, Mathf.RoundToInt(stats.magic.Value * towerMultiplier));
        int count = DamageArea.Hit(center, towerRadius, damage, combat, stats); //shared helper, base rules
        RingMarker.Spawn(center, 0.3f, towerRadius, 0.15f, 0.3f, towerColor, null, 0.12f);
        Debug.Log("<color=orange>The Tower</color> explodes: " + damage + " damage to " + count + " target(s)");
    }

  //holding c, crystal sight , no need for it now 

    void UpdateSight(bool held)
    {
        bool canPay = held && stats.TrySpendStamina(sightStaminaPerSecond * Time.deltaTime, false);
        SetSight(canPay);

        if (!SightActive) return;
        foreach (RevealedBySight obj in RevealedBySight.All)
        {
            bool near = Vector3.Distance(transform.position, obj.transform.position) <= sightRange;
            obj.SetRevealed(near);
        }
    }
    void SetSight(bool on)
    {
        if (SightActive == on) return;
        SightActive = on;

        if (!on)
        {
            foreach (RevealedBySight obj in RevealedBySight.All) obj.SetRevealed(false);
        }
    }

    //meditating so holding v 
    void UpdateMeditation(bool held)
    {
        bool moving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
        bool dashing = controller != null && controller.IsDashing;

        if (!held || moving || dashing || mana == null)
        {
            if (IsMeditating) Debug.Log("Meditation broken");
            IsMeditating = false;
            meditateHeldSince = -1f;
            return;
        }
        if (meditateHeldSince < 0f) meditateHeldSince = Time.time;
        if (!IsMeditating && Time.time - meditateHeldSince >= meditateStartDelay)
        {
            IsMeditating = true;
            Debug.Log("Meditating...");
        }
        if (IsMeditating) mana.Add(meditateManaPerSecond * Time.deltaTime);
    }
    //getting hit interrupts meditation 
    void BreakMeditation(int damage)
    {
        if (!IsMeditating) return;
        IsMeditating = false;
        meditateHeldSince = Time.time + 1f; //short lockout, must resettle before it starts again
        Debug.Log("Meditation broken by a hit");
    }
}
