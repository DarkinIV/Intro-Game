using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Lets an enemy teleport behind the player (the Hell Demon).
// While it's chasing, it vanishes in a burst of particles and appears behind the player a moment later,
// so it can attack straight away. The player can't hit it while it's gone.
[RequireComponent(typeof(EnemyController))]
public class EnemyBlink : MonoBehaviour
{
    [SerializeField] float minDistance = 4f;       // doesn't bother if the player is already this close
    [SerializeField] float maxDistance = 14f;
    [SerializeField] float cooldown = 9f;
    [SerializeField] float behindDistance = 1.8f;  // how far behind the player it appears
    [SerializeField] float vanishTime = 0.5f;      // how long it's gone for
    [SerializeField] string blinkAnimation;        // optional animator trigger for vanishing
    [SerializeField] GameObject blinkEffect;       // optional particles, played where it vanishes and where it appears

    EnemyController controller;
    CharacterStats stats;
    NavMeshAgent agent;
    Renderer[] renderers;
    float readyTime;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        stats = GetComponent<CharacterStats>();
        agent = GetComponent<NavMeshAgent>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        Transform target = controller.Target;
        if (target == null || Time.time < readyTime || controller.currentState != EnemyController.State.Chase)
            return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance >= minDistance && distance <= maxDistance)
            StartCoroutine(Blink(target));
    }

    IEnumerator Blink(Transform target)
    {
        readyTime = Time.time + cooldown;

        controller.PlayAnimation(blinkAnimation, vanishTime);
        stats.GrantInvulnerability(vanishTime);
        PlayEffect();
        SetVisible(false);

        yield return new WaitForSeconds(vanishTime);

        // Appear behind the player, on the NavMesh so it doesn't end up inside a wall
        if (target != null && !stats.IsDead)
        {
            Vector3 behind = target.position - target.forward * behindDistance;
            if (NavMesh.SamplePosition(behind, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(toTarget);
        }

        SetVisible(true);
        PlayEffect();
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer rend in renderers)
            rend.enabled = visible;
    }

    void PlayEffect()
    {
        if (blinkEffect != null)
            Destroy(Instantiate(blinkEffect, transform.position + Vector3.up, Quaternion.identity), 3f);
    }
}
