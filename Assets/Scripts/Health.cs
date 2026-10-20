using UnityEngine;
using UnityEngine.Events;

//health for ANYTHING that can be hurt, the player and enemies both use this same script
public class Health : MonoBehaviour, IDamageable //IDamageable = this script keeps the TakeDamage promise
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = false; //tick for things like the dummy that should just disappear

    [Header("Events")]
    //UnityEvents show up in the inspector, you can drag in things that should happen (sounds, effects etc)
    //other scripts can also listen to them, the hit reactions will plug in here later
    public UnityEvent onDamaged;
    public UnityEvent onDied;
    public UnityEvent onHitBlocked; //a hit was ignored bcz of i-frames (the acrobat's ult meter will use this later)

    //{ get; private set; } = other scripts can READ these but only this script can change them
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    //invincible ("i-frames") until this time. a time instead of true/false so different things
    //(dodge, ult, getting hit) can all give i-frames without switching each other's off
    private float invulnerableUntil = -1f;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    private void Awake()
    {
        CurrentHealth = maxHealth; //start full
    }

    //the class script calls this to set max health from the class sheet
    public void SetMaxHealth(float max)
    {
        maxHealth = max;
        CurrentHealth = maxHealth; //start full
    }

    //be invincible for this many seconds from now. if we are already invincible for longer, keep the longer one
    public void MakeInvulnerable(float seconds)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    public void TakeDamage(float amount, Vector3 hitFrom)
    {
        if (IsDead || amount <= 0f) return; //cant hurt something thats already dead

        if (IsInvulnerable)
        {
            Debug.Log($"<color=cyan>Hit blocked by i-frames</color> ({name} would have taken {amount})");
            onHitBlocked.Invoke();
            return; //no damage
        }

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f); //Max stops it going below 0
        Debug.Log($"{name} took {amount} damage ({CurrentHealth}/{maxHealth})"); //shows in the console for testing
        onDamaged.Invoke(); //tell anyone listening that we got hit

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    //back to full health and alive again, used when the player respawns
    public void Revive()
    {
        IsDead = false;
        CurrentHealth = maxHealth;
        invulnerableUntil = -1f;
    }

    private void Die()
    {
        IsDead = true;
        Debug.Log($"{name} died");
        onDied.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject); //removes the whole object from the scene
        }
    }
}
