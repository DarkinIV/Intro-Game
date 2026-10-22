using UnityEngine;

//optional marker for enemies
//light: the enemy gets yanked to the player
//heavy: too big to move(have to test this too pls add a "final boss" of sorts), the player should be pulled towards it 
public class Pullable : MonoBehaviour
{
    [SerializeField] bool heavy = false;
    public bool IsHeavy
    {
        get { return heavy; }
    }
}
