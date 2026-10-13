using UnityEngine;

/*
    Handles the player's world interactions.

    Finds the closest interactable in range, highlights it,
    controls the interaction UI and performs the interaction
    when requested by PlayerController.
*/
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] LayerMask interactionMask;
    [SerializeField] float interactionSearchRadius = 3f;

    Interactable currentInteractable;
    InteractableHighlight currentHighlight;

    void Update()
    {
        UpdateInteractionTarget();
    }


    public void TryInteract()
    {
        if (currentInteractable == null)
            return;

        currentInteractable.Interact();
    }


    /*
        Searches all nearby interactables and selects the nearest
        one that is actually within its own interaction range.
    */
    void UpdateInteractionTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactionSearchRadius,
            interactionMask,
            QueryTriggerInteraction.Collide
        );

        Interactable closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Interactable interactable = hit.GetComponentInParent<Interactable>();

            if (interactable == null)
                continue;

            if (!interactable.CanInteract(transform))
                continue;

            float distance = (interactable.InteractionPosition - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        if (closestInteractable != currentInteractable)
            SetInteractionTarget(closestInteractable);
    }


    /*
        Turns off feedback for the previous target and enables
        highlighting and UI feedback for the new one.
    */
    void SetInteractionTarget(Interactable newInteractable)
    {
        if (currentHighlight != null)
            currentHighlight.SetHighlighted(false);

        currentInteractable = newInteractable;
        currentHighlight = null;

        if (currentInteractable != null)
        {
            currentHighlight = currentInteractable.GetComponentInChildren<InteractableHighlight>();

            if (currentHighlight != null)
                currentHighlight.SetHighlighted(true);

            UIManager.instance.ShowInteractionPrompt("[E] " + currentInteractable.InteractionPrompt);
        }
        else
        {
            UIManager.instance.HideInteractionPrompt();
        }
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(transform.position, interactionSearchRadius);
    }
}