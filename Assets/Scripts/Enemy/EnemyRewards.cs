using UnityEngine;

// Gives the player experience and karma when this enemy dies
[RequireComponent(typeof(CharacterStats))]
public class EnemyRewards : MonoBehaviour
{
    [Header("Death Rewards")]
    [SerializeField] int experienceReward = 25;
    [SerializeField] int karmaReward = 10;

    CharacterStats stats;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    void OnEnable()
    {
        stats.OnHealthReachedZero += GiveRewards;
    }

    void OnDisable()
    {
        stats.OnHealthReachedZero -= GiveRewards;
    }

    void GiveRewards()
    {
        if (Player.instance == null)
            return;

        Player.instance.playerStats.AddExperience(experienceReward);
        Player.instance.playerStats.AddKarma(karmaReward);
    }
}
