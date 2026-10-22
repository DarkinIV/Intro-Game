using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the fortune teller's passive is that she forsees attacks coming
//every enemy announces each swing the moment it starts so before the hitbox opens partway through the animation
//the shared EnemyAttackListener collects those and so for each swing nearby a red ring is drawn where the hit will land and flassh the enemy, doesnt dodge for u just shows u danger
//she gains mana if she dodges when she was in ring 
public class Foresight : MonoBehaviour
{
    [SerializeField] float range = 15f; //only warn about enemies this close
    [SerializeField] float warningTime = 0.45f; //how long the warning stays up
    [SerializeField] float strikeDistance = 1.2f;//how far in front of the enemy the hit lands
    [SerializeField] float ringRadius = 1.3f;
    [SerializeField] Color warningColor = new Color(1f, 0.15f, 0.1f, 0.9f);
    [SerializeField] bool flashEnemy = true;

    [Header("Foreseen dodge")]
    [SerializeField] float dodgeMana = 25f;
    [SerializeField] float threatMargin = 1f;//counts as "in danger" this far outside the ring too
    //one warning ring that is threatening her
    class Warning
    {
        public Vector3 point; //where the hit will land
        public float until; //the ring disappears at this time
        public bool rewarded; //already paid out mana for dodging it
    }
    List<Warning> warnings = new List<Warning>();

    EnemyAttackListener enemyAttacks;
    PlayerController controller;
    Mana mana;
    bool wasDashing;
    void Awake()
    {
        controller = GetComponent<PlayerController>();
        mana = GetComponent<Mana>();

        enemyAttacks = GetComponent<EnemyAttackListener>();
        if (enemyAttacks == null)
        {
            enemyAttacks = gameObject.AddComponent<EnemyAttackListener>();
        }
    }
    void OnEnable()
    {
        if (enemyAttacks != null) enemyAttacks.OnEnemyAttack += OnEnemyAttack;
    }
    void OnDisable()
    {
        if (enemyAttacks != null) enemyAttacks.OnEnemyAttack -= OnEnemyAttack;
    }
    void Update()
    {
        CheckForeseenDodge();
    }
    void OnEnemyAttack(EnemyCombat enemy)
    {
        if (!isActiveAndEnabled || enemy == null) return;
        if (Vector3.Distance(transform.position, enemy.transform.position) > range) return;
        Vector3 strikePoint = enemy.transform.position + enemy.transform.forward * strikeDistance;
        strikePoint.y = enemy.transform.position.y;
        //the ring grows from small to full size so when its full the hit is about to land
        RingMarker.Spawn(strikePoint, ringRadius * 0.2f, ringRadius, warningTime, warningTime, warningColor, enemy.transform, 0.08f);
        //remember it only if she is in or near the ring so smth threatening her 
        Vector3 me = transform.position;
        me.y = strikePoint.y;
        if (Vector3.Distance(me, strikePoint) <= ringRadius + threatMargin)
        {
            Warning w = new Warning();
            w.point = strikePoint;
            w.until = Time.time + warningTime;
            warnings.Add(w);
        }

        if (flashEnemy) StartCoroutine(Flash(enemy));
    }

    void CheckForeseenDodge()
    {
        //forget warnings whose ring is gone
        for (int i = warnings.Count - 1; i >= 0; i--)
        {
            if (Time.time > warnings[i].until) warnings.RemoveAt(i);
        }
        //the base controller has IsDashing, we only care about the moment a dash starts rn 
        bool dashing = controller != null && controller.IsDashing;
        bool dashStarted = dashing && !wasDashing;
        wasDashing = dashing;
        if (!dashStarted || mana == null) return;
        foreach (Warning w in warnings)
        {
            if (w.rewarded) continue;
            w.rewarded = true; //each warning can only pay out once
            mana.Add(dodgeMana, "foreseen dodge");
        }
    }

    //tint the enemy red for the warning time, then put its colours back, kinda same pricniple as using cyan for invnincibility frames
    IEnumerator Flash(EnemyCombat enemy)
    {
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
        List<Renderer> tinted = new List<Renderer>();
        List<Color> originalColors = new List<Color>();

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (r is LineRenderer) continue;//dont tint the rings
            if (!r.material.HasProperty("_Color")) continue;
            tinted.Add(r);
            originalColors.Add(r.material.color);
            r.material.color = Color.Lerp(r.material.color, warningColor, 0.6f);
        }
        yield return new WaitForSeconds(warningTime);
        for (int i = 0; i < tinted.Count; i++)
        {
            if (tinted[i] != null) tinted[i].material.color = originalColors[i];
        }
    }
}
