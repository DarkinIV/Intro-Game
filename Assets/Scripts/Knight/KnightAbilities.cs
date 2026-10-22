using UnityEngine;
    //the knight ability key
    //blocking and parrying are in KnightShield so if ur looking for that check there
    //what u can find here is : F  Spin Attack
    //R  Unkillable  which is the ult we discussed
    //the F and R keys themselves are read by the shared AbilityKeys script, which runs the ability through the base system,
    //the base system then calls Spin / ActivateUnkillable here
    //these comments r so similar to the ones in prev scripts so we are just being repetitve atp 
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(KnightShield))]
public class KnightAbilities : MonoBehaviour
{
    [SerializeField] Color spinColor = new Color(0.85f, 0.9f, 1f);
    [SerializeField] Color unkillableColor = new Color(1f, 0.8f, 0.2f);
    public bool Spinning
    {
        get { return Time.time < spinEnd; }
    }
    PlayerStats stats;
    PlayerCombat combat;
    KnightShield shield;
    float spinStart;
    float spinEnd;
    float spinStartYaw;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        combat = GetComponent<PlayerCombat>();
        shield = GetComponent<KnightShield>();
    }

    //spin attack
    //called by SpinAttackAbility when F clicked
    public void Spin(SpinAttackData data)
    {
        int damage = Mathf.Max(1, Mathf.RoundToInt(stats.strength.Value * data.strengthMultiplier));

        //hurt everything around him (shared helper: base rules, no friendly fire(maybe if we do an ally system, added itcuz it seemed easy for rn, no dead, no self)
        int count = DamageArea.Hit(transform.position + Vector3.up, data.radius, damage, combat, stats);
        RingMarker.Spawn(transform.position, 0.5f, data.radius, data.spinTime, data.spinTime + 0.1f, spinColor, transform, 0.1f);
        Debug.Log("<color=#ddeeff>Spin Attack</color>: " + damage + " damage to " + count + " target(s)"); //logging
        //turn the knight a full circle (completley visual) dont touch for now even if it might not look as good
        spinStart = Time.time;
        spinEnd = Time.time + data.spinTime;
        spinStartYaw = transform.eulerAngles.y;
    }
    //LateUpdate runs after the controller has turned him, so the spin wins while it lasts
    void LateUpdate()
    {
        if (!Spinning) return;
        float t = (Time.time - spinStart) / Mathf.Max(spinEnd - spinStart, 0.01f); //0 at the start, 1 at the end
        transform.rotation = Quaternion.Euler(0f, spinStartYaw + 360f * Mathf.Clamp01(t), 0f);
    }
    //unkillable
    //called by UnkillableUltimate when R is clciked
    public void ActivateUnkillable(float duration)
    {
        shield.ActivateUnkillable(duration);//the shield's hook enforces "never below 1 HP"
        //ur gonna need to add more enemies to test this cuz the ult bar does not fill enough for this to be tested, pls keep that in mind
        RingMarker.Spawn(transform.position, 1.2f, 1.2f, 0f, duration, unkillableColor, transform, 0.09f);
        Debug.Log("<color=yellow>UNKILLABLE</color> for " + duration + "s: you can't drop below 1 HP");
    }
}
