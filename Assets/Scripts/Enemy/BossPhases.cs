using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Turns an enemy into a boss. Goes next to its EnemyController.
// The fight is split into phases. Each one starts when the boss's health drops to a set percent and can
// give it new attacks, make it faster, switch on hazards (fire rain, lava...), play an animation (a roar) and heal.
// The first phase starts when the boss first sees the player, so its animation is the boss's intro.
// It also shows the boss health bar at the bottom of the screen while the fight is on.
// Remember to tick Is Boss and untick Waits For Turn on the EnemyController.
[RequireComponent(typeof(EnemyController))]
public class BossPhases : MonoBehaviour
{
    [System.Serializable]
    public class Phase
    {
        public string name = "Phase";
        [Tooltip("Starts when the boss's health drops to this percent. Put 100 on the first phase")]
        [Range(0f, 100f)] public float startsAtHealth = 100f;

        [Header("Animation")]
        [Tooltip("Animator trigger played when the phase starts. Leave it empty for no animation")]
        public string animationTrigger = "Roar";
        [Tooltip("How long the boss stands still while it plays")]
        public float animationTime = 2f;

        [Header("Changes")]
        [Tooltip("The attacks for this phase. Leave it empty to keep the ones it already has")]
        public List<EnemyAttack> attacks = new List<EnemyAttack>();
        [Tooltip("0.2 means 20% faster than its normal speed")]
        public float extraMoveSpeed = 0f;
        [Tooltip("Only works if the attack state in the animator uses AttackSpeed as its speed multiplier")]
        public float attackSpeed = 1f;
        [Tooltip("Heals this percent of its max health while the phase animation plays. Stunning it (parry, whip pull) stops the heal")]
        [Range(0f, 100f)] public float healPercent = 0f;

        [Tooltip("Switched on while this phase is going (fire rain, lava, particles...)")]
        public GameObject[] hazards = new GameObject[0];
    }

    [SerializeField] string bossName = "Boss";
    [SerializeField] List<Phase> phases = new List<Phase>();

    EnemyController controller;
    CharacterStats stats;
    EnemyCombat combat;

    int currentPhase = -1; // -1 until the fight starts
    float speedBonus;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        stats = GetComponent<CharacterStats>();
        combat = GetComponent<EnemyCombat>();
    }

    void OnEnable()
    {
        controller.OnSpottedPlayer += StartFight;
        controller.OnLostPlayer += EndFight;
        stats.OnDamaged += CheckForNextPhase;
        stats.OnHealthReachedZero += EndFight;
    }

    void OnDisable()
    {
        controller.OnSpottedPlayer -= StartFight;
        controller.OnLostPlayer -= EndFight;
        stats.OnDamaged -= CheckForNextPhase;
        stats.OnHealthReachedZero -= EndFight;
    }

    void Start()
    {
        // No hazards until the fight starts
        foreach (Phase phase in phases)
            SetHazards(phase, false);
    }

    void StartFight()
    {
        BossHealthBar.instance.Show(bossName, stats);

        if (currentPhase == -1)
            StartPhase(0);
        else
            SetHazards(phases[currentPhase], true); // the player ran off and came back
    }

    // The player got away or the boss died
    void EndFight()
    {
        BossHealthBar.instance.Hide();

        if (currentPhase >= 0)
            SetHazards(phases[currentPhase], false);
    }

    void CheckForNextPhase(int damage)
    {
        // The killing blow shouldn't start a new phase
        if (currentPhase == -1 || stats.currentHealth <= 0)
            return;

        float healthPercent = 100f * stats.currentHealth / stats.maxHealth.Value;

        // Go from the last phase backwards, so a big hit can skip straight past a phase
        for (int i = phases.Count - 1; i > currentPhase; i--)
        {
            if (healthPercent <= phases[i].startsAtHealth)
            {
                StartPhase(i);
                return;
            }
        }
    }

    void StartPhase(int index)
    {
        if (currentPhase >= 0)
            SetHazards(phases[currentPhase], false);

        currentPhase = index;
        Phase phase = phases[index];

        if (phase.attacks.Count > 0)
            controller.SetAttacks(phase.attacks);

        // Swap the old speed bonus for the new one
        stats.RemovePacePercent(speedBonus);
        speedBonus = phase.extraMoveSpeed;
        stats.AddPacePercent(speedBonus);

        // Multiplies each attack's own animation speed
        combat.attackSpeedMultiplier = phase.attackSpeed;

        SetHazards(phase, true);

        if (!string.IsNullOrEmpty(phase.animationTrigger))
            controller.PlayAnimation(phase.animationTrigger, phase.animationTime);

        if (phase.healPercent > 0f)
            StartCoroutine(Heal(phase));
    }

    // Heals a little every frame while the phase animation plays. The player can still hit it
    // to undo the heal, and stunning it ends the animation and the heal with it.
    IEnumerator Heal(Phase phase)
    {
        int total = Mathf.RoundToInt(stats.maxHealth.Value * phase.healPercent / 100f);
        int healed = 0;

        for (float time = 0f; time < phase.animationTime && controller.currentState == EnemyController.State.Busy; time += Time.deltaTime)
        {
            int shouldHaveHealed = Mathf.RoundToInt(total * time / phase.animationTime);
            stats.Heal(shouldHaveHealed - healed);
            healed = shouldHaveHealed;
            yield return null;
        }
    }

    void SetHazards(Phase phase, bool on)
    {
        foreach (GameObject hazard in phase.hazards)
            hazard.SetActive(on);
    }
}
