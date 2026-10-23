using System.Collections.Generic;
using UnityEngine;

// A puzzle object for the Fortune Teller's Crystal Sight (hold C). Not used in any level yet.
// Normally it's hidden (invisible and not solid) until she looks at it.
// With illusion ticked it's the other way round, like a fake wall that disappears while she looks at it.
public class RevealedBySight : MonoBehaviour
{
    [SerializeField] bool illusion = false;
    [SerializeField] bool affectColliders = true;   // untick if it should only change how it looks

    // Every one of these in the scene, so the Fortune Teller doesn't have to search for them
    public static List<RevealedBySight> All = new List<RevealedBySight>();

    Renderer[] renderers;
    Collider[] colliders;
    bool revealed;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        Apply(false);
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    public void SetRevealed(bool on)
    {
        if (on != revealed)
            Apply(on);
    }

    void Apply(bool on)
    {
        revealed = on;

        // Hidden things only exist while she's looking, illusions only while she isn't
        bool exists = illusion ? !on : on;

        foreach (Renderer r in renderers)
            r.enabled = exists;

        if (affectColliders)
        {
            foreach (Collider c in colliders)
                c.enabled = exists;
        }
    }
}
