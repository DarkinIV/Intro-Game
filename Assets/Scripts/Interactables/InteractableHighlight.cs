using UnityEngine;

// Tints an interactable object when the player is close enough to use it
public class InteractableHighlight : MonoBehaviour
{
    public Color color = Color.white;
    public Renderer meshRenderer;   // uses the first renderer it finds if this is empty

    Material[] materials;
    Color[] originalColours;

    void Start()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<Renderer>();

        materials = meshRenderer.materials;
        originalColours = new Color[materials.Length];

        for (int i = 0; i < materials.Length; i++)
            originalColours[i] = materials[i].color;
    }

    public void SetHighlighted(bool highlighted)
    {
        for (int i = 0; i < materials.Length; i++)
            materials[i].color = highlighted ? originalColours[i] * color : originalColours[i];
    }
}
