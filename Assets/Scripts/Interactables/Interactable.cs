using UnityEngine;

// Base class for anything in the world the player can interact with (chests, pickups etc)
public class Interactable : MonoBehaviour
{
    [SerializeField] string interactionPrompt = "Interact";

    public float radius = 2f;
    public Transform interactionTransform;   // optional, uses this object's position if empty

    public string InteractionPrompt => interactionPrompt;
    public Vector3 InteractionPosition => interactionTransform != null ? interactionTransform.position : transform.position;

    public virtual bool CanInteract(Transform player)
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
