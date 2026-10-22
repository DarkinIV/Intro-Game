using UnityEngine;

//the fortune teller's version of playercombat, replacing PlayerCombat on her prefab (same idea as the acrobat's controller)
//playercombat is untouched, and everything that looks for a PlayerCombat still finds: combos projectiles etc, everything is inherited.
//only three things are different: damage in this case comes form magic and not strength, attacks cost mana instead of stamina, no attacking while she meditates to regen mana
//Foresight (foreseen dodge), without them the bar could never refill meaning they are required together
[RequireComponent(typeof(Mana))]
[RequireComponent(typeof(FortuneTellerAbilities))]
[RequireComponent(typeof(Foresight))]
public class FortuneTellerCombat : PlayerCombat
{
    [SerializeField] float manaPerOrb = 12f;
    PlayerStats ftStats;
    Mana mana;
    FortuneTellerAbilities abilities;
    public float ManaPerOrb
    {
        get { return manaPerOrb; }
    }
    protected override void Awake()
    {
        base.Awake();
        ftStats = GetComponent<PlayerStats>();
        mana = GetComponent<Mana>();
        abilities = GetComponent<FortuneTellerAbilities>();

        //RequireComponent only adds things when this component is first added, so check prefabs that already had it
        if (abilities == null || GetComponent<Foresight>() == null)
        {
            Debug.LogError("Fortune Teller setup incomplete: add the 'Fortune Teller Abilities' and 'Foresight' components "
                + "to this prefab, or her mana can never refill. See CHANGELOG.md.");
        }
    }
    //checked when an attack or the next hit of a combo starts
    protected override bool CanAttack()
    {
        if (abilities != null && abilities.IsMeditating) return false;

        if (!mana.Has(manaPerOrb))
        {
            if (abilities != null) Debug.Log("Not enough mana (hold V to meditate)");
            else Debug.Log("Not enough mana (meditate needs the Fortune Teller Abilities component)");
            return false;
        }
        return true;
    }
    //paid when the orb is actually fired (the FireProjectile animation event finally worked a bit on that)
    protected override bool ConsumeAttackCost()
    {
        return mana.TrySpend(manaPerOrb);
    }
    //magic instead of strength AS DISCUSSED
    protected override int GetAttackDamage()
    {
        return ftStats.magic.Value;
    }
}
