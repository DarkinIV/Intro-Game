using UnityEngine;

// The Knight. Hold right click to block. F: Spin Attack, R: Unkillable.
// The actual combat stuff is in KnightCombat.
[RequireComponent(typeof(KnightCombat))]
public class KnightController : PlayerController
{
    KnightCombat knightCombat;

    protected override void Awake()
    {
        base.Awake();
        knightCombat = GetComponent<KnightCombat>();
    }

    protected override void Update()
    {
        base.Update();
        knightCombat.SetBlocking(Input.GetMouseButton(1) && !IsBusy && !stats.IsDead);
    }

    // Can't attack with the shield up
    protected override void Attack()
    {
        if (!knightCombat.IsBlocking)
            base.Attack();
    }

    public override bool UseAbility(Ability ability)
    {
        if (!(ability.Data is SpinAttackData spin))
            return false;

        knightCombat.Spin(spin);
        return true;
    }

    public override bool UseUltimate(UltimateAbility ultimate)
    {
        if (!(ultimate.Data is UnkillableData unkillable))
            return false;

        knightCombat.ActivateUnkillable(unkillable.duration);
        return true;
    }

    public override string DebugStatus
    {
        get
        {
            string status = knightCombat.ShieldRaised ? "Shield up" : "Shield down";

            if (knightCombat.RiposteReady)
                status += "  Riposte ready";

            if (knightCombat.UnkillableActive)
                status += "  UNKILLABLE " + knightCombat.UnkillableRemaining.ToString("F1") + "s";

            return status;
        }
    }
}
