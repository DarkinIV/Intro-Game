using UnityEngine;

/*
    Creates a world-space health bar for this character.

    Add this component only to characters that should display
    a health bar above them in the world.
*/
[RequireComponent(typeof(CharacterStats))]
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] Transform healthBarPosition;

    CharacterStats stats;


    void Start()
    {
        stats = GetComponent<CharacterStats>();

        if (healthBarPosition == null)
            healthBarPosition = transform;

        if (HealthUIManager.instance != null)
        {
            HealthUIManager.instance.Create(
                healthBarPosition,
                stats
            );
        }
    }
}