using UnityEngine;
using UnityEngine.SceneManagement;

// Lets other scripts find the player easily (Player.instance) and restarts the level when the player dies
[RequireComponent(typeof(PlayerStats))]
public class Player : MonoBehaviour
{
    public static Player instance;

    public PlayerStats playerStats;

    void Awake()
    {
        instance = this;
        playerStats = GetComponent<PlayerStats>();
    }

    void Start()
    {
        playerStats.OnHealthReachedZero += Die;
    }

    void OnDestroy()
    {
        playerStats.OnHealthReachedZero -= Die;
    }

    void Die()
    {
        playerStats.ApplyDeathPenalty();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
