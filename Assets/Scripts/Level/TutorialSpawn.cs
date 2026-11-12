using UnityEngine;

// A trigger that shows one of UIManager's tutorials the first time the player walks into it
public class TutorialSpawn : MonoBehaviour
{
    [SerializeField] int collisionID; // which tutorial in UIManager's list to show

    bool shown;

    void OnTriggerEnter(Collider other)
    {
        if (shown || other is not CharacterController)
            return;

        shown = true;
        UIManager.instance.ShowTutorial(collisionID);
    }
}
