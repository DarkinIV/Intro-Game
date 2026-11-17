using UnityEngine;

// Lets an enemy block with a shield (the Brother Knight).
// Hits from the front are blocked while it isn't attacking, so the player has to wait for an opening
// or get round its side. Blocking wears down its guard, and when the guard breaks it's stunned for a moment.
[RequireComponent(typeof(EnemyController))]
public class EnemyGuard : MonoBehaviour, IDamageInterceptor
{
    [Range(0f, 1f)][SerializeField] float damageBlocked = 0.85f;
    [SerializeField] float blockAngle = 60f;        // hits from within this angle of where it faces are blocked
    [SerializeField] int guardHits = 4;             // blocks in a row before the guard breaks
    [SerializeField] float guardRecoverTime = 3f;   // the count resets after this long without blocking
    [SerializeField] float guardBreakStun = 1.5f;
    [SerializeField] string blockAnimation;         // optional animator trigger
    [SerializeField] GameObject blockEffect;        // optional sparks where the hit lands

    EnemyController controller;
    EnemyCombat combat;
    Animator animator;
    int blocksInARow;
    float lastBlockTime;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        combat = GetComponent<EnemyCombat>();
        animator = GetComponent<Animator>();
    }

    public int InterceptDamage(int amount, DamageType damageType, CharacterCombat attacker)
    {
        if (!CanBlock(attacker))
            return amount;

        if (Time.time - lastBlockTime > guardRecoverTime)
            blocksInARow = 0;

        blocksInARow++;
        lastBlockTime = Time.time;

        // Too many blocks, the guard breaks and the hit goes through
        if (blocksInARow >= guardHits)
        {
            blocksInARow = 0;
            controller.Stun(guardBreakStun);
            return amount;
        }

        if (!string.IsNullOrEmpty(blockAnimation))
            animator.SetTrigger(blockAnimation);

        if (blockEffect != null)
            Destroy(Instantiate(blockEffect, transform.position + Vector3.up * 1.4f + transform.forward * 0.6f, Quaternion.identity), 2f);

        return Mathf.RoundToInt(amount * (1f - damageBlocked));
    }

    // Only when it's ready (not attacking or stunned) and the hit comes from the front
    bool CanBlock(CharacterCombat attacker)
    {
        if (attacker == null || combat.IsAttacking || controller.IsStunned)
            return false;

        EnemyController.State state = controller.currentState;
        if (state == EnemyController.State.Staggered || state == EnemyController.State.Busy || state == EnemyController.State.Dead)
            return false;

        Vector3 toAttacker = attacker.transform.position - transform.position;
        toAttacker.y = 0f;
        return Vector3.Angle(transform.forward, toAttacker) <= blockAngle;
    }
}
