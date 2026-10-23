using UnityEngine;

// Enemy attacks. It also lets the player's scripts know whenever any enemy starts an attack.
public class EnemyCombat : CharacterCombat
{
    // Used by the Acrobat's auto dodge and the Fortune Teller's foresight
    public static event System.Action<EnemyCombat> OnAnyEnemyAttack;

    public Transform lockOnTransform;

    // The AI calls TryAttack every frame so it shouldn't queue up combos
    protected override bool CanQueueCombo => false;

    protected override void OnEnable()
    {
        base.OnEnable();
        OnAttack += AnnounceAttack;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        OnAttack -= AnnounceAttack;
    }

    void AnnounceAttack()
    {
        OnAnyEnemyAttack?.Invoke(this);
    }
}
