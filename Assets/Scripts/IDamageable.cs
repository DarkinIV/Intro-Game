using UnityEngine;

//an interface is a promise: anything that says it is IDamageable MUST have a TakeDamage method
//so the attack code can hit enemies, the player, breakable crates etc without knowing what they are
public interface IDamageable
{
    //amount = how much damage, hitFrom = where the hit came from (for knockback later)
    void TakeDamage(float amount, Vector3 hitFrom);
}
