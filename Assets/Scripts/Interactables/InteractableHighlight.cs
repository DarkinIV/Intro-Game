using System.Linq;
using UnityEngine;

/*
    Highlights an interactable object when the player is close enough
    to interact with it.
*/
public class InteractableHighlight : MonoBehaviour
{
    public Color color = Color.white;
    public Renderer meshRenderer;

    Material[] materials;
    Color[] originalColours;

    void Start()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<Renderer>();

        if (meshRenderer == null)
            return;

        materials = meshRenderer.materials;
        originalColours = materials.Select(x => x.color).ToArray();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (materials == null)
            return;

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].color = highlighted
                ? originalColours[i] * color
                : originalColours[i];
        }
    }
}