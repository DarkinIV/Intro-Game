using System.Collections.Generic;
using UnityEngine;

// The Fortune Teller. F: Tarot Draw, C: Crystal Sight (hold), V: Meditate (hold), R: Fate Sealed.
// Tarot Draw and Crystal Sight cost mana, Meditate gives it back.
// Her passive is Foresight: she sees where nearby enemy attacks will land,
// and dashing away from one gives her some mana.
// The combat side of her cards and Fate Sealed is in FortuneTellerCombat.
[RequireComponent(typeof(FortuneTellerCombat))]
public class FortuneTellerController : PlayerController
{
    public enum Card { None, Sun, Tower, Death, Star, Fool }

    [Header("Foresight")]
    [SerializeField] float foresightRange = 15f;
    [SerializeField] float warningTime = 0.45f;
    [SerializeField] float strikeDistance = 1.2f;   // how far in front of the enemy their hit lands
    [SerializeField] float warningRadius = 2.3f;    // she counts as in danger if she's this close to it
    [SerializeField] float foreseenDodgeMana = 25f;
    [SerializeField] GameObject warningParticles;

    FortuneTellerCombat ftCombat;
    Mana mana;

    // When each attack she's in danger from will land
    List<float> warnings = new List<float>();

    float meditatingSince = -1f;
    float meditateLockedUntil;

    float curseUntil;
    float cursePercent;
    bool curseApplied;

    public Card NextCard { get; private set; }
    public Card LastCard { get; private set; }
    public bool SightActive { get; private set; }
    public bool IsMeditating { get; private set; }
    public bool Cursed => Time.time < curseUntil;

    protected override void Awake()
    {
        base.Awake();
        ftCombat = GetComponent<FortuneTellerCombat>();
        mana = GetComponent<Mana>();
    }

