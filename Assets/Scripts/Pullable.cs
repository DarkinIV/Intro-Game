using UnityEngine;

//optional, put on an enemy to say how the acrobat's whip pull treats it
//light (default): the enemy gets yanked to the player
//heavy: too big to move, so the PLAYER gets pulled to the enemy instead
//enemies without this script count as light
public class Pullable : MonoBehaviour
{
    [SerializeField] private bool heavy = false;
    public bool IsHeavy => heavy;
}
