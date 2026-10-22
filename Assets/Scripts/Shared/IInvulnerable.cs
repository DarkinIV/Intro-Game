//For anyone reading or debugging this in the future: IInvulnerable is an interface
//Any script that uses IInvulnerable has a "contract! to have a method BlocksHitFrom called
//doesnt matter which script it is, hope it helps - kc 
//Any time something is about to deal damage, the game runs CanDamage to check whether the hit is allowed or not 
//The hook added in that snippet checks if the victim has an IInvlnerable script, if yes then the BHF is called
//Then it passes the attacker, so if u have BlocksHitsFrom(x), the x is the attacker's combat script
// so we as the player are told who is hitting us, and see how hard it hits us and where it hits us from
// Implemented by the Acrobat's i-frames (block everything while invulnerable) and the knight's shield
//(block from the front, parry, never drop below 1 HP during Unkillable).
//Return true = the base game musnt apply this hit
public interface IInvulnerable
{
    bool BlocksHitFrom(CharacterCombat attacker);
}
