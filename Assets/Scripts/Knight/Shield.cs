using UnityEngine;

// Goes on the Knight's shield collider. The collider is only on while the shield is raised.
// If an attack touches the shield before the Knight, KnightCombat decides how much gets through.
public class Shield : MonoBehaviour
{
    KnightCombat knight;

    public CharacterStats Owner { get; private set; }

    void Awake()
    {
        knight = GetComponentInParent<KnightCombat>();
        Owner = GetComponentInParent<CharacterStats>();
    }

    public void Block(CharacterCombat attacker)
    {
        knight.Blocked(attacker);
    }
}
