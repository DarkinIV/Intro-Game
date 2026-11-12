using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lets other scripts find the player easily (Player.instance).
// Also handles dying and resting at a checkpoint. Both reload the scene, which heals the player,
// refills the potions, clears insanity and brings the enemies back in one go.
[RequireComponent(typeof(PlayerStats))]
public class Player : MonoBehaviour
{
    public static Player instance;

    public PlayerStats playerStats;

    [SerializeField] float deathTime = 3f;   // time for the death animation and the fade before waking up
    [SerializeField] float restTime = 1.5f;

    Animator animator;
    bool reloading;

    void Awake()
    {
        instance = this;
        playerStats = GetComponent<PlayerStats>();
        animator = GetComponentInChildren<Animator>();
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

        animator.SetTrigger("Die");
        UIManager.instance.ShowDeathScreen();
        StartCoroutine(ReloadScene(deathTime));
    }

    // Called by a Checkpoint
    public void Rest()
    {
        if (reloading)
            return;

        RunState.hasCheckpoint = true;
        RunState.checkpointScene = gameObject.scene.name;
        RunState.checkpointPosition = transform.position;
        RunState.checkpointRotation = transform.rotation;

        // Invulnerable during fading
        playerStats.GrantInvulnerability(restTime);
        UIManager.instance.ShowRestScreen();
        StartCoroutine(ReloadScene(restTime));
    }

    IEnumerator ReloadScene(float delay)
    {
        reloading = true;
        yield return new WaitForSeconds(delay);

        // Karma is the only thing we keep for now
        RunState.karma = playerStats.Karma;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
