using UnityEngine;

// Goes on every attack state in the animator. Don't put it on the layer itself or idle counts as attacking too.
// Unity always calls enter and exit, even when an attack gets interrupted, so combat can't get stuck.
public class AttackStateBehaviour : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CharacterCombat combat = animator.GetComponentInParent<CharacterCombat>();
        if (combat != null)
            combat.AttackStateEntered();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CharacterCombat combat = animator.GetComponentInParent<CharacterCombat>();
        if (combat != null)
            combat.AttackStateExited();
    }
}