    protected override void Start()
    {
        base.Start();

        // Pick the first card now so the player can already see what's coming
        foreach (Ability ability in stats.Abilities)
        {
            if (ability.Data is TarotDrawData tarot)
                NextCard = Roll(tarot);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnemyCombat.OnAnyEnemyAttack += Foresee;
        stats.OnDamaged += BreakMeditation;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EnemyCombat.OnAnyEnemyAttack -= Foresee;
        stats.OnDamaged -= BreakMeditation;

        EndCurse();
        SetSight(false);
        IsMeditating = false;
        meditatingSince = -1f;
    }

    protected override void Update()
    {
        base.Update();

        if (curseApplied && !Cursed)
            EndCurse();

        warnings.RemoveAll(landTime => Time.time > landTime);
    }

    protected override void Attack()
    {
        if (!IsMeditating)
            base.Attack();
    }

    // Foresight

    // Enemies tell us when they start an attack, before the hit actually lands
    void Foresee(EnemyCombat enemy)
    {
        if (Vector3.Distance(transform.position, enemy.transform.position) > foresightRange)
            return;

        Vector3 strikePoint = enemy.transform.position + enemy.transform.forward * strikeDistance;

        Vector3 myPosition = transform.position;
        myPosition.y = strikePoint.y;

        if (Vector3.Distance(myPosition, strikePoint) <= warningRadius)
            warnings.Add(Time.time + warningTime);

        if (warningParticles != null)
            Destroy(Instantiate(warningParticles, strikePoint, Quaternion.identity, enemy.transform), warningTime);
    }

    // Dashing while an attack is about to hit her gives mana for each one
    protected override void OnDashStarted()
    {
        mana.Add(foreseenDodgeMana * warnings.Count);
        warnings.Clear();
    }

    // Crystal Sight and Meditate

    protected override bool CanChannel(ChanneledAbility ability)
    {
        // Has to be standing still on the ground, and there's no point at full mana
        if (ability.Data is MeditateData)
            return IsGrounded && !IsBusy && Time.time >= meditateLockedUntil && !mana.IsFull;

        if (ability.Data is CrystalSightData sight)
            return mana.Has(sight.manaPerSecond * Time.deltaTime);

        return true;
    }

    // Meditating keeps her in place
    protected override bool MovementBlocked => meditatingSince >= 0f;

    protected override void UpdateChannel(ChanneledAbility ability, bool active)
    {
        if (ability.Data is CrystalSightData sight)
            UpdateSight(sight, active);
        else if (ability.Data is MeditateData meditate)
            UpdateMeditation(meditate, active);
    }

    void UpdateSight(CrystalSightData sight, bool active)
    {
        SetSight(active);

        if (!SightActive)
            return;

        mana.TrySpend(sight.manaPerSecond * Time.deltaTime, false);

        foreach (RevealedBySight hidden in RevealedBySight.All)
            hidden.SetRevealed(Vector3.Distance(transform.position, hidden.transform.position) <= sight.range);
    }

    void SetSight(bool on)
    {
        if (SightActive == on)
            return;

        SightActive = on;

        // Hide everything again when she stops looking
        if (!on)
        {
            foreach (RevealedBySight hidden in RevealedBySight.All)
                hidden.SetRevealed(false);
        }
    }

    void UpdateMeditation(MeditateData meditate, bool active)
    {
        if (!active)
        {
            IsMeditating = false;
            meditatingSince = -1f;
            return;
        }

        if (meditatingSince < 0f)
            meditatingSince = Time.time;

        // Mana only starts coming in after a short delay
        IsMeditating = Time.time - meditatingSince >= meditate.startDelay;

        if (IsMeditating)
            mana.Add(meditate.manaPerSecond * Time.deltaTime);
    }

    // Getting hit stops the meditation and she can't start again for a second
    void BreakMeditation(int damage)
    {
        if (meditatingSince >= 0f)
            meditateLockedUntil = Time.time + 1f;
    }

    // Tarot Draw (F)
    public override bool UseAbility(Ability ability)
    {
        if (!(ability.Data is TarotDrawData tarot) || !mana.TrySpend(tarot.manaCost))
            return false;

        Card card = NextCard != Card.None ? NextCard : Roll(tarot);

        NextCard = Roll(tarot);
        LastCard = card;
        ApplyCard(card, tarot, true);
        return true;
    }

    // Fate Sealed (R)
    public override bool UseUltimate(UltimateAbility ultimate)
    {
        if (!(ultimate.Data is FateSealedData fate))
            return false;

        ftCombat.SealFate(fate);
        return true;
    }

    // Picks a card at random, cards with a higher weight come up more often
    Card Roll(TarotDrawData tarot)
    {
        int total = tarot.sunWeight + tarot.towerWeight + tarot.deathWeight + tarot.starWeight + tarot.foolWeight;
        int roll = Random.Range(0, total);

        int sum = tarot.sunWeight;
        if (roll < sum) return Card.Sun;

        sum += tarot.towerWeight;
        if (roll < sum) return Card.Tower;

        sum += tarot.deathWeight;
        if (roll < sum) return Card.Death;

        sum += tarot.starWeight;
        if (roll < sum) return Card.Star;

        return Card.Fool;
    }

    // allowFool is false when the Fool turns into another card, so it can't pick itself again
    void ApplyCard(Card card, TarotDrawData tarot, bool allowFool)
    {
        switch (card)
        {
            case Card.Sun:
                stats.Heal(Mathf.RoundToInt(stats.maxHealth.Value * tarot.sunHealPercent));
                break;

            case Card.Tower:
                ftCombat.ArmTower(tarot.towerRadius, tarot.towerMagicMultiplier, tarot.chargedCardDuration);
                break;

            case Card.Death:
                ftCombat.ArmDeath(tarot.deathThreshold, tarot.chargedCardDuration);
                break;

            case Card.Star:
                mana.AddPercent(tarot.starManaPercent);
                break;

            case Card.Fool:
                // Either a curse or one of the good cards at random
                if (allowFool && Random.value < tarot.foolCurseChance)
                {
                    StartCurse(tarot.foolSlowPercent, tarot.foolCurseDuration);
                }
                else
                {
                    Card[] goodCards = { Card.Sun, Card.Tower, Card.Death, Card.Star };
                    ApplyCard(goodCards[Random.Range(0, goodCards.Length)], tarot, false);
                }
                break;
        }
    }

    // The Fool's curse slows her down for a while
    void StartCurse(float slowPercent, float duration)
    {
        EndCurse();
        cursePercent = slowPercent;
        stats.AddPacePercent(-cursePercent);
        curseApplied = true;
        curseUntil = Time.time + duration;
    }

    void EndCurse()
    {
        if (!curseApplied)
            return;

        stats.RemovePacePercent(-cursePercent);
        curseApplied = false;
    }

    public override string DebugStatus
    {
        get
        {
            string status = "Next card: " + NextCard + "  Last: " + LastCard;

            if (ftCombat.TowerReady) status += "  Tower armed";
            if (ftCombat.DeathReady) status += "  Death armed";
            if (Cursed) status += "  Cursed";
            if (ftCombat.FateActive) status += "\nFATE SEALED " + ftCombat.FateRemaining.ToString("F1") + "s";

            return status;
        }
    }
}
