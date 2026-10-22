using UnityEngine;
using UnityEngine.AI;

//pauses an enemy without touching any of its scripts, the acrobat's whip pull (stun after the yank) and the knight
//parry (stagger) both need this, so its written once here for easability 
//Pause(enemy) cancels its current swing, switches its AI off, stops its navmesh agent
//Resume switches everything back on 

public class EnemyStun
{
    EnemyController ai;
    NavMeshAgent agent;
    bool agentWasUsable;
    public static EnemyStun Pause(CharacterStats enemy)
    {
        EnemyStun stun = new EnemyStun();
        if (enemy == null) return stun;
        stun.ai = enemy.GetComponent<EnemyController>();
        stun.agent = enemy.GetComponent<NavMeshAgent>();
        CharacterCombat combat = enemy.GetComponent<CharacterCombat>();
        if (combat != null) combat.EndAttack();       //cancels a wind-up or swing in process 
        if (stun.ai != null) stun.ai.enabled = false; //if ai paused no chasing and oer no new attacks 
        stun.agentWasUsable = stun.agent != null && stun.agent.enabled && stun.agent.isOnNavMesh;
        if (stun.agentWasUsable)
        {
            stun.agent.isStopped = true;
            stun.agent.ResetPath();
        }
        return stun;
    }

    //true if the enemys agent can be moved with agent, used by the whip to drag it along the navmesh
    public bool CanMoveAgent
    {
        get { return agentWasUsable && agent != null && agent.isOnNavMesh; }
    }
    public NavMeshAgent Agent
    {
        get { return agent; }
    }
    public void Resume()
    {
        //the enemy might have been destroyed while paused, unity makes destroyed things as null 
        if (agentWasUsable && agent != null && agent.isOnNavMesh) agent.isStopped = false;
        if (ai != null) ai.enabled = true;
    }
}
