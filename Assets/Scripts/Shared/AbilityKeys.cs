using UnityEngine;

//goes on the player, the two ability keys for every class see map below to get it 
//basic key: basic abaility for every class so fortune teller has tarot draw on F, knight has spin attack on F and  acrobat has none on that )
//ultimate key : ult ability for every class  (acrobat: Q, fortune teller and knight both use R)

[RequireComponent(typeof(PlayerStats))]
public class AbilityKeys : MonoBehaviour
{
    [SerializeField] KeyCode basicKey = KeyCode.F;
    [SerializeField] KeyCode ultimateKey = KeyCode.R;
    PlayerStats stats;
    PlayerController controller;   //prev controller or new one (they are subclasses  so this finds them too)
    PlayerCombat combat;
    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();
    }
    void Update()
    {
        if (stats.IsDead) return;
        if (basicKey != KeyCode.None && Input.GetKeyDown(basicKey))
        {
            if (stats.BasicAbility == null)
            {
                Debug.Log("No basic ability assigned on the class sheet.(yet?)");
            }
            else if (!stats.BasicAbility.TryUse(stats, controller, combat))
            {
                Debug.Log(stats.BasicAbility.Data.abilityName + " on cooldown (" + stats.BasicAbility.CooldownRemaining.ToString("F1") + "s)");
            }
        }
        if (ultimateKey != KeyCode.None && Input.GetKeyDown(ultimateKey))
        {
            if (stats.Ultimate == null)
            {
                Debug.Log("No ultimate assigned on the class sheet.");
            }
            else if (!stats.Ultimate.TryUse(stats, controller, combat))
            {
                Debug.Log(stats.Ultimate.Data.abilityName + " not ready (" + stats.Ultimate.ChargePercent.ToString("P0")
                    + " charged, cooldown " + stats.Ultimate.CooldownRemaining.ToString("F1") + "s)");
            }
        }
    }
}
