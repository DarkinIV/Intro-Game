using System.Collections.Generic;
using UnityEngine;

//goes on the player, left click hits everything damageable in front of the player
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float attackCooldown = 0.5f; //seconds between attacks so u cant spam
    [SerializeField] private float attackReach = 2f; //how far in front of the player's middle the hit area sits
    [SerializeField] private float attackRadius = 1.5f; //how big the hit area is
    [SerializeField] private LayerMask hittableLayers = ~0; //which layers can be hit, ~0 = everything

    private float lastAttackTime = -999f; //-999 so the first attack is allowed straight away

    void Update()
    {
        //0 = left mouse button, GetMouseButtonDown = only the frame it was clicked
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    private void Attack()
    {
        lastAttackTime = Time.time;

        //invisible ball in front of the player, anything touching it gets hit
        Vector3 center = transform.position + transform.forward * attackReach;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius, hittableLayers, QueryTriggerInteraction.Ignore);

        //one enemy can have more than one collider, this list stops us hitting the same one twice in one swing
        HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; //skip our own colliders (the body and the nose)

            //look on the thing we hit AND its parents, so hitting an enemy's arm still finds its health
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !alreadyHit.Add(target)) continue; //Add gives false if its already in the list

            target.TakeDamage(damage, transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        //DEBUG ONLY: shows the hit area in the scene view when the player is selected
        //flashes solid for a moment right after an attack
        Vector3 center = transform.position + transform.forward * attackReach;
        bool justAttacked = Application.isPlaying && Time.time - lastAttackTime < 0.15f;
        Gizmos.color = justAttacked ? new Color(1f, 0.5f, 0f, 0.5f) : new Color(1f, 0.9f, 0f, 0.8f);
        if (justAttacked) Gizmos.DrawSphere(center, attackRadius);
        else Gizmos.DrawWireSphere(center, attackRadius);
    }
}
