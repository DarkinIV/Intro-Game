using UnityEngine;
using UnityEngine.SceneManagement;

/*
    Provides easy access to the Player and handles player death.
*/
[RequireComponent(typeof(PlayerStats))]
public class Player : MonoBehaviour
{
    public static Player instance;

    public PlayerStats playerStats;

    void Awake()
    {
        instance = this;

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }


    void Start()
    {
        playerStats.OnHealthReachedZero += Die;
    }


    void Die()
    {
        playerStats.ApplyDeathPenalty();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnHealthReachedZero -= Die;
    }
}