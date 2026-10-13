using UnityEngine;

/*
    Gives the player progression rewards when this enemy dies.

    Add this component to each enemy prefab and set the XP and gold
    values independently in the Inspector.
*/
[RequireComponent(typeof(CharacterStats))]
public class EnemyRewards : MonoBehaviour
{
    [Header("Death Rewards")]
    [SerializeField] int experienceReward = 25;
    [SerializeField] int goldReward = 10;

    CharacterStats stats;
    bool rewardsGiven;


    void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }


    void OnEnable()
    {
        if (stats != null)
            stats.OnHealthReachedZero += GiveRewards;
    }


    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthReachedZero -= GiveRewards;
    }


    void GiveRewards()
    {
        if (rewardsGiven)
            return;

        rewardsGiven = true;

        PlayerStats playerStats = null;

        if (Player.instance != null)
            playerStats = Player.instance.playerStats;

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        if (playerStats == null)
            return;

        playerStats.AddExperience(experienceReward);
        playerStats.AddGold(goldReward);
    }
}
