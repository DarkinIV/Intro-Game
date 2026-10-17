using UnityEngine;

public class OrbController : MonoBehaviour
{
    public CharacterCombat shooter;

    void OnTriggerEnter(Collider collider)
    {
        if (collider.isTrigger)
        {
            return;
        }
        CharacterStats victim = collider.GetComponentInParent<CharacterStats>();
        if (victim != null)
        {
            shooter.DealDamage(victim);
        }
        Destroy(gameObject);
    }
}
