using System.Collections.Generic;
using UnityEngine;

//puzzle piece for the fortune teller's crystal sight ( u gotta hold C) can be put on any object in a level
//Illusion off this is the default actually: a hidden thing, invisible and not solid until she looks at it (ties in with senuas sacrifice hellblade puzzles)
//Illusion on: smth fake, smth that would be visible and solid normally, but disappears while she/he looks at it  ( can be fake walls)
//affects every renderer and collider on the object and its children
//DO NOT LOOK AT THIS IN THIS STAGE CUZ NOTHING IN THE ENV SUPPORTS IT, NOTHING ADDED TO SCENE TO SUPPORT THIS, THIS IS WHEN WE HAVE THE PUZZLES BUT SINCE I ALR DID IT I THOUGHT TO ADD IT. 
public class RevealedBySight : MonoBehaviour
{
    [SerializeField] bool illusion = false;
    [SerializeField] bool affectColliders = true;//untick for things that should only appear and disappear visually
    //every one of these in the scene, so the fortune teller can find them without searching
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
        if (on == revealed) return;
        Apply(on);
    }
    void Apply(bool on)
    {
        revealed = on;

        //hidden things exist only while looing, illusions only while not looking
        bool exists = on;
        if (illusion) exists = !on;
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = exists;
        }
        if (affectColliders)
        {
            foreach (Collider c in colliders)
            {
                if (c != null) c.enabled = exists;
            }
        }
    }
}
