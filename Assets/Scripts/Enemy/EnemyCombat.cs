using UnityEngine;

public class EnemyCombat : CharacterCombat
{
    // AI repeatedly calls TryAttack; don't auto-queue a combo every frame.
    protected override bool CanQueueCombo => false;

    public Transform lockOnTransform;
}