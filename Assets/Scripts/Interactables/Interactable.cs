using UnityEngine;

/*
    Base class for world objects the player can interact with,
    such as items, chests, doors and NPCs.
*/
public class Interactable : MonoBehaviour
{
    [SerializeField] string interactionPrompt = "Interact";

    public float radius = 2f;
    public Transform interactionTransform;

    public string InteractionPrompt => interactionPrompt;


    public Vector3 InteractionPosition
    {
        get
        {
            return interactionTransform != null ? interactionTransform.position : transform.position;
        }
    }


    public bool CanInteract(Transform player)
    {
        return Vector3.Distance(player.position, InteractionPosition) <= radius;
    }


    public virtual void Interact()
    {
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(InteractionPosition, radius);
    }
}