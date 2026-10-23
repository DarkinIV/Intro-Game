using UnityEngine;

// Finds the closest thing the player can interact with, highlights it and shows the [E] prompt.
// PlayerController calls TryInteract when E is pressed.
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] LayerMask interactionMask;
    [SerializeField] float interactionSearchRadius = 3f;

    Interactable currentInteractable;
    InteractableHighlight currentHighlight;

    void Update()
    {
        Interactable closest = FindClosestInteractable();

        if (closest != currentInteractable)
            SetInteractionTarget(closest);
    }

    public void TryInteract()
    {
        if (currentInteractable != null)
            currentInteractable.Interact();
    }

    Interactable FindClosestInteractable()
    {
        Interactable closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in Physics.OverlapSphere(transform.position, interactionSearchRadius, interactionMask, QueryTriggerInteraction.Collide))
        {
            Interactable interactable = hit.GetComponentInParent<Interactable>();

            // Each interactable has its own range, so it might still be too far away
            if (interactable == null || !interactable.CanInteract(transform))
                continue;

            float distance = (interactable.InteractionPosition - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        return closest;
    }

    void SetInteractionTarget(Interactable newInteractable)
    {
        if (currentHighlight != null)
            currentHighlight.SetHighlighted(false);

        currentInteractable = newInteractable;
        currentHighlight = null;

        if (currentInteractable == null)
        {
            UIManager.instance.HideInteractionPrompt();
            return;
        }

        currentHighlight = currentInteractable.GetComponentInChildren<InteractableHighlight>();
        if (currentHighlight != null)
            currentHighlight.SetHighlighted(true);

        UIManager.instance.ShowInteractionPrompt("[E] " + currentInteractable.InteractionPrompt);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionSearchRadius);
    }
}
