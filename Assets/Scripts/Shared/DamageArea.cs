using System.Collections.Generic;
using UnityEngine;

//one helper for doing damage to everything in a circle used by the acrobat's plunge slam, the fortune teller's tower card pull and the knight spin attack, here for resuability no point in having three 
//it goes through the base rules every time: CanDamage (no friendly fire, no dead targets), TakeDamage (damage numbers show up)
//and AddUltimateCharge (the hits fill the ult meter like normal hits)
public static class DamageArea
{
    //returns how many targets were hit
    public static int Hit(Vector3 center, float radius, int damage, CharacterCombat attacker, PlayerStats attackerStats)
    {
        //one enemy can have several colliders, this list makes sure each one is only hit once
        List<CharacterStats> alreadyHit = new List<CharacterStats>();

        Collider[] colliders = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider col in colliders)
        {
            CharacterStats victim = col.GetComponentInParent<CharacterStats>();
            if (victim == null) continue;
            if (alreadyHit.Contains(victim)) continue;
            alreadyHit.Add(victim);

            if (!attacker.CanDamage(victim)) continue; //base rules include not us, not friendly, not dead
            victim.TakeDamage(damage);//base damage path so damage numbers fire from here
            if (attackerStats != null) attackerStats.AddUltimateCharge(damage);
        }
        return alreadyHit.Count;
    }
}
